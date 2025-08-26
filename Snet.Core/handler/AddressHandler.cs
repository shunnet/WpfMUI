using Snet.Core.mq;
using Snet.Core.reflection;
using Snet.Core.script;
using Snet.Log;
using Snet.Model.data;
using Snet.Model.@enum;
using Snet.Unility;
using System.Collections.Concurrent;

namespace Snet.Core.handler
{
    /// <summary>
    /// 地址统一管理
    /// </summary>
    public static class AddressHandler
    {
        /// <summary>
        /// 执行处理日志输出文件
        /// </summary>
        private static readonly string ExecuteDisposeLog = "Core/AddressHandler/ExecuteDispose.log";

        /// <summary>
        /// 解析日志输出文件
        /// </summary>
        private static readonly string ParseLog = "Core/AddressHandler/Parse.log";

        /// <summary>
        /// 生产日志输出文件
        /// </summary>
        private static readonly string ProduceLog = "Core/AddressHandler/Produce.log";

        /// <summary>
        /// 消息中间件操作
        /// </summary>
        private static MqOperate mqOperate = MqOperate.Instance(new MqData());

        /// <summary>
        /// 脚本操作
        /// </summary>
        private static ScriptOperate scriptOperate = ScriptOperate.Instance(new ScriptData.Basics());

        /// <summary>
        /// 反射容器
        /// </summary>
        private static ConcurrentDictionary<ReflectionData.Basics, ReflectionOperate> ReflectionIoc = new ConcurrentDictionary<ReflectionData.Basics, ReflectionOperate>();

        /// <summary>
        /// 执行处理<br/>
        /// 收到的底层数据，调用此方法，直接返回
        /// </summary>
        /// <param name="addressDetails">地址详情数据</param>
        /// <param name="value">底层硬件返回的值</param>
        /// <param name="message">详细信息</param>
        /// <returns>地址值</returns>
        public static AddressValue? ExecuteDispose(this AddressDetails addressDetails, object? value, string? message)
        {
            try
            {
                //原始值
                object? OriginalValue = null;
                //实际值
                object? ResultValue = null;
                // -1：尚未经过处理
                // 0：异常
                // 1：正常
                // 2：类型错误
                // 3：数据经过解析，并且解析成功，无法得知数据的正确性
                // 4：解析错误
                QualityType Quality = QualityType.None;

                //当收到空数据时说明数据异常
                if (value is null || string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    //异常数据
                    Quality = 0;
                }
                else
                {
                    //数据质量
                    Quality = DataTypeConvert(addressDetails.AddressDataType, value.ToJson(), out object? outValue, out string? convertMessage);

                    //判断数据质量
                    if (Quality == QualityType.Normal)
                    {
                        //原始值
                        OriginalValue = outValue;

                        //转换后的数据
                        ResultValue = outValue;

                        //进行解析
                        Parse(addressDetails, ResultValue, out (QualityType Q, object? V, string? M) resData);

                        //解析后的结果
                        ResultValue = resData.V;

                        //数据质量
                        Quality = resData.Q;

                        //详细信息
                        if (!string.IsNullOrWhiteSpace(resData.M))
                        {
                            message = resData.M;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(convertMessage))
                        {
                            message = convertMessage;
                        }
                    }
                }

                //最终数据组织
                AddressValue addressValue = new AddressValue().SET(addressDetails);
                addressValue.ResultValue = ResultValue;
                addressValue.OriginalValue = OriginalValue;
                addressValue.Quality = Quality;
                addressValue.Message = message;
                //数据正常执行消息中间件生产
                if (Quality == QualityType.Normal || Quality == QualityType.Unknown)
                {
                    //数据执行消息中间件生产
                    Produce(addressDetails, ResultValue);
                }

                return addressValue;
            }
            catch (Exception ex)
            {
                LogHelper.Error(message: $"{LanguageHandler.GetLanguageValue("执行数据处理异常")}：{ex.Message}", filename: ExecuteDisposeLog);
            }
            return null;
        }

        /// <summary>
        /// 执行特殊处理<br/>
        /// 处理 string 值数据,先强制转换
        /// 收到的底层数据，调用此方法，直接返回
        /// </summary>
        /// <param name="addressDetails">地址详情数据</param>
        /// <param name="value">底层硬件返回的值</param>
        /// <param name="message">详细信息</param>
        /// <returns>地址值</returns>
        public static AddressValue? ExecuteSpecialDispose(this AddressDetails addressDetails, string? value, string? message)
        {
            QualityType qualityType = DataTypeConvert(addressDetails.AddressDataType, value, out object? outValue, out string? msg);
            if (qualityType == QualityType.Normal)
            {
                return ExecuteDispose(addressDetails, outValue, message);
            }
            else
            {
                AddressValue addressValue = new AddressValue().SET(addressDetails);
                addressValue.ResultValue = value;
                addressValue.OriginalValue = value;
                addressValue.Quality = qualityType;
                addressValue.Message = msg;
                return addressValue;
            }
        }

