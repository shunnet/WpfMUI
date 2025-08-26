using Snet.Core.extend;
using Snet.Model.data;
using Snet.Model.@interface;
using Snet.Unility;
using System.Net;
using System.Text;
using static Snet.Core.communication.net.http.service.HttpServiceData;

namespace Snet.Core.communication.net.http.service
{
    /// <summary>
    /// Http服务端操作
    /// </summary>
    public class HttpServiceOperate : CoreUnify<HttpServiceOperate, Basics>, IOn, IOff, IGetStatus, IDisposable
    {
        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="basics">基础数据</param>
        public HttpServiceOperate(Basics basics) : base(basics) { }

        /// <summary>
        /// 无参构造函数
        /// </summary>
        public HttpServiceOperate() : base()
        {
            basics = new Basics();
        }
        /// <summary>
        /// Http服务端
        /// </summary>
        private HttpListener? httpListener = null;

        /// <summary>
        /// 传播应取消操作的通知
        /// </summary>
        private CancellationTokenSource? Token = null;

        /// <summary>
        /// HttpListenerResponse 写入数据
        /// </summary>
        /// <param name="response">响应数据</param>
        /// <param name="obj">对象</param>
        public void Write(HttpListenerResponse response, object obj)
        {
            if (obj != null)
            {
                //Json 字符串
                string? json = obj.ToJson();
                //转换成 UTF-8的字节
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                //内容长度
                response.ContentLength64 = buffer.Length;
                //写入数据
                response.OutputStream.Write(buffer, 0, buffer.Length);
                // 关闭输出流
                Close(response);
            }
        }

        /// <summary>
        /// HttpListenerResponse 写入数据
        /// </summary>
        /// <param name="response">响应数据</param>
        /// <param name="obj">对象</param>
        /// <param name="token">传播应取消操作的通知</param>
        public async Task WriteAsync(HttpListenerResponse response, object obj, CancellationToken token)
        {
            if (obj != null)
            {
                //Json 字符串
                string json = obj.ToJson();
                //转换成 UTF-8的字节
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                //内容长度
                response.ContentLength64 = buffer.Length;
                //写入数据
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, token);
                // 关闭输出流
                Close(response);
            }
        }

        /// <summary>
        /// 关闭输出流<br/>
        /// 不返回数据时使用
        /// </summary>
        /// <param name="response">响应数据</param>
        public void Close(HttpListenerResponse response)
        {
            // 关闭输出流
            response.OutputStream.Close();
            // 释放
            response.OutputStream.Dispose();
        }

