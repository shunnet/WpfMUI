using System.Text.Json.Serialization;

namespace Snet.Windows.Core.@enum
{
    /// <summary>
    /// 皮肤类型
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SkinType
    {
        /// <summary>
        /// 黑夜模式
        /// </summary>
        Dark,

        /// <summary>
        /// 白天模式
        /// </summary>
        Light
    }
}