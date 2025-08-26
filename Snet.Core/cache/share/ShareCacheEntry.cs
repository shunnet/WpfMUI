namespace Snet.Core.cache.share
{
    /// <summary>
    /// 共享缓存条目
    /// </summary>
    public class ShareCacheEntry
    {
        /// <summary>
        /// 在内存文件中的起始位置
        /// </summary>
        public long Position { get; set; }
        /// <summary>
        /// 数据长度
        /// </summary>
        public int Length { get; set; }
    }
}
