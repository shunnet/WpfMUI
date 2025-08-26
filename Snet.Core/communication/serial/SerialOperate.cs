using Snet.Core.@abstract;
using Snet.Model.data;
using Snet.Model.@interface;
using System.IO.Ports;
using static Snet.Core.communication.serial.SerialData;
namespace Snet.Core.communication.serial
{
    /// <summary>
    /// 串口通信
    /// </summary>
    public class SerialOperate : CommunicationAbstract<SerialOperate, Basics>, ICommunication
    {
        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="basics">基础数据</param>
        public SerialOperate(Basics basics) : base(basics) { }

        /// <summary>
        /// 通信库
        /// </summary>
        private SerialPort? Communication = null;

        /// <summary>
        /// 获取当前在线的串口号
        /// </summary>
        /// <returns></returns>
        public static List<string> GetPortArray()
        {
            return SerialPort.GetPortNames().ToList();
        }

        /// <summary>
        /// 私有停止监控
        /// </summary>
        private void StopMonitor()
        {
            //关闭监控
            if (Communication != null)
            {
                Communication.DataReceived -= Communication_DataReceived;
            }
        }
        /// <summary>
        /// 私有启动监控
        /// </summary>
        private void StartMonitor()
        {
            //启动监控
            if (Communication != null)
            {
                Communication.DataReceived -= Communication_DataReceived;
                Communication.DataReceived += Communication_DataReceived;
            }
        }

        /// <inheritdoc/>
        public override OperateResult On()
        {
            BegOperate();
            try
            {
                //串口打开了
                if (GetStatus().GetDetails(out string? message))
                {
                    return EndOperate(false, message);
                }
                Communication = new SerialPort();
                //设置参数
                Communication.PortName = basics.PortName;
                Communication.BaudRate = basics.BaudRate;
                Communication.Parity = basics.ParityBit;
                Communication.DataBits = basics.DataBit;
                Communication.StopBits = basics.StopBit;
                Communication.WriteTimeout = basics.WriteTimeout;
                Communication.ReadTimeout = basics.ReadTimeout;
                Communication.ReceivedBytesThreshold = basics.ReceivedBytesThreshold;
                //打开串口
                Communication.Open();
                //启动监控
                StartMonitor();

                return EndOperate(true);
            }
            catch (Exception ex)
            {
                Off(true);
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 接收串口数据
        /// </summary>
        /// <param name="sender">源</param>
        /// <param name="e">事件</param>
        private void Communication_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                byte[] buffer = new byte[Communication.BytesToRead];
                int length = Communication.BaseStream.Read(buffer, 0, buffer.Length);
                if (length > 0)
                {
                    OnDataEventHandler(this, new EventDataResult(true, $"[{TAG}]{GetLanguageValue("监控数据")}", buffer));
                }
            }
            catch (Exception ex)
            {
                OnInfoEventHandler(this, new EventInfoResult(false, $"[{TAG}]{string.Format(GetLanguageValue("监控异常"), ex.Message)}"));
                Off(true);
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

                        Communication.Write(chunk, 0, chunk.Length);

                        bytesSent += chunkSize;
                    }
                    if (bytesSent != data.Length)
                    {
                        return EndOperate(false, GetLanguageValue("存在数据块发送失败"));
                    }
                }
                else
                {
                    Communication.Write(data, 0, data.Length);
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
                StopMonitor();
                OperateResult reverseBack = Send(data);
                if (reverseBack.Status)
                {
                    byte[] buffer = new byte[basics.BufferSize];
                    int length = 0;
                    bool Status = Task.Run(() =>
                    {
                        while (!token.IsCancellationRequested)
                        {
                            length = Communication.BaseStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false).GetAwaiter().GetResult();
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
            if (Communication != null && Communication.IsOpen)
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