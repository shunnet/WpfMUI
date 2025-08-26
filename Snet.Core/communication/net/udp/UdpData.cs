using System.ComponentModel;

namespace Snet.Core.communication.net.udp
{
    public class UdpData
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
            /// 广播模式<br/>
            /// 启动后无法指定与远程地址通信
            /// </summary>
            [Description("是否启用广播模式")]
            public bool EnableBroadcast { get; set; } = true;

            /// <summary>
            /// 远程Ip地址<br/>
            /// 与指定目标的远程地址通信
            /// </summary>
            [Description("远程Ip地址")]
            public string? RemoteIpAddress { get; set; }

            /// <summary>
            /// 远程端口<br/>
            /// 与指定目标的远程地址通信
            /// </summary>
            [Description("远程端口")]
            public int RemotePort { get; set; }
        }

        /// <summary>
        /// 终端数据
        /// </summary>
        public class TerminalMessage
        {
            /// <summary>
            /// IP端口
            /// </summary>
            public string IpPort { get; set; }

            /// <summary>
            /// 数据
            /// </summary>
            public byte[]? Bytes { get; set; }
        }
    }
}