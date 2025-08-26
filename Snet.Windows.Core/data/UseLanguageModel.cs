using Snet.Model.@enum;
using System.Text.Json.Serialization;

namespace Snet.Windows.Core.data
{
    /// <summary>
    /// 语言模型
    /// </summary>
    public class UseLanguageModel
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="languageType">语言类型</param>
        public UseLanguageModel(LanguageType languageType)
        {
            LanguageType = languageType;
        }

        /// <summary>
        /// 语言类型
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LanguageType LanguageType { get; set; }
    }
}
