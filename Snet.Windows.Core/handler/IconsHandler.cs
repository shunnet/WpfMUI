using Snet.Log;
using Snet.Windows.Core.data;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;

namespace Snet.Windows.Core.handler
{
    /// <summary>
    /// 图标资源处理器，提供基于缓存的图标加载、皮肤感知的自动重载功能。<br/>
    /// 使用 ConcurrentDictionary 实现线程安全的多级缓存：<br/>
    /// - 图标缓存（DrawingImage）：避免重复查找<br/>
    /// - 资源字典缓存（ResourceDictionary）：避免重复加载资源文件<br/>
    /// 皮肤切换时自动清空缓存并重新加载资源。
    /// </summary>
    public class IconsHandler
    {
        static IconsHandler()
        {
            SkinHandler.OnSkinEventAsync -= OnIconsEventHandlerAsync;
            SkinHandler.OnSkinEventAsync += OnIconsEventHandlerAsync;
        }
        /// <summary>
        /// 已经加载到系统的资源文件路径
        /// </summary>
        private static readonly string resourcePath = "pack://application:,,,/Snet.Windows.Core;component/resources/Icons.xaml";
        /// <summary>
        /// 资源路径包含部分
        /// </summary>
        private static readonly string resourcePathContains = "Snet.Windows.Core;component/themes";
        /// <summary>
        /// 资源文件缓存（Key = 资源路径，避免使用 GetHashCode 导致的哈希碰撞）
        /// </summary>
        private static ConcurrentDictionary<string, string> resourceFileCache = new();

        /// <summary>
        /// 图标缓存（Key = "path|key"，Value = DrawingImage）
        /// 作用：避免重复解析资源字典和对象查找，提升性能
        /// </summary>
        private static readonly ConcurrentDictionary<string, DrawingImage> cache = new();

        /// <summary>
        /// 资源文件缓存（Key = path，Value = ResourceDictionary）
        /// 作用：同一个资源文件只加载一次，避免频繁 IO / 内存消耗
        /// </summary>
        private static readonly ConcurrentDictionary<string, ResourceDictionary> dictCache = new();

        /// <summary>
        /// 根据资源路径和 Key 获取 DrawingImage（自动加载并缓存）
        /// </summary>
        /// <param name="key">资源字典中的 Key</param>
        /// <param name="path">资源路径 (支持 pack://application 或本地绝对/相对路径)</param>
        /// <returns>返回对应的 DrawingImage；未找到时抛出 <see cref="KeyNotFoundException"/>，不会返回 null</returns>
        /// <exception cref="KeyNotFoundException">资源文件中不存在该 Key</exception>
        /// <remarks>
        /// 返回的 DrawingImage 含 DynamicResource 画刷，无法冻结，归创建它的线程所有（其它线程访问会抛出 InvalidOperationException）。<br/>
        /// 因此共享缓存只服务 UI 线程：在 UI 线程调用时读写缓存；在其它线程调用时每次都会新建一份该线程私有的实例，
        /// 不读取也不写入缓存，该实例只能在调用它的线程使用。这样后台线程的首次调用不会把"归后台线程所有"的对象
        /// 放进缓存，使之后 UI 线程取到无法使用的图标。需要在界面上显示，请在 UI 线程调用。
        /// </remarks>
        public static DrawingImage? GetIcon(string key, string? path = null)
        {
            // 没有 WPF 应用程序（Application.Current 为 null）时按 UI 线程处理，保持原有行为
            var app = Application.Current;
            bool onUiThread = app == null || app.Dispatcher.CheckAccess();

            //使用默认路径
            path ??= resourcePath;

            // 组合缓存键（路径 + Key）
            string cacheKey = $"{path}|{key}";

            // 1. 优先命中图标缓存（性能最佳路径）；缓存只属于 UI 线程
            if (onUiThread && cache.TryGetValue(cacheKey, out var cachedIcon))
                return cachedIcon;

            // 2. 获取资源文件（UI 线程：若未加载则加载并缓存；其它线程：使用线程私有的字典，不共享）
            ResourceDictionary dict = onUiThread
                ? dictCache.GetOrAdd(path, p => new ResourceDictionary { Source = new Uri(p, UriKind.RelativeOrAbsolute) })
                : new ResourceDictionary { Source = new Uri(path, UriKind.RelativeOrAbsolute) };

            // 3. 直接索引查找（避免 Contains + 二次索引开销）
            if (dict[key] is DrawingImage icon)
            {
                // 4. 命中后缓存图标，加速下次查找
                if (onUiThread)
                    cache[cacheKey] = icon;
                return icon;
            }

            // 没有找到，抛出异常（比返回 null 更容易定位问题）
            throw new KeyNotFoundException($"资源文件 {path} 中未找到 Key: {key}");
        }

