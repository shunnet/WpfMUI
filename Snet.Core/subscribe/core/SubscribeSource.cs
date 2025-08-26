using Snet.Core.extend;
using Snet.Model.data;
using Snet.Unility;

namespace Snet.Core.subscribe.core
{
    /// <summary>
    /// 订阅数据库
    /// </summary>
    public class SubscribeSource<T> : CoreUnify<SubscribeSource<T>, String>, IDisposable
    {
        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="basics">基础数据</param>
        public SubscribeSource(String basics) : base(basics) { }


        /// <summary>
        /// 数据源
        /// </summary>
        public T Source { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <typeparam name="T">对象</typeparam>
        /// <param name="Data">数据</param>
        /// <returns>统一结果</returns>
        public OperateResult Set(T Data)
        {
            BegOperate();
            UpdateTime = DateTime.Now;
            if (!Data.Comparer(Source, new string[] { "Time" }).result)
            {
                Source = Data;
                OnDataEventHandler(this, new EventDataResult(true, "Data Update", this));
            }
            return EndOperate(true);
        }

        /// <summary>
        /// 设置数据异步
        /// </summary>
        /// <param name="Data">数据</param>
        /// <returns>统一结果</returns>
        public async Task<OperateResult> SetAsync(T Data) => await Task.Run(() => Set(Data));

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <returns>设置的啥类型，返回啥类型</returns>
        public OperateResult Get()
        {
            return EndOperate(true, "Get Succeed", this, methodName: BegOperate());
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <returns>设置的啥类型，返回啥类型</returns>
        public async Task<OperateResult> GetAsync() => await Task.Run(() => Get());

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
        }

        /// <inheritdoc/>
        public override async Task DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }
}