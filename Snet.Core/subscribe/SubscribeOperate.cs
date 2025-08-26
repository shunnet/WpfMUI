using Snet.Core.extend;
using Snet.Core.subscribe.core;
using Snet.Model.data;
using Snet.Model.@interface;
using Snet.Unility;
using System.Collections.Concurrent;
using System.Threading.Channels;
using static Snet.Core.subscription.SubscribeData;
namespace Snet.Core.subscription
{
    /// <summary>
    /// 自定义订阅操作<br/>
    /// 只支持两种数据结果的自定义订阅<br/>
    /// 1.ConcurrentDictionary《string, AddressValue》<br/>
    /// 2.List《ConcurrentDictionary《string, AddressValue》》
    /// </summary>
    public class SubscribeOperate : CoreUnify<SubscribeOperate, Basics>, ISubscribe, IOn, IOff, IGetStatus, IDisposable
    {
        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="basics">基础数据</param>
        public SubscribeOperate(Basics basics) : base(basics) { }

        /// <summary>
        /// 监控开关
        /// </summary>
        private CancellationTokenSource? MonitorSwitch = null;

        /// <summary>
        /// 任务集合
        /// </summary>
        private ConcurrentDictionary<CancellationTokenSource, Task>? TaskArray = null;

        /// <summary>
        /// 数据队列
        /// </summary>
        private Channel<AddressValue>? DataQueue = null;

        /// <summary>
        /// 别的操作进来是否继续轮询
        /// </summary>
        private bool GoOn = true;

        /// <summary>
        /// 数据缓存池
        /// </summary>
        private ConcurrentDictionary<string, AddressValue> DataCachePool = new ConcurrentDictionary<string, AddressValue>();

        /// <summary>
        /// 多组数据缓存池
        /// </summary>
        private List<ConcurrentDictionary<string, AddressValue>> DataCachePoolArray = new List<ConcurrentDictionary<string, AddressValue>>();

