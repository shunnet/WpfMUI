using Snet.Model.data;
using Snet.Unility;
using System.ComponentModel;
using System.Net;
namespace Snet.Core.communication.net.http.service
{
    /// <summary>
    /// Http服务端数据
    /// </summary>
    public class HttpServiceData
    {
        /// <summary>
        /// 基础数据
        /// </summary>
        public class Basics : WAModel
        {
            /// <summary>
            /// 唯一标识符
            /// </summary>
            [Description("唯一标识符")]
            public string SN { get; set; } = Guid.NewGuid().ToUpperNString();

            /// <summary>
            /// 请求与响应的方式
            /// </summary>
            [Description("请求与响应的方式")]
            public HttpMethod Method { get; set; } = HttpMethod.Get;

            /// <summary>
            /// 请求与响应的内容类型
            /// </summary>
            [Description("请求与响应的内容类型")]
            public string ContentType { get; set; } = "application/json";

            /// <summary>
            /// 接口的绝对路径集合
            /// </summary>
            [Description("接口的绝对路径集合")]
            public List<string> AbsolutePaths { get; set; } = new List<string>()
            {
                "/api/sample1",
                "/api/sample2",
            };

            /// <summary>
            /// 快速设置API模型参数
            /// </summary>
            /// <param name="wAModel">WA模型</param>
            public void SET(WAModel wAModel)
            {
                this.CrossDomain = wAModel.CrossDomain;
                this.IpAddress = wAModel.IpAddress;
                this.Port = wAModel.Port;
            }
        }
        /// <summary>
        /// 等待处理
        /// </summary>
        public class WaitHandler
        {
            /// <summary>
            /// 请求的数据
            /// </summary>
            public HttpListenerRequest Request { get; set; }

            /// <summary>
            /// 响应的数据
            /// </summary>
            public HttpListenerResponse Response { get; set; }

            /// <summary>
            /// Body数据
            /// </summary>
            public string? BodyData { get; set; }
        }
    }
}
