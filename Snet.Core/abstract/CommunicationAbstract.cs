using Snet.Core.extend;
using Snet.Model.data;
using Snet.Model.@interface;

namespace Snet.Core.@abstract
{
    /// <summary>
    /// 底层通信抽象类；<br/>
    /// 外部必须实现抽象方法；<br/>
    /// 实现接口的异步方法；<br/>
    /// 继承核心抽象类
    /// </summary>
    /// <typeparam name="O">操作类</typeparam>
    /// <typeparam name="D">基础数据类，构造参数类</typeparam>
    public abstract class CommunicationAbstract<O, D> : CoreUnify<O, D>, ICommunication
        where O : class
        where D : class
    {
        /// <summary>
        /// 无惨构造函数
        /// </summary>
        protected CommunicationAbstract() : base() { }

        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="param">参数</param>
        protected CommunicationAbstract(D param) : base(param) { }

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
        public abstract OperateResult GetBaseObject();
        /// <inheritdoc/>
        public abstract OperateResult GetStatus();
        /// <inheritdoc/>
        public abstract OperateResult Off(bool hardClose = false);
        /// <inheritdoc/>
        public abstract OperateResult On();
        /// <inheritdoc/>
        public abstract OperateResult Send(byte[] data);
        /// <inheritdoc/>
        public abstract OperateResult SendWait(byte[] data, CancellationToken token);

        /// <inheritdoc/>
        public async Task<OperateResult> GetBaseObjectAsync(CancellationToken token = default)
            => await Task.Run(() => GetBaseObject(), token);
        /// <inheritdoc/>
        public async Task<OperateResult> GetStatusAsync(CancellationToken token = default)
            => await Task.Run(() => GetStatus(), token);
        /// <inheritdoc/>
        public async Task<OperateResult> OffAsync(bool hardClose = false, CancellationToken token = default)
            => await Task.Run(() => Off(hardClose), token);
        /// <inheritdoc/>
        public async Task<OperateResult> OnAsync(CancellationToken token = default)
            => await Task.Run(() => On(), token);
        /// <inheritdoc/>
        public async Task<OperateResult> SendAsync(byte[] data, CancellationToken token = default)
            => await Task.Run(() => Send(data), token);
        /// <inheritdoc/>
        public async Task<OperateResult> SendWaitAsync(byte[] data, CancellationToken token)
            => await Task.Run(() => SendWait(data, token), token);
    }
}