        /// <summary>
        /// 通道设置
        /// </summary>
        private BoundedChannelOptions channelOptions = new BoundedChannelOptions(int.MaxValue)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        /// <summary>
        /// 执行轮询（极致优化版）
        /// </summary>
        private async Task Polling(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // 如果监控开关被取消，直接跳过当前轮询
                    if (!GoOn)
                        continue;

                    // 如果地址列表为空，直接跳过当前轮询
                    if (basics.Address == null || basics.Address.AddressArray == null || basics.Address.AddressArray.Count <= 0)
                        continue;

                    // 缓存地址引用副本，减少属性访问和锁竞争
                    var addressList = basics.Address.AddressArray;

                    // 检查是否存在地址名重复但类型不同的情况
                    var duplicateGroups = addressList
                        .GroupBy(p => p.AddressName)
                        .Where(g => g.Count() > 1)
                        .ToArray();

                    // 如果存在重复的地址名，移除所有重复的地址，并提示
                    if (duplicateGroups.Length > 0)
                    {
                        foreach (var group in duplicateGroups)
                        {
                            foreach (var ad in group)
                                addressList.Remove(ad);
                        }

                        var errorList = duplicateGroups
                            .Select(g => $"[ {g.Key} ]点位名存在{g.Count()}个重复，请检查后重新订阅当前点位（此操作为保证当前点位数据正确性）")
                            .ToList();

                        OnInfoEventHandler(this, EventInfoResult.CreateFailureResult(errorList.ToJson()));
                    }

                    // 执行读取函数（加锁保护函数与地址访问）
                    OperateResult result;
                    lock (basics.Address)
                    {
                        result = basics.Function?.Invoke(basics.Address) ?? OperateResult.CreateFailureResult("未设置读取函数");
                    }

                    if (!result.Status)
                    {
                        OnInfoEventHandler(this, EventInfoResult.CreateFailureResult($"自定义订阅轮询异常：{result.Message}"));
                        continue;
                    }

                    // 处理读取结果
                    switch (result.ResultData)
                    {
                        // 处理ConcurrentDictionary<string, AddressValue>结果
                        case ConcurrentDictionary<string, AddressValue> resultDict when resultDict.Count > 0:
                            if (basics.ChangeOut)
                            {
                                if (!Comparison(DataCachePool, resultDict))
                                {
                                    if (basics.AllOut)
                                    {
                                        OnDataEventHandler(this, new EventDataResult(true, "存在变化数据", resultDict));
                                    }
                                    else
                                    {
                                        foreach (var item in resultDict.Values)
                                            await DataQueue.Writer.WriteAsync(item, token).ConfigureAwait(false);
                                    }

                                    DataCachePool = resultDict;
                                }
                            }
                            else
                            {
                                OnDataEventHandler(this, new EventDataResult(true, "实时数据", resultDict));
                            }
                            break;
                        // 处理List<ConcurrentDictionary<string, AddressValue>>结果
                        case List<ConcurrentDictionary<string, AddressValue>> resultList when resultList.Count > 0:
                            if (basics.ChangeOut)
                            {
                                if (!Comparison(DataCachePoolArray, resultList))
                                {
                                    OnDataEventHandler(this, new EventDataResult(true, "存在变化数据", resultList));
                                    DataCachePoolArray = resultList;
                                }
                            }
                            else
                            {
                                OnDataEventHandler(this, new EventDataResult(true, "实时数据", resultList));
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    OnInfoEventHandler(this, EventInfoResult.CreateFailureResult($"自定义订阅轮询异常：{ex.Message}"));
                }
                finally
                {
                    await Task.Delay(basics.HandleInterval, token).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// 订阅服务
        /// </summary>
        private SubscribeService<AddressValue> subscribeService = SubscribeService<AddressValue>.Instance(Guid.NewGuid().ToUpperNString());

        /// <summary>
        /// 数据队列包
        /// </summary>
        private Channel<AddressValue>? DataQueuePack = null;
        /// <summary>
        /// 数据队列包生命周期
        /// </summary>
        private CancellationTokenSource? DataQueuePackToken = null;
        /// <summary>
        /// 任务处理
        /// </summary>
        /// <param name="token">任务令牌</param>
        /// <returns>任务</returns>
        private async Task TaskPackHandle(CancellationToken token)
        {
            try
            {
                while (await DataQueuePack.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    ConcurrentDictionary<string, AddressValue> param = new ConcurrentDictionary<string, AddressValue>();
                    while (DataQueuePack.Reader.TryRead(out var queueData))
                    {
                        if (queueData != null && !token.IsCancellationRequested)
                        {
                            param.AddOrUpdate(queueData.AddressName, queueData, (k, v) => queueData);
                        }
                    }
                    if (param.Count > 0)
                    {
                        OnDataEventHandler(this, new EventDataResult(true, "变化数据", param));
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }

        /// <summary>
        /// 订阅服务事件
        /// </summary>
        /// <param name="sender">源</param>
        /// <param name="e">事件结果</param>
        /// <exception cref="NotImplementedException"></exception>
        private async Task SubscribeService_OnDataEventAsync(object? sender, EventDataResult e)
        {
            SubscribeSource<AddressValue>? source = e.GetSource<SubscribeSource<AddressValue>>();
            if (source != null)
            {
                await DataQueuePack.Writer.WriteAsync(source.Source).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 任务处理
        /// </summary>
        /// <param name="token">任务令牌</param>
        /// <returns>任务</returns>
        private async Task TaskHandle(CancellationToken token)
        {
            try
            {
                while (await DataQueue.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (DataQueue.Reader.TryRead(out var queueData))
                    {
                        if (queueData != null && !token.IsCancellationRequested)
                        {
                            await subscribeService.SetAsync(queueData.AddressName, queueData, token);
                        }
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }

        /// <summary>
        /// 线程安全字典比对，是否一致
        /// </summary>
        /// <param name="Param1">参数1</param>
        /// <param name="Param2">参数2</param>
        /// <returns>是否一致</returns>
        private bool Comparison(ConcurrentDictionary<string, AddressValue> Param1, ConcurrentDictionary<string, AddressValue> Param2)
        {
            bool equal = false;
            if (Param1.Count == Param2.Count)
            {
                equal = true;
                foreach (var pair in Param1)
                {
                    AddressValue value;
                    if (Param2.TryGetValue(pair.Key, out value))
                    {
                        if (!value.Equals(pair.Value))
                        {
                            equal = false;
                            break;
                        }
                    }
                    else
                    {
                        equal = false;
                        break;
                    }
                }
            }
            return equal;
        }

        /// <summary>
        /// 数组线程安全字典比对，是否一致
        /// </summary>
        /// <param name="Param1">参数1</param>
        /// <param name="Param2">参数2</param>
        /// <returns>是否一致</returns>
        private bool Comparison(List<ConcurrentDictionary<string, AddressValue>> Param1, List<ConcurrentDictionary<string, AddressValue>> Param2)
        {
            foreach (ConcurrentDictionary<string, AddressValue> a in Param2)
            {
                if (!Param1.Exists(b => Comparison(a, b)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            Off(true);
            base.Dispose();
        }
        /// <inheritdoc/>
        public override async Task DisposeAsync()
        {
            await OffAsync(true);
            await base.DisposeAsync();
        }
        /// <inheritdoc/>
        public OperateResult Subscribe(Address address)
        {
            string mName = BegOperate();
            try
            {
                GoOn = false;
                lock (basics.Address)  //锁住不让其他操作
                {
                    //检索历史项与新进项是否存在相同点位名的地址
                    //移除历史中与新进项点位名一致的点,保留新进项点位
                    basics.Address.AddressArray.RemoveAll(a => basics.Address.AddressArray.Where(a => address.AddressArray.Any(b => b.AddressName == a.AddressName)).ToArray().Any(b => b.AddressName == a.AddressName));
                    //移除同批相同点,并订阅剩下的点位
                    return RemoveIdenticalItem(address, mName);
                }
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <summary>
        /// 移除同批相同项
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="mName">方法名</param>
        /// <returns></returns>
        private OperateResult RemoveIdenticalItem(Address address, string mName)
        {
            //失败信息
            List<string> failMessage = new List<string>();
            //检测是否有地址名相同,但数据类型或地址类型不一致,如果存在则移除 地址名相同的,并返回异常
            IGrouping<string?, AddressDetails>[] checkList = address.AddressArray.GroupBy(p => p.AddressName).Where(g => g.Count() > 1).ToArray();
            if (checkList.Any())
            {
                //存在相同的,移除相同的
                foreach (var item in checkList)
                {
                    foreach (var ad in item)
                    {
                        address.AddressArray.Remove(ad);
                    }
                }

                foreach (var item in checkList)
                {
                    failMessage.Add($"[ {item.Key} ]点位名存在{item.Count()}个重复，请检查后重新订阅当前点位（此操作为保证当前点位数据正确性）");
                }
            }
            //没触发移除的直接添加订阅
            if (address.AddressArray.Count > 0)
            {
                //移除重名,并把没有重名的添加订阅
                //添加新节点
                basics.Address.AddressArray.AddRange(address.AddressArray);
                //Distinct()去重
                basics.Address.AddressArray = basics.Address.AddressArray.Distinct().ToList();
            }
            //存在移除新增直接抛出
            if (failMessage.Count > 0)
            {
                GoOn = true;
                return EndOperate(false, $"订阅存在失败信息：{failMessage.ToJson()}", failMessage, methodName: mName);
            }

            GoOn = true;
            return EndOperate(true, methodName: mName);
        }

        /// <inheritdoc/>
        public OperateResult UnSubscribe(Address address)
        {
            //开始记录运行时间
            BegOperate();
            try
            {
                GoOn = false;
                //失败消息
                List<string> FillMessage = new List<string>();
                lock (basics.Address)  //锁住不让其他操作
                {
                    basics.Address.AddressArray.RemoveAll(a => address.AddressArray.Any(b => b.AddressName == a.AddressName));
                }
                GoOn = true;
                if (FillMessage.Count > 0)
                {
                    return EndOperate(false, FillMessage.ToJson());
                }
                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public OperateResult On()
        {
            //开始记录运行时间
            BegOperate();
            try
            {
                if (MonitorSwitch == null)
                {
                    MonitorSwitch = new CancellationTokenSource();

                    //当队列为空，初始化队列
                    if (DataQueue == null)
                    {
                        DataQueue = Channel.CreateBounded<AddressValue>(channelOptions);
                    }
                    //轮询执行动作任务
                    Polling(MonitorSwitch.Token);

                    if (DataQueuePack == null)
                    {
                        DataQueuePack = Channel.CreateBounded<AddressValue>(channelOptions);
                    }
                    if (DataQueuePackToken == null)
                    {
                        DataQueuePackToken = new CancellationTokenSource();
                        //直接执行任务
                        TaskPackHandle(DataQueuePackToken.Token);
                    }

                    //任务为空创建任务
                    if (TaskArray == null)
                    {
                        TaskArray = new ConcurrentDictionary<CancellationTokenSource, Task>();
                        //创建任务
                        for (int i = 0; i < basics.TaskNumber; i++)
                        {
                            CancellationTokenSource token = new CancellationTokenSource();
                            TaskArray.TryAdd(token, TaskHandle(token.Token));
                        }
                    }
                    //事件注册
                    subscribeService.OnDataEventAsync -= SubscribeService_OnDataEventAsync;
                    subscribeService.OnDataEventAsync += SubscribeService_OnDataEventAsync;
                    return EndOperate(true);
                }
                else
                {
                    return EndOperate(false, "已启动");
                }
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public OperateResult Off(bool hardClose = false)
        {
            //开始记录运行时间
            BegOperate();
            try
            {
                //立马停止轮询
                GoOn = false;
                //监控开关关闭
                if (MonitorSwitch != null)
                {
                    MonitorSwitch.Cancel();
                }
                if (DataQueuePackToken != null)
                {
                    //取消任务
                    DataQueuePackToken.Cancel();
                }
                //任务清空
                if (TaskArray != null)
                {
                    foreach (var item in TaskArray)
                    {
                        item.Key.Cancel();
                    }
                    TaskArray.Clear();
                    TaskArray = null;
                }
                //队列数据清空
                if (DataQueue != null)
                {
                    DataQueue.Writer.Complete();
                    while (DataQueue.Reader.TryRead(out _)) { }
                    DataQueue = null;
                }
                //队列数据清空
                if (DataQueuePack != null)
                {
                    DataQueuePack.Writer.Complete();
                    while (DataQueuePack.Reader.TryRead(out _)) { }
                    DataQueuePack = null;
                }

                //事件注册
                subscribeService.OnDataEventAsync -= SubscribeService_OnDataEventAsync;

                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public OperateResult GetStatus()
        {
            BegOperate();
            if (DataQueue == null && TaskArray == null)
            {
                return EndOperate(false, "未订阅", logOutput: false);
            }
            else
            {
                return EndOperate(true, "已订阅", logOutput: false);
            }
        }
        /// <inheritdoc/>
        public async Task<OperateResult> SubscribeAsync(Address address, CancellationToken token = default) => await Task.Run(() => Subscribe(address));
        /// <inheritdoc/>
        public async Task<OperateResult> UnSubscribeAsync(Address address, CancellationToken token = default) => await Task.Run(() => UnSubscribe(address));
        /// <inheritdoc/>
        public async Task<OperateResult> OnAsync(CancellationToken token = default) => await Task.Run(() => On(), token);
        /// <inheritdoc/>
        public async Task<OperateResult> OffAsync(bool hardClose = false, CancellationToken token = default) => await Task.Run(() => Off(hardClose), token);
        /// <inheritdoc/>
        public async Task<OperateResult> GetStatusAsync(CancellationToken token = default) => await Task.Run(() => GetStatus(), token);
    }
}