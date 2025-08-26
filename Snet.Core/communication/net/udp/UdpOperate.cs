using Snet.Core.@abstract;
using Snet.Model.data;
using Snet.Model.@interface;
using Snet.Unility;
using System.Net;
using System.Net.Sockets;
using static Snet.Core.communication.net.udp.UdpData;
namespace Snet.Core.communication.net.udp
{
    /// <summary>
    /// UDP通信操作
    /// </summary>
    public class UdpOperate : CommunicationAbstract<UdpOperate, Basics>, ICommunication, ISendUdp, ISendWaitUdp
    {
        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="basics">基础数据</param>
        public UdpOperate(Basics basics) : base(basics) { }

        /// <summary>
        /// 通信库
        /// </summary>
        private UdpClient? Communication = null;

        /// <summary>
        /// 监控开关
        /// </summary>
        private CancellationTokenSource? MonitorSwitch = null;

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
        /// 监控
        /// </summary>
        /// <returns></returns>
        private async Task Monitor(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result = await Communication.ReceiveAsync(token);  //读取数据
                    if (result.Buffer.Length > 0)
                    {
                        OnDataEventHandler(this, new EventDataResult(true, $"[{TAG}]{GetLanguageValue("监控数据")}，[{result.RemoteEndPoint}]", new UdpData.TerminalMessage { IpPort = result.RemoteEndPoint.ToString(), Bytes = result.Buffer }));  //数据传递出去
                    }
                    else
                    {
                        OnInfoEventHandler(this, new EventInfoResult(false, $"[{TAG}]{GetLanguageValue("监控数据错误")}"));  //数据传递出去
                        Off(true);
                        return;
                    }
                }
                catch (TaskCanceledException) { return; }
                catch (OperationCanceledException) { return; }
                catch (Exception ex)
                {
                    OnInfoEventHandler(this, new EventInfoResult(false, $"[{TAG}]{string.Format(GetLanguageValue("监控异常"), ex.Message)}"));
                    Off(true);
                    return;
                }
            }
        }

        /// <summary>
        /// 私有发送
        /// </summary>
        /// <param name="data">字节数据</param>
        /// <returns>发送状态</returns>
        private bool SendPrvate(byte[] data)
        {
            return Communication.Send(data, data.Length) == data.Length;
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
                IPEndPoint IpPort = new IPEndPoint(IPAddress.Parse(basics.IpAddress), basics.Port);
                Communication = new UdpClient(IpPort);
                //设置是否广播模式
                Communication.EnableBroadcast = basics.EnableBroadcast;
                //远程IP端口不为空并且不使用广播模式时，连接到指定的远程IP端口
                if (!basics.RemoteIpAddress.IsNullOrWhiteSpace() && basics.RemotePort != 0 && !basics.EnableBroadcast)
                {
                    IPEndPoint RemoteIpPort = new IPEndPoint(IPAddress.Parse(basics.RemoteIpAddress), basics.RemotePort);
                    //连接到指定的远程IP端口
                    Communication.Connect(RemoteIpPort);
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
                StopMonitor();
                Communication?.Close();
                Communication?.Dispose();
                Communication = null;
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
                        return EndOperate(false, GetLanguageValue("发送数据失败"));
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
                //关闭监控
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
                            UdpReceiveResult result = await Communication.ReceiveAsync(token);   //读取数据
                            if (result.Buffer.Length > 0)
                            {
                                buffer = result.Buffer;
                                length = result.Buffer.Length;
                                return;
                            }
                        }
                    }, token).Wait(basics.SendWaitInterval);
                    if (Status)
                    {
                        byte[] retData = buffer;
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
            if (Communication != null)
            {
                return EndOperate(true, GetLanguageValue("已连接"), logOutput: false);
            }
            else
            {
                return EndOperate(false, GetLanguageValue("未连接"), logOutput: false);
            }
        }
        /// <inheritdoc/>
        public OperateResult Send(byte[] data, IPEndPoint iPEndPoint)
        {
            BegOperate();
            try
            {
                if (!GetStatus().GetDetails(out string? message))
                {
                    return EndOperate(false, message);
                }
                //数据长度
                int length;
                //远程IP端口不为空并且不使用广播模式时，连接到指定的远程IP端口
                if (!basics.RemoteIpAddress.IsNullOrWhiteSpace() && basics.RemotePort != 0 && !basics.EnableBroadcast)
                {
                    length = Communication.Send(data, data.Length);
                }
                else
                {
                    length = Communication.Send(data, data.Length, iPEndPoint);
                }
                if (length == data.Length)
                {
                    return EndOperate(true);
                }
                else
                {
                    return EndOperate(false, GetLanguageValue("数据发送失败，发送长度小于等于零"));
                }
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public OperateResult SendWait(byte[] data, IPEndPoint iPEndPoint, CancellationToken token)
        {
            BegOperate();
            try
            {
                if (!GetStatus().GetDetails(out string? message))
                {
                    return EndOperate(false, message);
                }
                //关闭监控
                StopMonitor();

                OperateResult reverseBack = Send(data, iPEndPoint);
                if (reverseBack.Status)
                {
                    byte[] buffer = new byte[basics.BufferSize];
                    int length = 0;
                    bool Status = Task.Run(async () =>
                    {
                        while (!token.IsCancellationRequested)
                        {
                            UdpReceiveResult result = await Communication.ReceiveAsync(token);   //读取数据
                            if (result.Buffer.Length > 0)
                            {
                                buffer = result.Buffer;
                                length = result.Buffer.Length;
                                return;
                            }
                        }
                    }, token).Wait(basics.SendWaitInterval);
                    if (Status)
                    {
                        byte[] retData = buffer;
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
        public async Task<OperateResult> SendAsync(byte[] data, IPEndPoint iPEndPoint, CancellationToken token = default) => await Task.Run(() => Send(data, iPEndPoint), token);
        /// <inheritdoc/>
        public async Task<OperateResult> SendWaitAsync(byte[] data, IPEndPoint iPEndPoint, CancellationToken token = default) => await Task.Run(() => SendWait(data, iPEndPoint, token), token);
    }
}