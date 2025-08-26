using System.Text.Json.Serialization;

namespace Snet.Core.script
{
    /// <summary>
    /// 脚本数据
    /// </summary>
    public class ScriptData
    {
        /// <summary>
        /// 基础数据
        /// </summary>
        public class Basics
        {
            /// <summary>
            /// 脚本类型
            /// </summary>
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public ScriptType ScriptType { get; set; } = ScriptType.JavaScript;

            /// <summary>
            /// 脚本代码或路径
            /// </summary>
            public string? ScriptCode { get; set; }

            /// <summary>
            /// 脚本函数
            /// </summary>
            public string? ScriptFunction { get; set; }
        }

        /// <summary>
        /// 脚本类型
        /// </summary>
        public enum ScriptType
        {
            JavaScript
        }
    }
}