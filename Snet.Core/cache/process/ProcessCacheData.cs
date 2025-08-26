using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel;

namespace Snet.Core.cache.process
{
    /// <summary>
    /// 进程缓存数据
    /// </summary>
    public class ProcessCacheData
    {
        /// <summary>
        /// 绝对过期时间(分钟)
        /// </summary>
        [Description("绝对过期时间(分钟)")]
        public int AbsoluteExpiration { get; set; } = 60;

        /// <summary>
        /// 滑动过期时间(分钟)
        /// </summary>
        [Description("滑动过期时间(分钟)")]
        public int SlidingExpiration { get; set; } = 20;

        /// <summary>
        /// 优先级
        /// </summary>
        [Description("优先级")]
        [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
        public CacheItemPriority Priority { get; set; } = CacheItemPriority.Normal;
    }
}
