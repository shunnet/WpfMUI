using Snet.Core.@abstract;
using Snet.Model.data;
using Snet.Model.@interface;
using Snet.Unility;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using static Snet.Core.communication.net.ws.service.WsServiceData;
namespace Snet.Core.communication.net.ws.service
{
    public class WsServiceOperate : CommunicationServiceAbstract<WsServiceOperate, Basics>, ICommunicationService
    {
        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="basics">基础数据</param>
        public WsServiceOperate(Basics basics) : base(basics) { }

        /// <summary>
        /// 通信库
        /// </summary>
        private HttpListener? Communication = null;

        /// <summary>
        /// 监控开关
        /// </summary>
        private CancellationTokenSource? MonitorConnectSwitch = null;

        /// <summary>
        /// 客户端数据
        /// </summary>
        private class Client
        {
            /// <summary>
            /// 客户端任务对象
            /// </summary>
            public Task TaskObj { get; set; }

            /// <summary>
            /// 开关
            /// </summary>
            public CancellationTokenSource Switch { get; set; }

            /// <summary>
            /// websocket对象
            /// </summary>
            public WebSocketContext WebSocketObj { get; set; }
        }

        /// <summary>
        /// 客户端消息任务容器
        /// </summary>
        private ConcurrentDictionary<string, Client> ClientIoc = new ConcurrentDictionary<string, Client>();