        /// <summary>
        /// 更换皮肤触发
        /// </summary>
        /// <param name="sender">源</param>
        /// <param name="e">事件</param>
        private static async Task OnIconsEventHandlerAsync(object? sender, EventSkinResult e)
        {
            //清空ICON缓存
            dictCache.Clear();
            cache.Clear();
            // 执行处理
            await HandlerAsync();
        }

        /// <summary>
        /// 加载外部图标<br/>
        /// 不用引用资源文件也可以直接访问
        /// </summary>
        /// <param name="resourceFile">资源文件路径</param>
        public static void Loading(string resourceFile)
        {
            // 直接同步调用，避免 .GetAwaiter().GetResult() 在 UI 线程上造成死锁
            resourceFileCache[resourceFile] = resourceFile;
            Task task = HandlerAsync();

            // 在 UI 线程调用时 HandlerAsync 同步执行，加载失败的异常直接抛给调用方；
            // 在其他线程调用时工作被投递到 UI 线程、本方法不等待，必须观察失败，否则异常会无声丢失
            if (!task.IsCompleted)
            {
                task.ContinueWith(
                    t => LogHelper.Error($"IconsHandler 加载外部图标失败：{t.Exception?.GetBaseException().Message}", "Snet.Windows.Core", t.Exception),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }

        /// <summary>
        /// 加载外部图标<br/>
        /// 不用引用资源文件也可以直接访问
        /// </summary>
        /// <param name="resourceFile">资源文件路径</param>
        public static async Task LoadingAsync(string resourceFile)
        {
            //存在则更新,不存在则添加
            resourceFileCache[resourceFile] = resourceFile;

            // 执行处理
            await HandlerAsync();
        }

        /// <summary>
        /// 获取资源字典中的所有DrawingImage
        /// </summary>
        /// <param name="dict">对象</param>
        /// <returns>集合</returns>
        private static List<KeyValuePair<string, DrawingImage>> GetAllDrawingImages(ResourceDictionary dict)
        {
            var result = new List<KeyValuePair<string, DrawingImage>>();
            foreach (var key in dict.Keys)
            {
                if (dict[key] is DrawingImage drawingImage)
                {
                    result.Add(new KeyValuePair<string, DrawingImage>(key.ToString(), drawingImage));
                }
            }
            return result;
        }

        /// <summary>
        /// 私有处理：替换资源并合并缓存资源图标集合，极致优化版。
        /// </summary>
        private static Task HandlerAsync()
        {
            var app = Application.Current;
            if (app == null) return Task.CompletedTask;
            if (!app.Dispatcher.CheckAccess())
                return app.Dispatcher.InvokeAsync(HandlerAsync).Task.Unwrap();

            var mergedDictionaries = app.Resources.MergedDictionaries;

            // 快速查找一级资源（包含指定路径关键字）
            var firstLevel = mergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains(resourcePathContains));

            if (firstLevel == null) return Task.CompletedTask;

            // 二级资源中查找旧资源
            var oldResource = firstLevel.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString == resourcePath);

            // 先在不触碰现有资源的前提下构造完整的新主资源字典，成功后再一次性替换旧字典：
            // 原先"先移除旧字典、再解析外部文件"的顺序，会在外部资源文件缺失/损坏而抛异常时丢掉全部内置图标
            var mainDict = new ResourceDictionary
            {
                Source = new Uri(resourcePath, UriKind.RelativeOrAbsolute)
            };

            // 将缓存资源合并入新主字典
            Exception? firstFailure = null;
            foreach (var item in resourceFileCache)
            {
                try
                {
                    var tempDict = new ResourceDictionary
                    {
                        Source = new Uri(item.Value, UriKind.RelativeOrAbsolute)
                    };

                    foreach (var pair in GetAllDrawingImages(tempDict))
                    {
                        string key = pair.Key;
                        DrawingImage image = pair.Value;

                        // 使用索引覆盖写入更高效
                        mainDict[key] = image;
                    }
                }
                catch (Exception ex)
                {
                    // 无法加载的外部资源文件必须从缓存移除，否则这个"毒化条目"会让之后每一次换肤都失败
                    resourceFileCache.TryRemove(item.Key, out _);
                    firstFailure ??= ex;
                }
            }

            // 合并资源（只修改这一块资源，不影响全局）：移除与添加放在同一对 BeginInit/EndInit 之间，
            // 只触发一次资源失效，也不会出现"图标暂时消失"的中间状态
            app.Resources.BeginInit();
            try
            {
                if (oldResource != null)
                {
                    firstLevel.MergedDictionaries.Remove(oldResource);
                }
                firstLevel.MergedDictionaries.Add(mainDict);
            }
            finally
            {
                app.Resources.EndInit();
            }

            // 内置图标已完整恢复；仍把外部文件的加载失败告知调用方（保持原异常类型与堆栈）
            if (firstFailure != null)
            {
                ExceptionDispatchInfo.Capture(firstFailure).Throw();
            }

            return Task.CompletedTask;
        }
    }
}
