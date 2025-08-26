using System.ComponentModel;
using System.IO.MemoryMappedFiles;

namespace Snet.Core.cache.share
{
    /// <summary>
    /// 共享缓存的数据
    /// </summary>
    public class ShareCacheData
    {
        /// <summary>
        /// 默认路径<br/>
        /// 默认指向系统缓存目录<br/>
        /// 也可以自定义目录
        /// </summary>
        [Description("默认路径")]
        public string Path { get; set; } = System.IO.Path.GetTempPath();

        /// <summary>
        /// 文件名称
        /// </summary>
        [Description("文件名称")]
        public string FileName { get; set; } = "SnetShareCache.dat";

        /// <summary>
        /// 映射名称
        /// </summary>
        [Description("映射名称")]
        public string MapName { get; set; } = "SnetShareCache";

        /// <summary>
        /// 容量<br/>
        /// 默认10M
        /// </summary>
        [Description("容量")]
        public long Capacity { get; set; } = 1024 * 1024 * 10;

        /// <summary>
        /// 缓存文件访问类型
        /// </summary>
        [Description("缓存文件访问类型")]
        [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
        public MemoryMappedFileAccess Access { get; set; } = MemoryMappedFileAccess.ReadWrite;
    }
}