        /// <summary>
        /// 处理程序接入连接
        /// </summary>
        /// <param name="listener">http 服务端</param>
        /// <param name="token">传播应取消操作的通知</param>
        /// <returns></returns>
        private async Task HandlerIncomingConnections(HttpListener listener, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    //内容
                    HttpListenerContext context = await listener.GetContextAsync();
                    //生成一个随机GUID
                    string guid = Guid.NewGuid().ToUpperNString();
                    //开始记录时间
                    TimeHandler.Instance(guid).StartRecord();
                    //请求数据
                    HttpListenerRequest request = context.Request;
                    //响应数据
                    HttpListenerResponse response = context.Response;
                    //设置内容类型
                    response.ContentType = basics.ContentType;
                    //响应json
                    string? errorJson = string.Empty;
                    //数据校验
                    if (request.HttpMethod.ToUpper() != basics.Method.ToString())
                    {
                        //请求方式错误
                        errorJson = new OperateResult(false, $"请求方式错误，必须为 [ {basics.Method.ToString()} ]", TimeHandler.Instance(guid).StopRecord().milliseconds).ToJson();
                    }
                    else if (basics.AbsolutePaths.FirstOrDefault(c => c == request.Url.AbsolutePath) == null)
                    {
                        //接口不存在
                        errorJson = new OperateResult(false, $"[ {request.Url.AbsolutePath} ] 接口不存在", TimeHandler.Instance(guid).StopRecord().milliseconds).ToJson();
                    }
                    else if (request.ContentType != null && !request.ContentType.Contains(basics.ContentType))
                    {
                        //内容类型错误
                        errorJson = new OperateResult(false, $"内容类型错误，必须为 [ {basics.ContentType} ]", TimeHandler.Instance(guid).StopRecord().milliseconds).ToJson();
                    }
                    else
                    {
                        //停止计时
                        TimeHandler.Instance(guid).StopRecord();
                    }

                    if (!string.IsNullOrWhiteSpace(errorJson))
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(errorJson);
                        //设置初始状态码
                        response.StatusCode = 400;
                        //内容长度
                        response.ContentLength64 = buffer.Length;
                        //写入数据
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                        // 关闭输出流
                        response.OutputStream.Close();
                    }
                    else
                    {
                        //设置初始状态码
                        response.StatusCode = 200;

                        //跨域操作
                        if (basics.CrossDomain)
                        {
                            // 允许特定来源的跨域请求
                            response.AppendHeader("Access-Control-Allow-Origin", request.Headers["Origin"]);
                            // 允许特定标头的跨域请求
                            response.AppendHeader("Access-Control-Allow-Headers", "*");
                            // 允许特定方法的跨域请求
                            response.AppendHeader("Access-Control-Allow-Methods", "POST,GET,PUT,OPTIONS,DELETE");
                            response.AppendHeader("Access-Control-Allow-Credentials", "true");
                            response.AppendHeader("Access-Control-Max-Age", "3600");

                            if (request.HttpMethod == "OPTIONS")
                            {
                                // 处理预检请求（OPTIONS 请求）
                                response.OutputStream.Close();
                            }
                            else
                            {
                                Handler(request, response);
                            }
                        }
                        else
                        {
                            Handler(request, response);
                        }
                    }
                }
                catch (TaskCanceledException) { }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    OnInfoEventHandler(this, new EventInfoResult(false, $"HandlerIncomingConnections 处理异常 : {ex.Message}"));
                }
            }
        }
        /// <summary>
        /// 把外来请求通过事件抛出,事件注册方处理请求
        /// </summary>
        /// <param name="request">请求数据</param>
        /// <param name="response">响应数据</param>
        private void Handler(HttpListenerRequest request, HttpListenerResponse response)
        {
            // 读取请求体数据
            using (System.IO.Stream body = request.InputStream)
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(body, request.ContentEncoding))
                {
                    //抛出数据
                    OnDataEventHandler(this, new EventDataResult(true, "Api Request", new WaitHandler()
                    {
                        BodyData = reader.ReadToEnd(),
                        Request = request,
                        Response = response
                    }));
                }
            }
        }

        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="response">响应数据</param>
        /// <param name="obj">对象</param>
        public static void Write<T>(HttpListenerResponse response, T obj)
        {
            if (obj != null)
            {
                //Json 字符串
                string json = obj.ToJson();
                //转换成 UTF-8的字节
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                //内容长度
                response.ContentLength64 = buffer.Length;
                //写入数据
                response.OutputStream.Write(buffer, 0, buffer.Length);
                // 关闭输出流
                response.OutputStream.Close();
            }
        }

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

        /// <inheritdoc/>
        public OperateResult On()
        {
            BegOperate();
            try
            {
                if (GetStatus().GetDetails(out string? message))
                {
                    return EndOperate(false, message);
                }
                if (NetHandler.IsPortInUse(basics.Port))
                {
                    return EndOperate(false, "端口被占用");
                }
                string host = $"http://{basics.IpAddress}:{basics.Port}";
                httpListener = new HttpListener();
                httpListener.Prefixes.Add(host + "/");
                httpListener.Start();
                //输出接口详情
                foreach (var item in basics.AbsolutePaths)
                {
                    OnInfoEventHandler(this, EventInfoResult.CreateSuccessResult($"{host}{item}"));
                }
                if (Token == null)
                {
                    Token = new CancellationTokenSource();
                    HandlerIncomingConnections(httpListener, Token.Token);
                }
                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message);
            }
        }
        /// <inheritdoc/>
        public OperateResult Off(bool hardClose = false)
        {
            BegOperate();
            try
            {
                if (!hardClose)
                {
                    if (!GetStatus().GetDetails(out string? message))
                    {
                        return EndOperate(false, message);
                    }
                }
                Token?.Cancel();
                Token = null;
                httpListener?.Stop();
                httpListener?.Close();
                httpListener = null;
                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message);
            }
        }
        /// <inheritdoc/>
        public OperateResult GetStatus()
        {
            BegOperate();
            if (httpListener == null)
            {
                return EndOperate(false, "未启动", logOutput: false);
            }
            else
            {
                if (httpListener.IsListening)
                {
                    return EndOperate(true, "已启动", logOutput: false);
                }
                return EndOperate(false, "未启动", logOutput: false);
            }
        }

        /// <inheritdoc/>
        public async Task<OperateResult> OnAsync(CancellationToken token = default) => await Task.Run(() => On(), token);
        /// <inheritdoc/>
        public async Task<OperateResult> OffAsync(bool hardClose = false, CancellationToken token = default) => await Task.Run(() => Off(hardClose), token);
        /// <inheritdoc/>
        public async Task<OperateResult> GetStatusAsync(CancellationToken token = default) => await Task.Run(() => GetStatus(), token);

    }
}
