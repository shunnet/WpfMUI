using System.ComponentModel;
using System.IO.Ports;

namespace Snet.Core.communication.serial
{
    /// <summary>
    /// 串口数据
    /// </summary>
    public class SerialData
    {
        /// <summary>
        /// 串口通信基础数据
        /// </summary>
        public class Basics : BasicsData
        {
            /// <summary>
            /// 串口号
            /// </summary>
            [Category("基础数据")]
            [Description("串口号")]
            public string? PortName { get; set; } = "COM1";

            /// <summary>
            /// 波特率
            /// </summary>
            [Description("波特率")]
            public int BaudRate { get; set; } = 19200;

            /// <summary>
            /// 校验位
            /// </summary>
            [Description("校验位")]
            [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
            public Parity ParityBit { get; set; } = Parity.Even;

            /// <summary>
            /// 数据位
            /// </summary>
            [Description("数据位")]
            public int DataBit { get; set; } = 8;

            /// <summary>
            /// 停止位
            /// </summary>
            [Description("停止位")]
            [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
            public StopBits StopBit { get; set; } = StopBits.One;

            /// <summary>
            /// 写入超时时间
            /// </summary>
            [Description("写入超时时间")]
            public int WriteTimeout { get; set; } = 1000;

            /// <summary>
            /// 读取超时时间
            /// </summary>
            [Description("读取超时时间")]
            public int ReadTimeout { get; set; } = 1000;

            /// <summary>
            /// 接收缓冲区中数据的字节数阈值
            /// </summary>
            [Description("接收缓冲区中数据的字节数阈值")]
            public int ReceivedBytesThreshold { get; set; } = 1;
        }
    }
}