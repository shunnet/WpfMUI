using Snet.Core.@abstract;
using Snet.Model.data;
using Snet.Model.@interface;
using System.Net;
using System.Net.Sockets;
using static Snet.Core.communication.net.tcp.client.TcpClientData;

namespace Snet.Core.communication.net.tcp.client
{
    /// <summary>
    /// TCP客户端通信
    /// </summary>
    public class TcpClientOperate : CommunicationAbstract<TcpClientOperate, Basics>, ICommunication
    {
        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="basics">基础数据</param>
        public TcpClientOperate(Basics basics) : base(basics) { }

        /// <summary>
        /// 通信库
        /// </summary>
        private TcpClient? Communication = null;

        /// <summary>
        /// 监控开关
        /// </summary>
        private CancellationTokenSource? MonitorSwitch = null;

        /// <summary>
        /// 断线重连开关
        /// </summary>
        private CancellationTokenSource? InterruptReconnectionSwitch = null;

        /// <summary>
        /// 私有关闭
        /// </summary>
        private void Close()
        {
            //关闭监控
            StopMonitor();
            //关闭连接
            Communication?.Client?.Close();
            Communication?.Client?.Dispose();
            Communication?.Close();
            Communication?.Dispose();
            Communication = null;
        }
        /// <summary>
        /// 私有停止监控
        /// </summary>
        private void StopMonitor()
        {
            //关闭监控
            if (MonitorSwitch != null)
            {
                MonitorSwitch.Cancel();
                //监控开关清空
                MonitorSwitch = null;
            }
        }
        /// <summary>
        /// 私有启动监控
        /// </summary>
        private void StartMonitor()
        {
            //监控开关
            if (MonitorSwitch == null || MonitorSwitch.IsCancellationRequested)
            {
                MonitorSwitch = new CancellationTokenSource();
                //监控
                Monitor(MonitorSwitch.Token);
            }
        }
        /// <summary>
        /// 私有发送
        /// </summary>
        /// <param name="data">字节数据</param>
        /// <returns>发送状态</returns>
        private bool SendPrvate(byte[] data)
        {
            return Communication.GetStream().WriteAsync(data, 0, data.Length).Wait(basics.Timeout);
        }

        /// <summary>
        /// 监控
        /// </summary>
        /// <returns></returns>
        private async Task Monitor(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    //数据缓冲区
                    byte[] buffer = new byte[basics.BufferSize];
                    int length = await Communication?.GetStream().ReadAsync(buffer, 0, buffer.Length, token);  //读取数据
                    if (length > 0)
                    {
                        OnDataEventHandler(this, new EventDataResult(true, $"[{TAG}]{GetLanguageValue("监控数据")}", buffer.Take(length).ToArray()));  //数据传递出去
                    }
                    else
                    {
                        OnInfoEventHandler(this, new EventInfoResult(false, $"[{TAG}]{GetLanguageValue("监控数据错误")}"));  //数据传递出去
                        if (basics.InterruptReconnection)
                        {
                            Close();
                        }
                        else
                        {
                            Off(true);
                        }
                        return;
                    }
                }
                catch (TaskCanceledException) { return; }
                catch (OperationCanceledException) { return; }
                catch (Exception ex)
                {
                    OnInfoEventHandler(this, new EventInfoResult(false, $"[{TAG}]{string.Format(GetLanguageValue("监控异常"), ex.Message)}"));
                    if (basics.InterruptReconnection)
                    {
                        Close();
                    }
                    else
                    {
                        Off(true);
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// 断线重连
        /// </summary>
        /// <returns></returns>
        private async Task InterruptReconnection(CancellationToken token)
        {
            //重连次数
            int ReconnectionCount = 1;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (Communication == null)
                    {
                        Communication = new TcpClient();
                        Communication.SendTimeout = basics.Timeout;
                        if (!Communication.ConnectAsync(new IPEndPoint(IPAddress.Parse(basics.IpAddress), basics.Port)).Wait(basics.Timeout))
                        {
                            await OnInfoEventHandlerAsync(this, EventInfoResult.CreateFailureResult($"[{TAG}]{string.Format(GetLanguageValue("重连失败"), ReconnectionCount)}"));  //数据传递出去
                            ReconnectionCount++;
                            Communication.Dispose();
                            Communication = null;
                        }
                    }
                    else if (ReconnectionCount > 1)
                    {
                        await OnInfoEventHandlerAsync(this, EventInfoResult.CreateSuccessResult($"[{TAG}]{GetLanguageValue("重连成功")}"));  //数据传递出去
                        ReconnectionCount = 1;

                        StartMonitor();
                    }

                    await Task.Delay(basics.ReconnectionInterval, token);
                }
                catch (TaskCanceledException) { }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    await OnInfoEventHandlerAsync(this, EventInfoResult.CreateFailureResult($"[{TAG}]{GetLanguageValue("重连异常")}：{ex.Message}"));
                }
            }
        }

