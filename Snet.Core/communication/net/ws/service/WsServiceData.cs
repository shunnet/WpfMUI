using Snet.Unility;
using System.ComponentModel;

namespace Snet.Core.communication.net.ws.service
{
    public class WsServiceData
    {
        /// <summary>
        /// 基础数据
        /// </summary>
        public class Basics
        {
            /// <summary>
            /// 唯一标识符
            /// </summary>
            [Category("基础数据")]
            [Description("唯一标识符")]
            public string? SN { get; set; } = Guid.NewGuid().ToUpperNString();

            /// <summary>
            /// 地址
            /// </summary>
            [Description("地址")]
            public string Host { get; set; } = "ws://127.0.0.1:6688/";

            /// <summary>
            /// 最大块大小;
            /// 如果数据超过限定值则自动分包发送
            /// </summary>
            [Description("最大块大小")]
            public int MaxChunkSize { get; set; } = byte.MaxValue * 1024;

            /// <summary>
            /// 重试发送次数;
            /// 只在分包发送中触发;
            /// 在限定次数中还是失败则直接返回失败
            /// </summary>
            [Description("重试发送次数")]
            public int RetrySendCount { get; set; } = 5;

            /// <summary>
            /// 超时时间
            /// </summary>
            [Description("超时时间")]
            public int Timeout { get; set; } = 1000;

            /// <summary>
            /// 数据缓冲区大小
            /// </summary>
            [Description("数据缓冲区大小")]
            public int BufferSize { get; set; } = 1024 * 1024;
        }

        /// <summary>
        /// 客户端消息
        /// </summary>
        public class ClientMessage
        {
            /// <summary>
            /// 步骤
            /// </summary>
            [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
            public Steps Step { get; set; }

            /// <summary>
            /// IP地址:端口
            /// </summary>
            public string IpPort { get; set; }

            /// <summary>
            /// 字节数据
            /// </summary>
            public byte[]? Bytes { get; set; } = null;
        }

        /// <summary>
        /// 步骤枚举
        /// </summary>
        public enum Steps
        {
            客户端连接,
            客户端断开,
            消息接收
        }
    }
}