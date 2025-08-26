using Snet.Model.data;
using Snet.Model.@enum;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace Snet.Core.extend
{
    /// <summary>
    /// 核心扩展
    /// </summary>
    public static class CoreExtend
    {

        /// <summary>
        /// 检查地址是否存在无效数据
        /// </summary>
        /// <param name="address">地址集合</param>
        /// <returns>
        /// true:一切正常<br/>
        /// false:存在无效数据
        /// </returns>
        public static bool CheckAddress(this Address address)
        {
            if (address.AddressArray != null && address.AddressArray.Count > 0)
            {
                if (address.AddressArray.Where(c => string.IsNullOrWhiteSpace(c.AddressName)).Count() == 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取精简返回值集合
        /// </summary>
        /// <param name="param">键值</param>
        /// <returns>精简地址集合</returns>
        public static IEnumerable<AddressValueSimplify>? GetSimplifyArray(this ConcurrentDictionary<string, AddressValue> param)
        {
            if (param.Count == 0)
            {
                return null;
            }
            return param.Values.Select(c => c.GetSimplify());
        }

        /// <summary>
        /// 获取精简 Protobuf 数据对象
        /// </summary>
        /// <param name="param">键值</param>
        /// <returns>精简 Protobuf 数据对象</returns>
        public static AddressValueSimplifyProtobuf? GetSimplifyProtobuf(this ConcurrentDictionary<string, AddressValue> param)
        {
            if (param.Count == 0)
            {
                return null;
            }
            return new AddressValueSimplifyProtobuf(param.Values.Select(c => c.GetSimplify()).ToList());
        }

        /// <summary>
        /// 查询精简数据集合；<br/>
        /// 从传入的字典中检索指定的地址信息
        /// </summary>
        /// <param name="param">参数</param>
        /// <param name="addressNames">地址名称集合</param>
        /// <returns>指定名称的数据对象</returns>
        public static IEnumerable<AddressValueSimplify>? SelectSimplifyArray(this ConcurrentDictionary<string, AddressValue> param, List<string> addressNames)
        {
            if (param.Count == 0)
            {
                return null;
            }
            return addressNames.Select(c => param[c].GetSimplify());
        }

        /// <summary>
        /// 查询精简  Protobuf  数据；<br/>
        /// 从传入的字典中检索指定的地址信息
        /// </summary>
        /// <param name="param">参数</param>
        /// <param name="addressNames">地址名称集合</param>
        /// <returns>指定名称的数据对象</returns>
        public static AddressValueSimplifyProtobuf? SelectSimplifyProtobuf(this ConcurrentDictionary<string, AddressValue> param, List<string> addressNames)
        {
            if (param.Count == 0)
            {
                return null;
            }
            return new AddressValueSimplifyProtobuf(addressNames.Select(c => param[c].GetSimplify()));
        }

        /// <summary>
        /// 获取返回值集合
        /// </summary>
        /// <param name="param">键值</param>
        /// <returns>精简地址集合</returns>
        public static IEnumerable<AddressValue>? GetArray(this ConcurrentDictionary<string, AddressValue> param)
        {
            if (param.Count == 0)
            {
                return null;
            }
            return param.Values.ToArray();
        }

        /// <summary>
        /// 查询数据集合；<br/>
        /// 从传入的字典中检索指定的地址信息
        /// </summary>
        /// <param name="param">参数</param>
        /// <param name="addressNames">地址名称集合</param>
        /// <returns>精简地址集合</returns>
        public static IEnumerable<AddressValue>? SelectArray(this ConcurrentDictionary<string, AddressValue> param, List<string> addressNames)
        {
            if (param.Count == 0)
            {
                return null;
            }
            return addressNames.Select(c => param[c]);
        }

        /// <summary>
        /// 写入获取字节；<br/>
        /// 支持类型:Int16/UInt16/Int32/UInt32/Int64/UInt64/Double/Single
        /// </summary>
        /// <param name="value">值</param>
        /// <returns>字节数据</returns>
        public static byte[] WriteGetBytes(this object value)
        {
            if (value is short || value is Int16)
            {
                return BitConverter.GetBytes((Int16)value);
            }
            if (value is ushort || value is UInt16)
            {
                return BitConverter.GetBytes((UInt16)value);
            }
            if (value is int || value is Int32)
            {
                return BitConverter.GetBytes((Int32)value);
            }
            if (value is uint || value is UInt32)
            {
                return BitConverter.GetBytes((UInt32)value);
            }
            if (value is long || value is Int64)
            {
                return BitConverter.GetBytes((Int64)value);
            }
            if (value is ulong || value is UInt64)
            {
                return BitConverter.GetBytes((UInt64)value);
            }
            if (value is double || value is Double)
            {
                return BitConverter.GetBytes((Double)value);
            }
            if (value is float || value is Single)
            {
                return BitConverter.GetBytes((Single)value);
            }
            return null;
        }

        /// <summary>
        /// 写入获取字节；<br/>
        /// 目前支持:String
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="encoding">编码格式</param>
        /// <returns>字节数据</returns>
        public static byte[] WriteGetBytes(this string value, Encoding encoding)
        {
            return encoding.GetBytes(value);
        }

        /// <summary>
        /// 读取获取对应类型的长度；<br/>
        /// 支持类型:Int16/UInt16/Int32/UInt32/Int64/UInt64/Double/Single
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <returns>返回对应类型的数据长度</returns>
        public static ushort ReadGetLength(this DataType dataType)
        {
            switch (dataType)
            {
                case DataType.Double:
                    return 8;
                case DataType.Single:
                case DataType.Float:
                    return 4;
                case DataType.Int32:
                case DataType.Int:
                    return 4;
                case DataType.UInt32:
                case DataType.Uint:
                    return 4;
                case DataType.Int64:
                case DataType.Long:
                    return 8;
                case DataType.UInt64:
                case DataType.Ulong:
                    return 8;
                case DataType.Int16:
                case DataType.Short:
                    return 2;
                case DataType.UInt16:
                case DataType.Ushort:
                    return 2;
            }
            return 0;
        }

        /// <summary>
        /// 获取对应的编码格式
        /// </summary>
        /// <param name="encodingType_AllowEmpty">编码格式_允许为空</param>
        /// <returns>System.Text.Encoding 对应格式的编码类型</returns>
        public static Encoding GetEncoding(this EncodingType? encodingType_AllowEmpty)
        {
            if (encodingType_AllowEmpty is null)
            {
                return Encoding.Default;
            }
            else
            {
                return Encoding.GetEncoding((int)encodingType_AllowEmpty);
            }
        }

        /// <summary>
        /// 获取对应的编码格式
        /// </summary>
        /// <param name="encodingType">编码格式</param>
        /// <returns>System.Text.Encoding 对应格式的编码类型</returns>
        public static Encoding GetEncoding(this EncodingType encodingType)
        {
            return GetEncoding(encodingType_AllowEmpty: encodingType);
        }

        /// <summary>
        /// 获取默认编码的写入
        /// </summary>
        /// <param name="sources">写入的数据点，与数据</param>
        /// <param name="encodingType">编码格式</param>
        /// <returns>带编码的返回,写入的数据点，数据，编码格式</returns>
        public static ConcurrentDictionary<string, (object value, EncodingType? encodingType)> GetDefaultEncodingWrite(this ConcurrentDictionary<string, object> sources, EncodingType? encodingType = EncodingType.ANSI)
        {
            // 创建目标字典
            ConcurrentDictionary<string, (object value, EncodingType? encodingType)> targetDict = new ConcurrentDictionary<string, (object value, EncodingType? encodingType)>();

            // 并行遍历原始字典
            Parallel.ForEach(sources, kvp =>
            {
                targetDict.TryAdd(kvp.Key, (kvp.Value, encodingType ?? EncodingType.ANSI));
            });

            return targetDict;
        }

        private static readonly Regex BracketRegex = new Regex(
    @"\[\s*[^\[\]]+\s*\]\s*\((?>[^()]+|\((?<Depth>)|\)(?<-Depth>))*(?(Depth)(?!))\)",
    RegexOptions.Compiled);

        private static readonly Regex ParenthesisRegex = new Regex(
            @"\((?>[^()]+|\((?<Depth>)|\)(?<-Depth>))*(?(Depth)(?!))\)",
            RegexOptions.Compiled);

        /// <summary>
        ///  消息标准化处理
        /// </summary>
        /// <param name="input">输入的数据</param>
        /// <returns>标准化后的数据</returns>
        public static string MessageStandard(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // 直接使用 LastIndexOf 而非 Split，提高效率
            int colonIndex = input.LastIndexOf(" : ", StringComparison.Ordinal);
            string nestedPart = colonIndex > 0 ? input.Substring(0, colonIndex) : input;
            string tailMessage = colonIndex > 0 ? input.Substring(colonIndex + 3).Trim() : "";

            var sb = new StringBuilder();
            var parenBuilder = new StringBuilder();

            // 提取第一个 [xxx] 以及所有 (...) 内容
            string firstBracket = null;
            var matches = BracketRegex.Matches(nestedPart);

            foreach (Match m in matches)
            {
                string val = m.Value;
                int endBracket = val.IndexOf(']');
                if (endBracket < 0) continue;

                if (firstBracket == null)
                    firstBracket = val.Substring(0, endBracket + 1);

                parenBuilder.Append(val.AsSpan(endBracket + 1));
            }

            // 查找所有独立的小括号，并排除已包含的
            var parenMatches = ParenthesisRegex.Matches(nestedPart);
            foreach (Match pm in parenMatches)
            {
                bool contained = false;
                foreach (Match bm in matches)
                {
                    if (pm.Index >= bm.Index && pm.Index < bm.Index + bm.Length)
                    {
                        contained = true;
                        break;
                    }
                }
                if (!contained)
                    parenBuilder.Append(pm.Value);
            }

            // 如果没有匹配到中括号结构，返回原始字符串
            if (firstBracket == null) return input;

            // 构造最终输出
            sb.Append(firstBracket);
            sb.Append(parenBuilder);
            if (!string.IsNullOrEmpty(tailMessage))
            {
                sb.Append(" : ").Append(tailMessage);
            }

            return sb.ToString();
        }

    }
}