        /// <inheritdoc/>
        public override OperateResult On()
        {
            BegOperate();
            try
            {
                //打开了
                if (GetStatus().GetDetails(out string? message))
                {
                    return EndOperate(false, message);
                }
                Communication = new TcpClient();
                Communication.SendTimeout = basics.Timeout;
                if (!Communication.ConnectAsync(new IPEndPoint(IPAddress.Parse(basics.IpAddress), basics.Port)).Wait(basics.Timeout))
                {
                    Off(true);
                    return EndOperate(false, GetLanguageValue("连接超时"));
                }

                //断线重连
                if (basics.InterruptReconnection)
                {
                    //监控开关
                    if (InterruptReconnectionSwitch == null || InterruptReconnectionSwitch.IsCancellationRequested)
                    {
                        InterruptReconnectionSwitch = new CancellationTokenSource();
                    }
                    //监控
                    InterruptReconnection(InterruptReconnectionSwitch.Token);
                }

                StartMonitor();

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
                    if (!GetStatus().GetDetails(out string? message))
                    {
                        return EndOperate(false, message);
                    }
                }
                //关闭监控
                if (InterruptReconnectionSwitch != null)
                {
                    InterruptReconnectionSwitch.Cancel();
                    //监控开关清空
                    InterruptReconnectionSwitch = null;
                }
                Close();
                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public override OperateResult Send(byte[] data)
        {
            BegOperate();
            try
            {
                if (!GetStatus().GetDetails(out string? message))
                {
                    return EndOperate(false, message);
                }
                //实现自动分包发送
                if (data.Length > basics.MaxChunkSize)
                {
                    int maxChunkSize = basics.MaxChunkSize;
                    int bytesSent = 0;
                    while (bytesSent < data.Length)
                    {
                        int remainingBytes = data.Length - bytesSent;
                        int chunkSize = Math.Min(remainingBytes, maxChunkSize);
                        byte[] chunk = new byte[chunkSize];
                        Array.Copy(data, bytesSent, chunk, 0, chunkSize);

                        if (!SendPrvate(chunk))
                        {
                            for (int i = 0; i < basics.RetrySendCount; i++)
                            {
                                if (SendPrvate(chunk))
                                {
                                    //发送成功跳出循环
                                    continue;
                                }
                            }
                        }
                        bytesSent += chunkSize;
                    }
                    if (bytesSent != data.Length)
                    {
                        return EndOperate(false, GetLanguageValue("存在数据块发送失败"));
                    }
                }
                else
                {
                    if (!SendPrvate(data))
                    {
                        return EndOperate(false, GetLanguageValue("发送数据超时"));
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
        public override OperateResult SendWait(byte[] data, CancellationToken token)
        {
            BegOperate();
            try
            {
                if (!GetStatus().GetDetails(out string? message))
                {
                    return EndOperate(false, message);
                }

                //先关闭监控
                StopMonitor();

                OperateResult reverseBack = Send(data);
                if (reverseBack.Status)
                {
                    byte[] buffer = new byte[basics.BufferSize];
                    int length = 0;
                    bool Status = Task.Run(async () =>
                    {
                        while (!token.IsCancellationRequested)
                        {
                            length = await Communication.GetStream().ReadAsync(buffer, 0, buffer.Length, token);  //读取数据
                            if (length > 0)
                            {
                                return;
                            }
                        }

                    }, token).Wait(basics.SendWaitInterval);
                    if (Status)
                    {
                        byte[] retData = buffer.Take(length).ToArray();
                        if (retData.Length > 0)
                        {
                            return EndOperate(true, resultData: retData);
                        }
                        else
                        {
                            return EndOperate(false, GetLanguageValue("未接收到数据"));
                        }
                    }
                    else
                    {
                        return EndOperate(false, GetLanguageValue("接收数据超时"));
                    }

                }
                return EndOperate(false, GetLanguageValue("发送等待结果失败"));
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
            finally
            {
                if (GetStatus().Status)
                {
                    //启动监控
                    StartMonitor();
                }
            }
        }
        /// <inheritdoc/>
        public override OperateResult GetBaseObject()
        {
            BegOperate();
            if (!GetStatus().GetDetails(out string? message))
            {
                return EndOperate(false, message);
            }
            return EndOperate(true, resultData: Communication);
        }
        /// <inheritdoc/>
        public override OperateResult GetStatus()
        {
            BegOperate();
            if (Communication != null && Communication.Connected)
            {
                return EndOperate(true, GetLanguageValue("已连接"), logOutput: false);
            }
            else
            {
                return EndOperate(false, GetLanguageValue("未连接"), logOutput: false);
            }
        }
    }
}