        /// <summary>
        /// 数据类型转换
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="value">值</param>
        /// <param name="outValue">返回转换后的值</param>
        /// <param name="message">消息</param>
        /// <returns>
        /// 数据质量；<br/>
        /// -1：尚未经过处理；<br/>
        /// 0：异常；<br/>
        /// 1：正常；<br/>
        /// 2：类型错误；<br/>
        /// 3：数据经过解析，并且解析成功，无法得知数据解析后的正确性；<br/>
        /// 4：解析错误；
        /// </returns>
        private static QualityType DataTypeConvert(DataType dataType, string value, out object? outValue, out string? message)
        {
            //数据质量
            QualityType Quality = QualityType.Normal;
            //转换后的值
            outValue = null;
            //抛出的消息
            message = string.Empty;
            try
            {
                //数据类型判断
                switch (dataType)
                {
                    case DataType.Bool:
                        outValue = Convert.ToBoolean(value);
                        break;
                    case DataType.String:
                        outValue = Convert.ToString(value);
                        break;
                    case DataType.Char:
                        outValue = Convert.ToChar(value);
                        break;
                    case DataType.Double:
                        outValue = Convert.ToDouble(value);
                        break;
                    case DataType.Float:
                    case DataType.Single:
                        outValue = Convert.ToSingle(value);
                        break;
                    case DataType.Short:
                    case DataType.Int16:
                        outValue = Convert.ToInt16(value);
                        break;
                    case DataType.Ushort:
                    case DataType.UInt16:
                        outValue = Convert.ToUInt16(value);
                        break;
                    case DataType.Int:
                    case DataType.Int32:
                        outValue = Convert.ToInt32(value);
                        break;
                    case DataType.Uint:
                    case DataType.UInt32:
                        outValue = Convert.ToUInt32(value);
                        break;
                    case DataType.Long:
                    case DataType.Int64:
                        outValue = Convert.ToInt64(value);
                        break;
                    case DataType.Ulong:
                    case DataType.UInt64:
                        outValue = Convert.ToUInt64(value);
                        break;
                    case DataType.Date:
                    case DataType.Time:
                    case DataType.DateTime:
                        outValue = Convert.ToDateTime(value);
                        break;
                    case DataType.ByteArray:
                        outValue = value.ToJsonEntity<byte[]>();
                        break;
                    case DataType.BoolArray:
                        outValue = value.ToJsonEntity<bool[]>();
                        break;
                    case DataType.DoubleArray:
                        outValue = value.ToJsonEntity<double[]>();
                        break;
                    case DataType.FloatArray:
                    case DataType.SingleArray:
                        outValue = value.ToJsonEntity<float[]>();
                        break;
                    case DataType.ShortArray:
                    case DataType.Int16Array:
                        outValue = value.ToJsonEntity<short[]>();
                        break;
                    case DataType.UshortArray:
                    case DataType.UInt16Array:
                        outValue = value.ToJsonEntity<ushort[]>();
                        break;
                    case DataType.IntArray:
                    case DataType.Int32Array:
                        outValue = value.ToJsonEntity<int[]>();
                        break;
                    case DataType.UintArray:
                    case DataType.UInt32Array:
                        outValue = value.ToJsonEntity<uint[]>();
                        break;
                    case DataType.LongArray:
                    case DataType.Int64Array:
                        outValue = value.ToJsonEntity<long[]>();
                        break;
                    case DataType.UlongArray:
                    case DataType.UInt64Array:
                        outValue = value.ToJsonEntity<ulong[]>();
                        break;

                    case DataType.None:
                        outValue = value;
                        break;
                }
            }
            catch
            {
                //数据质量
                Quality = QualityType.DataTypeError;

                //设置消息
                message = $"[ {value} ][ {dataType.ToString()} ]{LanguageHandler.GetLanguageValue("类型错误")}";
            }
            return Quality;
        }

