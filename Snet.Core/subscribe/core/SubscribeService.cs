using Snet.Core.extend;
using Snet.Model.data;
using System.Collections.Concurrent;

namespace Snet.Core.subscribe.core
{
    /// <summary>
    /// 订阅服务数据层，与调用者共存亡<br/>
    /// 确保SN 是唯一，不然会导致数据冲突
    /// </summary>
    public class SubscribeService<T> : CoreUnify<SubscribeService<T>, String>, IDisposable
    {
        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="basics">基础数据</param>
        public SubscribeService(String basics) : base(basics) { }

        /// <summary>
        /// 数据源
        /// </summary>
        private ConcurrentDictionary<string, SubscribeSource<T>> Source = new ConcurrentDictionary<string, SubscribeSource<T>>();

        /// <inheritdoc/>
        public override void Dispose()
        {
            foreach (var item in Source)
            {
                item.Value.Dispose();
            }
            Source.Clear();
            base.Dispose();
        }

        /// <inheritdoc/>
        public override async Task DisposeAsync()
        {
            foreach (var item in Source)
            {
                await item.Value.DisposeAsync();
            }
            Source.Clear();
            await base.DisposeAsync();
        }

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="SN">唯一标识符</param>
        /// <returns>设置的啥类型，返回啥类型</returns>
        public OperateResult Get(string SN)
        {
            if (objList == null)
            {
                throw new Exception("please use singleton mode");
            }
            if (Source.ContainsKey(SN))
            {
                return Source[SN].Get();
            }
            return EndOperate(false, $"({SN}) Instance does not exist", methodName: BegOperate());
        }

        /// <summary>
        /// 设置数据异步
        /// </summary>
        /// <param name="SN">唯一标识符</param>
        /// <param name="token">令牌</param>
        /// <returns>设置的啥类型，返回啥类型</returns>
        public async Task<OperateResult> GetAsync(string SN, CancellationToken token = default) => await Task.Run(() => Get(SN), token);

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="SN">唯一标识符</param>
        /// <param name="Data">数据</param>
        /// <returns>统一结果</returns>
        public OperateResult Set(string SN, T Data)
        {
            if (objList == null)
            {
                throw new Exception("please use singleton mode");
            }
            BegOperate();
            if (!Source.ContainsKey(SN))
            {
                SubscribeSource<T> core = new SubscribeSource<T>(SN);
                core.OnDataEvent += OnDataEventHandler;
                Source.AddOrUpdate(SN, core, (k, v) => core);
            }
            Source[SN].Set(Data);
            return EndOperate(true);
        }

        /// <summary>
        /// 设置数据异步
        /// </summary>
        /// <param name="SN">唯一标识符</param>
        /// <param name="Data">数据</param>
        /// <param name="token">令牌</param>
        /// <returns>统一结果</returns>
        public async Task<OperateResult> SetAsync(string SN, T Data, CancellationToken token = default) => await Task.Run(() => Set(SN, Data), token);
    }
}