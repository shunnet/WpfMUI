using Microsoft.Extensions.Caching.Memory;
using Snet.Core.extend;
using Snet.Model.data;

namespace Snet.Core.cache.process
{
    /// <summary>
    /// 进程缓存操作类<br/>
    /// 只允许在单个进程中使用，不能跨进程使用
    /// </summary>
    public class ProcessCacheOperate : CoreUnify<ProcessCacheOperate, ProcessCacheData>, IDisposable
    {
        /// <summary>
        /// 缓存对象
        /// </summary>
        private readonly MemoryCache cacheObject = new MemoryCache(new MemoryCacheOptions());
        /// <summary>
        /// 缓存选项
        /// </summary>
        private readonly MemoryCacheEntryOptions cacheOptions = new MemoryCacheEntryOptions();

        /// <summary>
        /// 有参构造函数<br/>
        /// 进程缓存操作类
        /// </summary>
        /// <param name="basics">基础数据</param>
        public ProcessCacheOperate(ProcessCacheData basics) : base(basics)
        {
            cacheOptions.SetAbsoluteExpiration(TimeSpan.FromMinutes(basics.AbsoluteExpiration));
            cacheOptions.SetSlidingExpiration(TimeSpan.FromMinutes(basics.SlidingExpiration));
            cacheOptions.SetPriority(basics.Priority);
        }

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <typeparam name="T">缓存对象</typeparam>
        /// <param name="key">健</param>
        /// <param name="value">值</param>
        /// <returns>设置状态</returns>
        public OperateResult SetCache<T>(string key, T value)
        {
            BegOperate();
            try
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
                };
                cacheObject.Set(key, value, cacheEntryOptions);
                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 异步设置缓存
        /// </summary>
        /// <typeparam name="T">缓存对象类型</typeparam>
        /// <param name="key">健</param>
        /// <param name="value">值</param>
        /// <param name="token">生命周期</param>
        /// <returns>缓存对象</returns>
        public async Task<OperateResult> SetCacheAsync<T>(string key, T value, CancellationToken token = default)
            => await Task.Run(() => SetCache<T>(key, value), token);

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <typeparam name="T">缓存对象类型</typeparam>
        /// <param name="key">健</param>
        /// <returns>缓存对象</returns>
        public OperateResult GetCache<T>(string key)
        {
            BegOperate();
            try
            {
                if (cacheObject.TryGetValue(key, out T? value))
                {
                    return EndOperate(true, resultData: value);
                }
                else
                {
                    return EndOperate(false, "未获取到对象键值对象");
                }
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 异步获取缓存
        /// </summary>
        /// <typeparam name="T">缓存对象类型</typeparam>
        /// <param name="key">健</param>
        /// <param name="token">生命周期</param>
        /// <returns>缓存对象</returns>
        public async Task<OperateResult> GetCacheAsync<T>(string key, CancellationToken token = default)
            => await Task.Run(() => GetCache<T>(key), token);

        /// <summary>
        /// 移除缓存
        /// </summary>
        /// <param name="key">健</param>
        /// <returns>操作结果</returns>
        public OperateResult RemoveCache(string key)
        {
            BegOperate();
            try
            {
                cacheObject.Remove(key);
                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 异步移除缓存
        /// </summary>
        /// <param name="key">健</param>
        /// <param name="token">生命周期</param>
        /// <returns>操作结果</returns>
        public async Task<OperateResult> RemoveCacheAsync(string key, CancellationToken token = default)
            => await Task.Run(() => RemoveCache(key), token);

        /// <summary>
        /// 清空缓存
        /// </summary>
        /// <returns>操作结果</returns>
        public OperateResult ClearCache()
        {
            BegOperate();
            try
            {
                //清空
                cacheObject.Clear();
                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 异步清空缓存
        /// </summary>
        /// <param name="token">生命周期</param>
        /// <returns>操作结果</returns>
        public async Task<OperateResult> ClearCacheAsync(CancellationToken token = default)
            => await Task.Run(() => ClearCache(), token);

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
