using Snet.Unility;
using System.ComponentModel;
using System.Net;
using System.Text;

namespace Snet.Core.communication.net.http.client
{
    /// <summary>
    /// HTTP数据
    /// </summary>
    public class HttpClientData
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
            public string SN { get; set; } = Guid.NewGuid().ToUpperNString();

            /// <summary>
            /// 最大连接数
            /// </summary>
            [Description("最大连接数")]
            public int MaxConnectCount { get; set; } = 1000;
        }

        /// <summary>
        /// 内容数据类型
        /// </summary>
        public enum BType
        {
            /// <summary>
            /// 表单形式传输数据；<br/>
            /// ContentType:multipart/form-data
            /// </summary>
            [Description("表单形式传输数据")]
            FormData,
            /// <summary>
            /// 原始数据形式传输数据；<br/>
            /// 基本使用JSON数据传输；<br/>
            /// ContentType:application/json
            /// </summary>
            [Description("原始数据形式传输数据")]
            Raw,
            /// <summary>
            /// 无内容
            /// </summary>
            [Description("无内容")]
            None
        }

        /// <summary>
        /// 请求的数据
        /// </summary>
        public class RequestData
        {
            /// <summary>
            /// 请求的链接
            /// </summary>
            public string Url { get; set; } = "http://127.0.0.1:6688/api/sample1";

            /// <summary>
            /// 请求标头
            /// </summary>
            public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

            /// <summary>
            /// 请求方式
            /// </summary>
            public HttpMethod Method { get; set; } = HttpMethod.Get;

            /// <summary>
            /// 内容数据类型
            /// </summary>
            public BType BodyType { get; set; } = BType.Raw;

            /// <summary>
            /// 内容；<br/>
            /// string，配合 Raw 类型；<br/>
            /// Dictionary＜string, string＞，配合 FormData 类型
            /// </summary>
            public object BodyContent { get; set; } = null;

            /// <summary>
            /// 编码格式
            /// </summary>
            public Encoding Encoding { get; set; } = Encoding.UTF8;

            /// <summary>
            /// 内容类型
            /// </summary>
            public string ContentType { get; set; } = "application/json";

            /// <summary>
            /// 超时时间
            /// </summary>
            public TimeSpan TimeOut { get; set; } = new TimeSpan(0, 0, 60);

            /// <summary>
            /// 代理
            /// </summary>
            public IWebProxy? Proxy { get; set; } = null;

            /// <summary>
            /// 缓存
            /// </summary>
            public CookieContainer CookieContainer { get; set; } = new CookieContainer();
        }

        /// <summary>
        /// 响应数据
        /// </summary>
        public class ResponseData
        {
            /// <summary>
            /// 状态码
            /// </summary>
            public int StatusCode { get; set; }

            /// <summary>
            /// 响应头数据集合
            /// </summary>
            public Dictionary<string, string>? ResHeadersDatas { get; set; }

            /// <summary>
            /// 响应的COOKIE
            /// </summary>
            public CookieCollection ResCookieData { get; set; } = new CookieCollection();

            /// <summary>
            /// 响应的数据
            /// </summary>
            public string? ResData { get; set; }

            /// <summary>
            /// 请求的数据
            /// </summary>
            public RequestData ReqData { get; set; }
        }
    }
}