using Snet.Unility;
using System.ComponentModel;

namespace Snet.Core.communication
{
    /// <summary>
    /// 基础数据
    /// </summary>
    public class BasicsData
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        [Category("基础数据")]
        [Description("唯一标识符")]
        public string? SN { get; set; } = Guid.NewGuid().ToUpperNString();

        /// <summary>
        /// 发送等待间隔<br/>
        /// 继承者可选重写
        /// </summary>
        [Description("发送等待间隔")]
        public virtual int SendWaitInterval { get; set; } = 5000;

        /// <summary>
        /// 最大块大小；<br/>
        /// 如果数据超过限定值则自动分包发送<br/>
        /// 继承者可选重写
        /// </summary>
        [Description("最大块大小")]
        public virtual int MaxChunkSize { get; set; } = byte.MaxValue * 1024;

        /// <summary>
        /// 重试发送次数；<br/>
        /// 只在分包发送中触发；<br/>
        /// 在限定次数中还是失败则直接返回失败<br/>
        /// 继承者可选重写
        /// </summary>
        [Description("重试发送次数")]
        public virtual int RetrySendCount { get; set; } = 5;

        /// <summary>
        /// 数据缓冲区大小<br/>
        /// 继承者可选重写
        /// </summary>
        [Description("数据缓冲区大小")]
        public virtual int BufferSize { get; set; } = 1024 * 1024;
    }
}