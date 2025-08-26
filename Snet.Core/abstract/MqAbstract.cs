using Snet.Core.extend;
using Snet.Model.data;
using Snet.Model.@interface;
using System.Text;

namespace Snet.Core.@abstract
{
    /// <summary>
    /// 消息中间件抽象类；<br/>
    /// 外部必须实现抽象方法；<br/>
    /// 实现接口的异步方法；<br/>
    /// 继承核心抽象类
    /// </summary>
    /// <typeparam name="O">操作类</typeparam>
    /// <typeparam name="D">基础数据类，构造参数类</typeparam>
    public abstract class MqAbstract<O, D> : CoreUnify<O, D>, IMq
        where O : class
        where D : class
    {
        /// <summary>
        /// 无惨构造函数
        /// </summary>
        protected MqAbstract() : base() { }

        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="param">参数</param>
        protected MqAbstract(D param) : base(param) { }

        /// <inheritdoc/>
        public abstract OperateResult GetStatus();
        /// <inheritdoc/>
        public abstract OperateResult Off(bool hardClose = false);
        /// <inheritdoc/>
        public abstract OperateResult On();
        /// <inheritdoc/>
        public virtual OperateResult Produce(string topic, string content, Encoding? encoding = null)
            => Produce(topic, encoding == null ? Encoding.UTF8.GetBytes(content) : encoding.GetBytes(content));
        /// <inheritdoc/>
        public abstract OperateResult Produce(string topic, byte[] content);
        /// <inheritdoc/>
        public abstract OperateResult Consume(string topic);
        /// <inheritdoc/>
        public abstract OperateResult UnConsume(string topic);


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
        public async Task<OperateResult> ProduceAsync(string topic, string content, Encoding? encoding = null, CancellationToken token = default)
            => await Task.Run(() => Produce(topic, content, encoding), token);
        /// <inheritdoc/>
        public async Task<OperateResult> ProduceAsync(string topic, byte[] content, CancellationToken token = default)
            => await Task.Run(() => Produce(topic, content), token);
        /// <inheritdoc/>
        public async Task<OperateResult> ConsumeAsync(string topic, CancellationToken token = default)
            => await Task.Run(() => Consume(topic), token);
        /// <inheritdoc/>
        public async Task<OperateResult> UnConsumeAsync(string topic, CancellationToken token = default)
            => await Task.Run(() => UnConsume(topic), token);

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
    }
}
