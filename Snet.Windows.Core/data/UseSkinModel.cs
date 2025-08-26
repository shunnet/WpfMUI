using Snet.Windows.Core.@enum;
using System.Text.Json.Serialization;

namespace Snet.Windows.Core.data
{
    /// <summary>
    /// 皮肤模型
    /// </summary>
    public class UseSkinModel
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="skinType">皮肤类型</param>
        public UseSkinModel(SkinType skinType)
        {
            SkinType = skinType;
        }

        /// <summary>
        /// 皮肤类型
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SkinType SkinType { get; set; }
    }
}
