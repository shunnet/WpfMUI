using System.ComponentModel;

namespace Snet.Core.communication.net.ws.client
{
    public class WsClientData
    {
        /// <summary>
        /// 基础数据
        /// </summary>
        public class Basics : BasicsData
        {
            /// <summary>
            /// 主机地址
            /// </summary>
            [Category("基础数据")]
            [Description("主机地址")]
            public string? Host { get; set; } = "ws://127.0.0.1:6688/";

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
            /// 超时时间
            /// </summary>
            [Description("超时时间")]
            public int Timeout { get; set; } = 1000;
        }
    }
}