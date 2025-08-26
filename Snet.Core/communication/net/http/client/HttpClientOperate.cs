using Snet.Core.extend;
using Snet.Model.data;
using System.Net;
using static Snet.Core.communication.net.http.client.HttpClientData;

namespace Snet.Core.communication.net.http.client
{
    /// <summary>
    /// HTTP客户端操作
    /// </summary>
    public class HttpClientOperate : CoreUnify<HttpClientOperate, Basics>, IDisposable
    {
        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="basics">基础数据</param>
        public HttpClientOperate(Basics basics) : base(basics) { }

        /// <summary>
        /// 无参构造函数
        /// </summary>
        public HttpClientOperate() : base()
        {
            basics = new Basics();
        }
        /// <summary>
        /// 请求
        /// </summary>
        /// <param name="requestData">请求数据</param>
        /// <returns>统一出参</returns>
        public OperateResult Request(RequestData requestData)
        {
            BegOperate();
            try
            {
                bool UseProxy = requestData.Proxy != null;
                bool UseCookies = requestData.CookieContainer.Count > 0;
                using (HttpClientHandler httpClientHandler = new HttpClientHandler { UseCookies = UseCookies, CookieContainer = requestData.CookieContainer, UseProxy = UseProxy, Proxy = requestData.Proxy, MaxConnectionsPerServer = basics.MaxConnectCount })
                using (HttpClient httpClient = new HttpClient(httpClientHandler))
                {
                    //设置超时时间
                    httpClient.Timeout = requestData.TimeOut;

                    //实例化对象，设置请求数据
                    HttpRequestMessage request = new HttpRequestMessage(requestData.Method, requestData.Url);

                    //设置请求头
                    if (requestData.Headers != null)
                    {
                        foreach (var item in requestData.Headers)
                        {
                            request.Headers.Add(item.Key, item.Value);
                        }
                    }

                    //内容赋值
                    switch (requestData.BodyType)
                    {
                        case BType.FormData:
                            MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
                            if (requestData.BodyContent != null)
                            {
                                foreach (var item in requestData.BodyContent as Dictionary<string, string>)
                                {
                                    multipartFormDataContent.Add(new StringContent(item.Value), item.Key);
                                }
                            }
                            if (multipartFormDataContent != null && multipartFormDataContent.Count() > 0)
                            {
                                multipartFormDataContent.Headers.ContentType.MediaType = requestData.ContentType;
                                request.Content = multipartFormDataContent;
                            }
                            break;

                        case BType.Raw:
                            //设置请求参数
                            string data = requestData.BodyContent as string;
                            if (!string.IsNullOrEmpty(data))
                            {
                                request.Content = new StringContent(data, requestData.Encoding, requestData.ContentType);  //设置post请求参数
                            }
                            break;
                    }
                    //数据发送
                    HttpResponseMessage response = httpClient.Send(request);
                    //获取响应流
                    StreamReader streamReader = new StreamReader(response.Content.ReadAsStream(), requestData.Encoding);
                    //获取Cookie
                    CookieCollection cookies = new CookieCollection();
                    //转换响应头数据
                    Dictionary<string, string>? ResponseHeadersData = DisposeResponseData(response.Headers.ToString(), ref cookies, requestData.Url);
                    //响应数据
                    string? ResponseData = streamReader.ReadToEnd();
                    //状态码
                    int StateCode = (int)response.StatusCode;
                    //关闭
                    streamReader.Close();
                    //释放
                    streamReader.Dispose();
                    //返回数据
                    return EndOperate(true, GetLanguageValue("请求成功"), new ResponseData() { ResData = ResponseData, ResHeadersDatas = ResponseHeadersData, StatusCode = StateCode, ReqData = requestData, ResCookieData = cookies });
                }
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message);
            }
        }

        /// <summary>
        /// 请求
        /// </summary>
        /// <param name="requestData">请求数据</param>
        /// <returns>统一出参</returns>
        public async Task<OperateResult> RequestAsync(RequestData requestData, CancellationToken token = default) => await Task.Run(() => Request(requestData), token);

        /// <summary>
        /// 解析响应数据
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        private Dictionary<string, string>? DisposeResponseData(string Data, ref CookieCollection cookieContainer, string url)
        {
            string[] Datas = Data.Split(new char[] { '\r', '\n' });
            if (Datas.Length > 0)
            {
                Dictionary<string, string> tuples = new Dictionary<string, string>();
                foreach (var item in Datas)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        string[] infos = item.Split(new char[] { ':' });
                        tuples.Add(infos[0], infos[1]);
                        if (infos[0].Equals("Set-Cookie"))
                        {
                            string[] strings = infos[1].Split(';');
                            string[] KV1 = strings[0].Split('=');
                            url = url.Replace("https:", "").Replace("http:", "").Replace("/", "");
                            cookieContainer.Add(new Cookie(KV1[0].Trim(), KV1[1].Trim(), "/", url.Trim()));
                        }
                    }
                }
                return tuples;
            }
            return null;
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
        }
        /// <inheritdoc/>
        public override async Task DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }
}