        /// <summary>
        /// 监控连接
        /// </summary>
        /// <returns></returns>
        private async Task MonitorConnect(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    HttpListenerContext httpListenerContext = await Communication.GetContextAsync();
                    if (httpListenerContext.Request.IsWebSocketRequest)  // 如果是websocket请求
                    {
                        string IpPort = httpListenerContext.Request.RemoteEndPoint.ToString();
                        try
                        {
                            WebSocketContext webSocketContext = await httpListenerContext.AcceptWebSocketAsync(null);
                            if (!ClientIoc.ContainsKey(IpPort))  //当字典中没有这个连接对象
                            {
                                CancellationTokenSource ts = new CancellationTokenSource();
                                Client client = new Client
                                {
                                    WebSocketObj = webSocketContext,
                                    Switch = ts,
                                    TaskObj = MonitorMessageTask(ts, webSocketContext.WebSocket, IpPort)
                                };
                                //把此对象添加到字典中
                                ClientIoc.AddOrUpdate(IpPort, client, (k, v) => client);
                                OnDataEventHandler(this, new EventDataResult(true, $"[{IpPort}]连接成功", new ClientMessage { Step = Steps.客户端连接, IpPort = IpPort }));
                            }
                        }
                        catch
                        {
                            httpListenerContext.Response.StatusCode = 500;
                            httpListenerContext.Response.Close();
                        }
                    }
                    else
                    {
                        httpListenerContext.Response.StatusCode = 400;
                        httpListenerContext.Response.Close();
                    }
                }
                catch (TaskCanceledException) { }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    OnInfoEventHandler(this, new EventInfoResult(false, $"[{TAG}]监控客户端连接异常：{ex.Message}"));
                }
            }
        }

        /// <summary>
        /// 监控消息任务
        /// </summary>
        /// <param name="token"></param>
        /// <param name="webSocket"></param>
        /// <param name="IpPort"></param>
        /// <returns></returns>
        private async Task MonitorMessageTask(CancellationTokenSource token, WebSocket webSocket, string IpPort)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    byte[] buffer = new byte[basics.BufferSize]; //数据缓冲区
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer, token.Token); // 接收数据，并返回数据的长度,等待获取完成；
                    if (result.Count > 0)
                    {
                        OnDataEventHandler(this, new EventDataResult(true, $"[{IpPort}]数据接收成功", new ClientMessage { Step = Steps.消息接收, IpPort = IpPort, Bytes = buffer.Take(result.Count).ToArray() }));
                    }
                    else
                    {
                        //移除此客户端
                        Remove(new string[] { IpPort });
                        //发布消息
                        OnDataEventHandler(this, new EventDataResult(false, $"[{IpPort}]发来的数据长度错误（小于等于零），已强制关闭连接", new ClientMessage { Step = Steps.客户端断开, IpPort = IpPort }));
                        return;
                    }
                }
                catch (TaskCanceledException) { return; }
                catch (OperationCanceledException) { return; }
                catch (Exception ex)
                {
                    //移除此客户端
                    Remove(new string[] { IpPort });
                    //发布消息
                    OnDataEventHandler(this, new EventDataResult(false, $"监控[{IpPort}]消息异常：{ex.Message}", new ClientMessage { Step = Steps.客户端断开, IpPort = IpPort }));
                    return;
                }
            }
        }

        /// <summary>
        /// 私有发送
        /// </summary>
        /// <param name="socket">客户端对象</param>
        /// <param name="data">数据</param>
        /// <returns></returns>
        private bool SendPrvate(WebSocket socket, byte[] data)
        {
            return socket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None).Wait(basics.Timeout);
        }

        /// <summary>
        /// 分包发送
        /// </summary>
        /// <param name="socket">客户端对象</param>
        /// <param name="data">数据</param>
        /// <returns></returns>
        private int SendPackPrvate(WebSocket socket, byte[] data)
        {
            int maxChunkSize = basics.MaxChunkSize;
            int bytesSent = 0;
            while (bytesSent < data.Length)
            {
                int remainingBytes = data.Length - bytesSent;
                int chunkSize = Math.Min(remainingBytes, maxChunkSize);
                byte[] chunk = new byte[chunkSize];
                Array.Copy(data, bytesSent, chunk, 0, chunkSize);

                if (!SendPrvate(socket, chunk))
                {
                    for (int i = 0; i < basics.RetrySendCount; i++)
                    {
                        if (SendPrvate(socket, chunk))
                        {
                            //发送成功跳出循环
                            continue;
                        }
                    }
                }
                bytesSent += chunkSize;
            }
            return bytesSent;
        }
        /// <inheritdoc/>
        public override OperateResult On()
        {
            BegOperate();
            try
            {
                if (GetStatus().GetDetails(out string? messge))
                {
                    return EndOperate(false, messge);
                }
                Communication = new HttpListener();
                string format = "ws://";
                string rFormat = "http://";
                string host = basics.Host;
                if (basics.Host.Contains(format))
                {
                    host = basics.Host.Replace(format, rFormat);
                }
                Communication.Prefixes.Add(host);
                Communication.Start();
                //监控开关
                if (MonitorConnectSwitch == null || MonitorConnectSwitch.IsCancellationRequested)
                {
                    MonitorConnectSwitch = new CancellationTokenSource();
                }
                //监控
                MonitorConnect(MonitorConnectSwitch.Token);

                return EndOperate(true);
            }
            catch (Exception ex)
            {
                Off(true);
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public override OperateResult Off(bool hardClose = false)
        {
            BegOperate();
            try
            {
                if (!hardClose)
                {
                    if (!GetStatus().GetDetails(out string? messge))
                    {
                        return EndOperate(false, messge);
                    }
                }
                //关闭监控
                if (MonitorConnectSwitch != null)
                {
                    MonitorConnectSwitch.Cancel();
                    //监控开关清空
                    MonitorConnectSwitch = null;
                }
                if (ClientIoc != null)
                {
                    //关闭客户端消息线程
                    foreach (var item in ClientIoc)
                    {
                        item.Value.Switch.Cancel();
                    }
                    //关闭客户端消息线程
                    foreach (var item in ClientIoc)
                    {
                        item.Value.WebSocketObj.WebSocket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
                        item.Value.WebSocketObj.WebSocket.Abort();
                        item.Value.WebSocketObj.WebSocket.Dispose();
                    }
                }
                //清空
                ClientIoc?.Clear();
                Communication?.Stop();
                Communication?.Close();
                Communication = null;

                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public override OperateResult Remove(string[] address)
        {
            //开始记录运行时间
            BegOperate();
            try
            {
                foreach (var IpPort in address)
                {
                    if (ClientIoc.ContainsKey(IpPort))
                    {
                        ClientIoc[IpPort]?.Switch?.Cancel();
                        ClientIoc[IpPort]?.WebSocketObj?.WebSocket?.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
                        ClientIoc[IpPort]?.WebSocketObj?.WebSocket?.Abort();
                        ClientIoc[IpPort]?.WebSocketObj?.WebSocket?.Dispose();
                        ClientIoc?.Remove(IpPort, out _);
                    }
                }
                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public override OperateResult Send(byte[] data, string[]? address = null)
        {
            BegOperate();
            try
            {
                if (!GetStatus().GetDetails(out string? messge))
                {
                    return EndOperate(false, messge);
                }
                List<string> FailMessage = new List<string>();
                if (address == null)
                {
                    if (ClientIoc.Count.Equals(0))
                    {
                        return EndOperate(false, "客户端未连接");
                    }
                    //群发
                    foreach (var client in ClientIoc)
                    {
                        string IP = client.Key;
                        if (data.Length > basics.MaxChunkSize)
                        {
                            if (SendPackPrvate(client.Value.WebSocketObj.WebSocket, data) != data.Length)
                            {
                                FailMessage.Add("存在数据块发送失败");
                            }
                        }
                        else
                        {
                            if (!SendPrvate(client.Value.WebSocketObj.WebSocket, data))
                            {
                                FailMessage.Add($"数据发送[{IP}]失败");
                            }
                        }
                    }
                }
                else
                {
                    foreach (var IpPort in address)
                    {
                        //指定发送
                        if (ClientIoc.ContainsKey(IpPort))
                        {
                            //实现自动分包发送
                            if (data.Length > basics.MaxChunkSize)
                            {
                                if (SendPackPrvate(ClientIoc[IpPort].WebSocketObj.WebSocket, data) != data.Length)
                                {
                                    FailMessage.Add("存在数据块发送失败");
                                }
                            }
                            else
                            {
                                if (!SendPrvate(ClientIoc[IpPort].WebSocketObj.WebSocket, data))
                                {
                                    FailMessage.Add($"数据发送[{IpPort}]失败");
                                }
                            }
                        }
                        else
                        {
                            FailMessage.Add($"数据发送失败，[{IpPort}]不存在");
                        }
                    }
                }
                if (FailMessage.Count > 0)
                {
                    return EndOperate(false, FailMessage.ToJson(), FailMessage);
                }
                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public override OperateResult Send(byte[] data, string address)
        {
            BegOperate();
            try
            {
                if (!GetStatus().GetDetails(out string? messge))
                {
                    return EndOperate(false, messge);
                }
                //指定发送
                if (ClientIoc.ContainsKey(address))
                {
                    try
                    {
                        //实现自动分包发送
                        if (data.Length > basics.MaxChunkSize)
                        {
                            if (SendPackPrvate(ClientIoc[address].WebSocketObj.WebSocket, data) != data.Length)
                            {
                                return EndOperate(false, "存在数据块发送失败");
                            }
                        }
                        else
                        {
                            if (!SendPrvate(ClientIoc[address].WebSocketObj.WebSocket, data))
                            {
                                return EndOperate(false, $"数据发送[{address}]失败");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return EndOperate(false, $"数据发送[{address}]异常：{ex.Message}");
                    }
                }
                else
                {
                    return EndOperate(false, $"数据发送失败，[{address}]不存在");
                }
                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public override OperateResult GetBaseObject()
        {
            BegOperate();
            if (!GetStatus().GetDetails(out string? messge))
            {
                return EndOperate(false, messge);
            }
            return EndOperate(true, resultData: Communication);
        }
        /// <inheritdoc/>
        public override OperateResult GetStatus()
        {
            BegOperate();
            if (Communication != null)
            {
                return EndOperate(true, "已启动", logOutput: false);
            }
            else
            {
                return EndOperate(false, "未启动", logOutput: false);
            }
        }
    }
}