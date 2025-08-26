using System.ComponentModel;

namespace Snet.Core.communication.net.tcp.client
{
    public class TcpClientData
    {
        /// <summary>
        /// 基础数据
        /// </summary>
        public class Basics : BasicsData
        {
            /// <summary>
            /// Ip地址
            /// </summary>
            [Category("基础数据")]
            [Description("Ip地址")]
            public string? IpAddress { get; set; } = "127.0.0.1";

            /// <summary>
            /// 端口
            /// </summary>
            [Description("端口")]
            public int Port { get; set; } = 6688;

            /// <summary>
            /// 是否需要断开重新连接
            /// </summary>
            [Description("是否需要断开重新连接")]
            public bool InterruptReconnection { get; set; } = true;

            /// <summary>
            /// 重连间隔(毫秒)
            /// </summary>
            [Description("重连间隔(毫秒)")]
            public int ReconnectionInterval { get; set; } = 2000;

            /// <summary>
            /// 超时时间(毫秒)
            /// </summary>
            [Description("超时时间(毫秒)")]
            public int Timeout { get; set; } = 1000;
        }
    }
}