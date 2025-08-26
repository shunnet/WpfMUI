using Snet.Model.@enum;
using System.Text.Json.Serialization;

namespace Snet.Core.virtualAddress
{
    /// <summary>
    /// 虚拟地址的数据
    /// </summary>
    public class VirtualAddressData
    {
        /// <summary>
        /// 地址名称
        /// </summary>
        public string? AddressName { get; set; }

        /// <summary>
        /// 地址类型
        /// </summary>
        public AddressType AddressType { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DataType DataType { get; set; }
    }
}