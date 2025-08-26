using Snet.Core.extend;
using Snet.Model.data;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Text.Json;

namespace Snet.Core.cache.share
{
    /// <summary>
    /// 共享缓存操作类
    /// </summary>
    public class ShareCacheOperate : CoreUnify<ShareCacheOperate, ShareCacheData>, IDisposable
    {
        /// <summary>
        /// 内存映射文件实例（全局只创建一次，避免反复创建开销）
        /// </summary>
        private MemoryMappedFile mappedFile;

        /// <summary>
        /// 内存映射文件访问器（全局复用）
        /// </summary>
        private MemoryMappedViewAccessor mappedViewAccessor;

        /// <summary>
        /// 缓存索引表，保存 Key 对应的存储位置和长度
        /// </summary>
        private readonly ConcurrentDictionary<string, ShareCacheEntry> indexs = new();

        /// <summary>
        /// 使用 ShareCacheFreeListAllocator 管理空间
        /// </summary>
        private readonly ShareCacheFreeListAllocator allocator = new();

        /// <summary>
        /// 文件头用于存储索引的大小（1MB）
        /// </summary>
        private const int HeaderSize = 1024 * 1024;

        /// <summary>
        /// 跨进程互斥锁，确保多进程写入安全
        /// </summary>
        private static readonly Mutex GlobalMutex = new Mutex(false, "Global_ShareCache_Mutex");

        /// <summary>
        /// 有参构造函数<br/>
        /// 共享缓存操作类
        /// </summary>
        /// <param name="basics">基础数据</param>
        public ShareCacheOperate(ShareCacheData basics) : base(basics)
        {
            Init();
            RefreshIndex();
        }

        #region 索引持久化

        /// <summary>
        /// 刷新索引（重新加载共享内存里的最新索引）
        /// </summary>
        private void RefreshIndex()
        {
            byte[] buffer = new byte[HeaderSize];
            mappedViewAccessor.ReadArray(0, buffer, 0, HeaderSize);

            string json = System.Text.Encoding.UTF8.GetString(buffer).Trim('\0');

            indexs.Clear();
            if (!string.IsNullOrWhiteSpace(json))
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, ShareCacheEntry>>(json);
                if (dict != null)
                {
                    foreach (var kv in dict)
                    {
                        indexs[kv.Key] = kv.Value;
                    }
                }
            }
        }

