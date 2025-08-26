using Snet.Core.communication.net.http.service;
using Snet.Core.extend;
using Snet.Model.data;
using Snet.Model.@enum;
using Snet.Model.@interface;
using Snet.Unility;
using System.Collections.Concurrent;
using System.Text;

namespace Snet.Core.@abstract
{
    /// <summary>
    /// 采集抽象类；<br/>
    /// 外部必须实现抽象方法；<br/>
    /// 实现接口的异步方法；<br/>
    /// 继承核心抽象类
    /// </summary>
    /// <typeparam name="O">操作类</typeparam>
    /// <typeparam name="D">基础数据类，构造参数类</typeparam>
    public abstract class DaqAbstract<O, D> : CoreUnify<O, D>, IDaq
        where O : class
        where D : class
    {
        /// <summary>
        /// 无惨构造函数
        /// </summary>
        protected DaqAbstract() : base()
        {
            //让其支持 GB2312 编码格式
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="param">参数</param>
        protected DaqAbstract(D param) : base(param)
        {
            //让其支持 GB2312 编码格式
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        #region WebApi操作
        /// <summary>
        /// Api服务端
        /// </summary>
        private HttpServiceOperate? WebApi = null;
        /// <summary>
        /// 打开接口绝对路径
        /// </summary>
        private readonly string on = "/api/on";
        /// <summary>
        /// 关闭接口绝对路径
        /// </summary>
        private readonly string off = "/api/off";
        /// <summary>
        /// 读取接口绝对路径
        /// </summary>
        private readonly string read = "/api/read";
        /// <summary>
        /// 写入接口绝对路径
        /// </summary>
        private readonly string write = "/api/write";
        /// <summary>
        /// 获取状态接口绝对路径
        /// </summary>
        private readonly string getstatus = "/api/getstatus";
        /// <summary>
        /// 切换语言接口绝对路径
        /// </summary>
        private readonly string switchlanguage = "/api/switchlanguage";

        /// <summary>
        /// WebApi请求事件
        /// </summary>
        /// <param name="sender">源</param>
        /// <param name="e">事件结果</param>
        private void WebApi_OnDataEvent(object? sender, EventDataResult e)
        {
            HttpServiceData.WaitHandler? handler = e.GetSource<HttpServiceData.WaitHandler>();
            try
            {
                if (handler != null)
                {
                    //得到请求的接口
                    string absolutePath = handler.Request.Url.AbsolutePath;
                    if (absolutePath.Equals(on))
                    {
                        WebApi?.Write(handler.Response, On());
                    }
                    if (absolutePath.Equals(off))
                    {
                        WebApi?.Write(handler.Response, Off());
                    }
                    if (absolutePath.Equals(read))
                    {
                        Address? param = handler.BodyData?.ToJsonEntity<Address>();
                        if (param == null)
                        {
                            WebApi?.Write(handler.Response, OperateResult.CreateFailureResult($"[ {absolutePath} ] 接口请求参数错误"));
                            return;
                        }
                        WebApi?.Write(handler.Response, Read(param));
                    }
                    if (absolutePath.Equals(write))
                    {
                        ConcurrentDictionary<string, WriteModel>? param = handler.BodyData?.ToJsonEntity<ConcurrentDictionary<string, WriteModel>>();
                        if (param == null)
                        {
                            WebApi?.Write(handler.Response, OperateResult.CreateFailureResult($"[ {absolutePath} ] 接口请求参数错误"));
                            return;
                        }
                        WebApi?.Write(handler.Response, Write(param));
                    }
                    if (absolutePath.Equals(getstatus))
                    {
                        WebApi?.Write(handler.Response, GetStatus());
                    }
                    if (absolutePath.Equals(switchlanguage))
                    {
                        LanguageType type = GetLanguage() == LanguageType.zh ? LanguageType.en : LanguageType.zh;
                        SetLanguage(type);
                    }
                }
            }
            catch (Exception ex)
            {
                WebApi?.Write(handler.Response, OperateResult.CreateFailureResult($"接口响应异常：{ex.Message}"));
            }
        }
        /// <inheritdoc/>
        public OperateResult WAOn(WAModel wAModel)
        {
            if (WebApi == null)
            {
                WebApi = new HttpServiceOperate();
            }
            HttpServiceData.Basics basics = new HttpServiceData.Basics();
            basics.SET(wAModel);
            basics.AbsolutePaths = new List<string>
            {
                on,
                off,
                read,
                write,
                getstatus,
                switchlanguage
            };
            WebApi = HttpServiceOperate.Instance(basics);
            WebApi.OnDataEvent -= WebApi_OnDataEvent;
            WebApi.OnDataEvent += WebApi_OnDataEvent;
            //把信息事件抛到外部
            WebApi.OnInfoEvent -= OnInfoEventHandler;
            WebApi.OnInfoEvent += OnInfoEventHandler;
            return WebApi.On();
        }
        /// <inheritdoc/>
        public OperateResult WAOff()
        {
            BegOperate();
            if (WebApi == null)
            {
                return EndOperate(false, "WA尚未启动");
            }
            WebApi.Dispose();
            WebApi = null;
            return EndOperate(false, "WA停止成功");
        }
        /// <inheritdoc/>
        public OperateResult WAStatus()
        {
            if (WebApi == null)
            {
                return EndOperate(false, "WA尚未启动", methodName: BegOperate());
            }
            return WebApi.GetStatus();
        }
        /// <inheritdoc/>
        public OperateResult WARequestExample()
        {
            return EndOperate(true, resultData: @$"
{on} [ 打开 ]，无参请求

{off} [ 关闭 ]，无参请求

{getstatus} [ 获取状态 ]，无参请求

{switchlanguage} [ 切换语言 ]，无参请求

{read} [ 读取 ]，带参请求：Snet.Model.data.Address
{{
    ""SN"": ""8c71f4a7-04eb-4f9c-88d9-849c2f0c3a00"",
    ""AddressArray"": [
        {{
            ""SN"": ""TestAddress"",
            ""AddressName"": ""M100"",
            ""AddressDataType"": ""Float""
        }}
    ],
    ""CreationTime"": ""2024-06-05T13:01:24.6245462+08:00""
}}

{write} [ 写入 ]，带参请求：ConcurrentDictionary<string, Snet.Model.data.WriteModel>
{{
  ""M100"": {{
    ""Value"": 99.1,
    ""AddressDataType"": ""Float""
  }}
}}", methodName: BegOperate());
        }

        /// <inheritdoc/>
        public async Task<OperateResult> WAOnAsync(WAModel wAModel, CancellationToken token = default)
            => await Task.Run(() => WAOn(wAModel), token);
        /// <inheritdoc/>
        public async Task<OperateResult> WAOffAsync(CancellationToken token = default)
            => await Task.Run(() => WAOff(), token);
        /// <inheritdoc/>
        public async Task<OperateResult> WAStatusAsync(CancellationToken token = default)
            => await Task.Run(() => WAStatus(), token);
        /// <inheritdoc/>
        public async Task<OperateResult> WARequestExampleAsync(CancellationToken token = default)
            => await Task.Run(() => WARequestExample(), token);
        #endregion

        /// <inheritdoc/>
        public abstract OperateResult GetBaseObject();
        /// <inheritdoc/>
        public abstract OperateResult GetStatus();
        /// <inheritdoc/>
        public abstract OperateResult Off(bool hardClose = false);
        /// <inheritdoc/>
        public abstract OperateResult On();
        /// <inheritdoc/>
        public abstract OperateResult Read(Address address);
        /// <inheritdoc/>
        public abstract OperateResult Subscribe(Address address);
        /// <inheritdoc/>
        public abstract OperateResult UnSubscribe(Address address);
        /// <inheritdoc/>
        public abstract OperateResult Write(ConcurrentDictionary<string, (object value, EncodingType? encodingType)> values);
        /// <inheritdoc/>
        public OperateResult Write(ConcurrentDictionary<string, object> values)
            => Write(values.GetDefaultEncodingWrite(EncodingType.ASCII));
        /// <inheritdoc/>
        public OperateResult Write(ConcurrentDictionary<string, WriteModel> values)
        {
            if (values == null || values.Count <= 0)
            {
                return OperateResult.CreateFailureResult("数据不能为空");
            }
            ConcurrentDictionary<string, (object value, EncodingType? encodingType)> param = new ConcurrentDictionary<string, (object value, EncodingType? encodingType)>();
            foreach (var item in values)
            {
                try
                {
                    switch (item.Value.AddressDataType)
                    {
                        case Model.@enum.DataType.Bool:
                            param.TryAdd(item.Key, (Convert.ToBoolean(item.Value.Value.ToString()), item.Value.EncodingType));
                            break;
                        case Model.@enum.DataType.BoolArray:
                            if (item.Value.Value is string)
                            {
                                param.TryAdd(item.Key, (item.Value.Value.ToString().ToJsonEntity<bool[]>(), item.Value.EncodingType));
                            }
                            else
                            {
                                param.TryAdd(item.Key, (item.Value.Value.GetSource<bool[]>(), item.Value.EncodingType));
                            }
                            break;
                        case Model.@enum.DataType.String:
                            param.TryAdd(item.Key, (item.Value.Value.ToString(), item.Value.EncodingType));
                            break;
                        case Model.@enum.DataType.Char:
                            param.TryAdd(item.Key, (Convert.ToChar(item.Value.Value.ToString()), item.Value.EncodingType));
                            break;
                        case Model.@enum.DataType.Double:
                            param.TryAdd(item.Key, (Convert.ToDouble(item.Value.Value.ToString()), item.Value.EncodingType));
                            break;
                        case Model.@enum.DataType.DoubleArray:
                            if (item.Value.Value is string)
                            {
                                param.TryAdd(item.Key, (item.Value.Value.ToString().ToJsonEntity<double[]>(), item.Value.EncodingType));
                            }
                            else
                            {
                                param.TryAdd(item.Key, (item.Value.Value.GetSource<double[]>(), item.Value.EncodingType));
                            }
                            break;
                        case Model.@enum.DataType.Float:
                        case Model.@enum.DataType.Single:
                            param.TryAdd(item.Key, (Convert.ToSingle(item.Value.Value.ToString()), item.Value.EncodingType));
                            break;
                        case Model.@enum.DataType.FloatArray:
                        case Model.@enum.DataType.SingleArray:
                            if (item.Value.Value is string)
                            {
                                param.TryAdd(item.Key, (item.Value.Value.ToString().ToJsonEntity<float[]>(), item.Value.EncodingType));
                            }
                            else
                            {
                                param.TryAdd(item.Key, (item.Value.Value.GetSource<float[]>(), item.Value.EncodingType));
                            }
                            break;
                        case Model.@enum.DataType.Int:
                        case Model.@enum.DataType.Int32:
                            param.TryAdd(item.Key, (Convert.ToInt32(item.Value.Value.ToString()), item.Value.EncodingType));
                            break;
                        case Model.@enum.DataType.IntArray:
                        case Model.@enum.DataType.Int32Array:
                            if (item.Value.Value is string)
                            {
                                param.TryAdd(item.Key, (item.Value.Value.ToString().ToJsonEntity<int[]>(), item.Value.EncodingType));
                            }
                            else
                            {
                                param.TryAdd(item.Key, (item.Value.Value.GetSource<int[]>(), item.Value.EncodingType));
                            }
                            break;
                        case Model.@enum.DataType.Uint:
                        case Model.@enum.DataType.UInt32:
                            param.TryAdd(item.Key, (Convert.ToUInt32(item.Value.Value.ToString()), item.Value.EncodingType));
                            break;
                        case Model.@enum.DataType.UintArray:
                        case Model.@enum.DataType.UInt32Array:
                            if (item.Value.Value is string)
                            {
                                param.TryAdd(item.Key, (item.Value.Value.ToString().ToJsonEntity<uint[]>(), item.Value.EncodingType));
                            }
                            else
                            {
                                param.TryAdd(item.Key, (item.Value.Value.GetSource<uint[]>(), item.Value.EncodingType));
                            }
                            break;
                        case Model.@enum.DataType.Long:
                        case Model.@enum.DataType.Int64:
                            param.TryAdd(item.Key, (Convert.ToInt64(item.Value.Value.ToString()), item.Value.EncodingType));
                            break;
                        case Model.@enum.DataType.LongArray:
                        case Model.@enum.DataType.Int64Array:
                            if (item.Value.Value is string)
                            {
                                param.TryAdd(item.Key, (item.Value.Value.ToString().ToJsonEntity<long[]>(), item.Value.EncodingType));
                            }
                            else
                            {
                                param.TryAdd(item.Key, (item.Value.Value.GetSource<long[]>(), item.Value.EncodingType));
                            }
                            break;
                        case Model.@enum.DataType.Ulong:
                        case Model.@enum.DataType.UInt64:
                            param.TryAdd(item.Key, (Convert.ToUInt64(item.Value.Value.ToString()), item.Value.EncodingType));
                            break;
                        case Model.@enum.DataType.UlongArray:
                        case Model.@enum.DataType.UInt64Array:
                            if (item.Value.Value is string)
                            {
                                param.TryAdd(item.Key, (item.Value.Value.ToString().ToJsonEntity<ulong[]>(), item.Value.EncodingType));
                            }
                            else
                            {
                                param.TryAdd(item.Key, (item.Value.Value.GetSource<ulong[]>(), item.Value.EncodingType));
                            }
                            break;
                        case Model.@enum.DataType.Short:
                        case Model.@enum.DataType.Int16:
                            param.TryAdd(item.Key, (Convert.ToInt16(item.Value.Value.ToString()), item.Value.EncodingType));
                            break;
                        case Model.@enum.DataType.ShortArray:
                        case Model.@enum.DataType.Int16Array:
                            if (item.Value.Value is string)
                            {
                                param.TryAdd(item.Key, (item.Value.Value.ToString().ToJsonEntity<short[]>(), item.Value.EncodingType));
                            }
                            else
                            {
                                param.TryAdd(item.Key, (item.Value.Value.GetSource<short[]>(), item.Value.EncodingType));
                            }
                            break;
                        case Model.@enum.DataType.Ushort:
                        case Model.@enum.DataType.UInt16:
                            param.TryAdd(item.Key, (Convert.ToUInt16(item.Value.Value.ToString()), item.Value.EncodingType));
                            break;
                        case Model.@enum.DataType.UshortArray:
                        case Model.@enum.DataType.UInt16Array:
                            if (item.Value.Value is string)
                            {
                                param.TryAdd(item.Key, (item.Value.Value.ToString().ToJsonEntity<ushort[]>(), item.Value.EncodingType));
                            }
                            else
                            {
                                param.TryAdd(item.Key, (item.Value.Value.GetSource<ushort[]>(), item.Value.EncodingType));
                            }
                            break;
                        case Model.@enum.DataType.ByteArray:
                            if (item.Value.Value is string)
                            {
                                param.TryAdd(item.Key, (ByteHandler.HexStringToByteArray(item.Value.Value.ToString()), item.Value.EncodingType));
                            }
                            else
                            {
                                param.TryAdd(item.Key, (item.Value.Value.GetSource<byte[]>(), item.Value.EncodingType));
                            }
                            break;
                        default:
                            return OperateResult.CreateFailureResult($"{item.Key} 不支持 {item.Value.AddressDataType} 类型转换");
                    }
                }
                catch (Exception ex)
                {
                    return OperateResult.CreateFailureResult($"{item.Key} 地址数据类型转换异常:{ex.Message}");
                }
            }
            return Write(param);
        }

        /// <inheritdoc/>
        public async Task<OperateResult> GetBaseObjectAsync(CancellationToken token = default)
            => await Task.Run(() => GetBaseObject(), token);
        /// <inheritdoc/>
        public async Task<OperateResult> GetStatusAsync(CancellationToken token = default)
            => await Task.Run(() => GetStatus(), token);
        /// <inheritdoc/>
        public async Task<OperateResult> OffAsync(bool hardClose = false, CancellationToken token = default)
            => await Task.Run(() => Off(hardClose), token);
        /// <inheritdoc/>
        public async Task<OperateResult> OnAsync(CancellationToken token = default)
            => await Task.Run(() => On(), token);
        /// <inheritdoc/>
        public async Task<OperateResult> ReadAsync(Address address, CancellationToken token = default)
            => await Task.Run(() => Read(address), token);
        /// <inheritdoc/>
        public async Task<OperateResult> SubscribeAsync(Address address, CancellationToken token = default)
            => await Task.Run(() => Subscribe(address), token);
        /// <inheritdoc/>
        public async Task<OperateResult> UnSubscribeAsync(Address address, CancellationToken token = default)
            => await Task.Run(() => UnSubscribe(address), token);
        /// <inheritdoc/>
        public async Task<OperateResult> WriteAsync(ConcurrentDictionary<string, object> values, CancellationToken token = default)
            => await Task.Run(() => Write(values), token);
        /// <inheritdoc/>
        public async Task<OperateResult> WriteAsync(ConcurrentDictionary<string, WriteModel> values, CancellationToken token = default)
            => await Task.Run(() => Write(values), token);
        /// <inheritdoc/>
        public async Task<OperateResult> WriteAsync(ConcurrentDictionary<string, (object value, EncodingType? encodingType)> values, CancellationToken token = default)
            => await Task.Run(() => Write(values), token);
        /// <inheritdoc/>
        public override void Dispose()
        {
            WAOff();
            Off(true);
            base.Dispose();
        }

        /// <inheritdoc/>
        public override async Task DisposeAsync()
        {
            await WAOffAsync();
            await OffAsync(true);
            await base.DisposeAsync();
        }
    }
}