        /// <summary>
        /// 执行消息中间件生产
        /// </summary>
        /// <param name="addressDetails">地址详情</param>
        /// <param name="value">值</param>
        private static void Produce(AddressDetails addressDetails, object value)
        {
            if (addressDetails.AddressMqParam != null)
            {
                //如果内容格式不为空
                if (!string.IsNullOrWhiteSpace(addressDetails.AddressMqParam.ContentFormat) && addressDetails.AddressMqParam.ContentFormat.Contains("{0}"))
                {
                    value = string.Format(addressDetails.AddressMqParam.ContentFormat, value);
                }
                OperateResult operateResult = mqOperate.Produce(addressDetails.AddressMqParam.Topic, value.ToString(), addressDetails.AddressMqParam.ISns);
                if (!operateResult.Status)
                {
                    object log = new
                    {
                        Topic = addressDetails.AddressMqParam?.Topic,
                        Content = value,
                        ISns = addressDetails.AddressMqParam?.ISns?.ToJson()
                    };
                    LogHelper.Error(log, "{@log}", ProduceLog, consoleShow: false);
                }
            }
        }

        /// <summary>
        /// 数据解析
        /// </summary>
        /// <param name="addressDetails">地址详情</param>
        /// <param name="value">当前值</param>
        /// <param name="result">结果，解析后的值(数据质量，值，消息)</param>
        private static void Parse(AddressDetails addressDetails, object value, out (QualityType Q, object? V, string? M) result)
        {
            //详细信息
            string message = string.Empty;
            //最新值
            object? newValue = null;
            //判断地址解析参数是否为空
            if (addressDetails.AddressParseParam != null)
            {
                try
                {
                    //反射解析参数不为空
                    if (addressDetails.AddressParseParam?.ReflectionParam != null)
                    {
                        //基础数据
                        ReflectionData.Basics? basics = addressDetails.AddressParseParam?.ReflectionParam[0] as ReflectionData.Basics;
                        //需要执行的哪个SN
                        string? rsn = addressDetails.AddressParseParam?.ReflectionParam[1].ToString();
                        //判断基础数据
                        if (basics != null && rsn != null)
                        {
                            //如果不存在则新增一个
                            if (!ReflectionIoc.ContainsKey(basics))
                            {
                                ReflectionOperate operate = ReflectionOperate.Instance(basics);
                                ReflectionIoc.AddOrUpdate(basics, operate, (k, v) => operate);
                                OperateResult operateResult = ReflectionIoc[basics].Init();
                                if (!operateResult.Status)
                                {
                                    ReflectionIoc.Remove(basics, out _);
                                    message = $"反射初始化失败：{operateResult.Message}";
                                    LogHelper.Error(message: message, filename: ParseLog);
                                }
                            }
                            if (ReflectionIoc.ContainsKey(basics))
                            {
                                newValue = ReflectionIoc[basics].ExecuteMethod(rsn, new object[] { addressDetails.AddressName, value })?.ToString();
                            }
                        }
                        else
                        {
                            message = $"使用了“反射”解析，但参数错误";
                            LogHelper.Error(message: message, filename: ParseLog);
                        }
                    }

                    //脚本解析参数不为空
                    if (addressDetails.AddressParseParam?.ScriptParam != null)
                    {
                        //基础数据
                        ScriptData.Basics? basics = addressDetails.AddressParseParam?.ScriptParam as ScriptData.Basics;
                        //判断基础数据
                        if (basics != null)
                        {
                            OperateResult operateResult = scriptOperate.Execute(basics.ScriptType, basics.ScriptCode, basics.ScriptFunction, new object[] { addressDetails.AddressName, value });
                            if (operateResult.Status)
                            {
                                newValue = operateResult.ResultData?.ToString();
                            }
                            else
                            {
                                message = $"脚本执行异常：{operateResult.Message}";
                                LogHelper.Error(message: message, filename: ParseLog);
                            }
                        }
                        else
                        {
                            message = $"使用了“脚本”解析，但参数错误";
                            LogHelper.Error(message: message, filename: ParseLog);
                        }
                    }

                    //判断是否为空
                    if (newValue is null || string.IsNullOrWhiteSpace(newValue.ToString()))
                    {
                        //为空返回空字符串，并设置数据质量为4
                        result = (QualityType.ParseError, null, message);
                        return;
                    }
                    else
                    {
                        //不为空返回解析到的数据，并设置数据质量为3
                        result = (QualityType.Unknown, newValue, message);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    message = $"{LanguageHandler.GetLanguageValue("解析异常")}：" + ex.Message;
                    result = (QualityType.ParseError, null, message);
                    LogHelper.Error(message, ParseLog, ex, consoleShow: false);
                }
            }
            //没有使用解析
            result = (QualityType.Normal, value, message);
            return;
        }
    }
}