        /// <summary>
        /// 保存索引到共享内存
        /// </summary>
        private void SaveIndex()
        {
            var json = JsonSerializer.Serialize(indexs);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            if (bytes.Length > HeaderSize)
                throw new InvalidOperationException("索引数据超过 Header 区域大小");

            // 写入索引
            mappedViewAccessor.WriteArray(0, bytes, 0, bytes.Length);

            // 清零剩余部分
            if (bytes.Length < HeaderSize)
            {
                mappedViewAccessor.WriteArray(bytes.Length, new byte[HeaderSize - bytes.Length], 0, HeaderSize - bytes.Length);
            }
        }

        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            if (!Directory.Exists(basics.Path))
            {
                // 如果目录不存在，创建目录
                Directory.CreateDirectory(basics.Path);
            }
            // 初始化内存映射文件（只创建一次）
            mappedFile = MemoryMappedFile.CreateFromFile(
                Path.Combine(basics.Path, basics.FileName),
                FileMode.OpenOrCreate,
                basics.MapName,
                basics.Capacity,
                basics.Access);
            mappedViewAccessor = mappedFile.CreateViewAccessor();
        }


        /// <summary>
        /// 分配空间
        /// </summary>
        private ShareCacheEntry AllocateSpace(int size)
        {
            long pos = allocator.Allocate(size) + HeaderSize; // 跳过头部
            return new ShareCacheEntry
            {
                Position = pos,
                Length = size
            };
        }

        /// <summary>
        /// 设置缓存（同步）
        /// </summary>
        public OperateResult SetCache(string key, byte[] value)
        {
            BegOperate();
            try
            {
                // 加锁，确保索引与写入原子性
                GlobalMutex.WaitOne();

                // 重新加载索引（避免本地缓存与共享内存中的不一致）
                RefreshIndex();

                if (!indexs.TryGetValue(key, out var entry))
                {
                    entry = AllocateSpace(value.Length);
                    indexs[key] = entry;
                }
                else if (value.Length > entry.Length)
                {
                    // 清零旧数据
                    mappedViewAccessor.WriteArray(entry.Position, new byte[entry.Length], 0, entry.Length);

                    // 旧块回收
                    allocator.Free(entry.Position, entry.Length);

                    // 分配新块
                    entry = AllocateSpace(value.Length);
                    indexs[key] = entry;
                }
                else
                {
                    // 小于等于，直接覆盖
                }

                // 写入数据
                mappedViewAccessor.WriteArray(entry.Position, value, 0, value.Length);

                SaveIndex(); // 每次更新缓存后保存索引

                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
            finally
            {
                GlobalMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 设置缓存（异步）
        /// </summary>
        public Task<OperateResult> SetCacheAsync(string key, byte[] value, CancellationToken token = default) =>
            Task.Run(() => SetCache(key, value), token);

        /// <summary>
        /// 获取缓存（同步）
        /// </summary>
        public OperateResult GetCache(string key)
        {
            BegOperate();
            try
            {
                // 加锁，确保索引与写入原子性
                GlobalMutex.WaitOne();

                // 重新加载索引（避免本地缓存与共享内存中的不一致）
                RefreshIndex();

                if (!indexs.TryGetValue(key, out var entry))
                    return EndOperate(false, $"未找到缓存: {key}");

                byte[] data = new byte[entry.Length];
                mappedViewAccessor.ReadArray(entry.Position, data, 0, entry.Length);

                return EndOperate(true, resultData: data);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
            finally
            {
                GlobalMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 获取缓存（异步）
        /// </summary>
        public Task<OperateResult> GetCacheAsync(string key, CancellationToken token = default) =>
            Task.Run(() => GetCache(key), token);

        /// <summary>
        /// 移除缓存（同步）
        /// </summary>
        public OperateResult RemoveCache(string key)
        {
            BegOperate();
            try
            {
                // 加锁，确保索引与写入原子性
                GlobalMutex.WaitOne();

                // 重新加载索引（避免本地缓存与共享内存中的不一致）
                RefreshIndex();

                if (!indexs.TryRemove(key, out var entry))
                    return EndOperate(false, $"未找到缓存: {key}");

                mappedViewAccessor.WriteArray(entry.Position, new byte[entry.Length], 0, entry.Length);

                // 回收空间
                allocator.Free(entry.Position, entry.Length);

                // 移除缓存后更新索引
                SaveIndex();
                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
            finally
            {
                GlobalMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 移除缓存（异步）
        /// </summary>
        public Task<OperateResult> RemoveCacheAsync(string key, CancellationToken token = default) =>
            Task.Run(() => RemoveCache(key), token);

        /// <summary>
        /// 清空所有缓存（同步）
        /// </summary>
        public OperateResult ClearCache()
        {
            BegOperate();
            try
            {
                GlobalMutex.WaitOne();
                RefreshIndex();

                // 1. 清空索引和指针
                indexs.Clear();
                allocator.Reset();

                // 2. 释放旧的视图和映射文件
                mappedViewAccessor?.Dispose();
                mappedFile?.Dispose();

                // 3. 删除旧文件
                var filePath = Path.Combine(basics.Path, basics.FileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // 4. 重新初始化
                Init();

                //5. 清空索引
                SaveIndex();
                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
            finally
            {
                GlobalMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 清空所有缓存（异步）
        /// </summary>
        public Task<OperateResult> ClearCacheAsync(CancellationToken token = default) =>
            Task.Run(() => ClearCache(), token);

        /// <inheritdoc/>
        public override void Dispose()
        {
            ClearCache();
            base.Dispose();
        }

        /// <inheritdoc/>
        public override async Task DisposeAsync()
        {
            await ClearCacheAsync();
            await base.DisposeAsync();
        }
    }
}
