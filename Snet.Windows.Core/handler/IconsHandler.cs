using Snet.Windows.Core.data;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;

namespace Snet.Windows.Core.handler
{
    /// <summary>
    /// 图标处理类
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
        /// 资源文件缓存
        /// </summary>
        private static ConcurrentDictionary<int, string> resourceFileCache = new();

        /// <summary>
        /// 图标缓存（Key = "path|key"，Value = DrawingImage）
        /// 作用：避免重复解析资源字典和对象查找，提升性能
        /// </summary>
        private static readonly Dictionary<string, DrawingImage> cache = new();

        /// <summary>
        /// 资源文件缓存（Key = path，Value = ResourceDictionary）
        /// 作用：同一个资源文件只加载一次，避免频繁 IO / 内存消耗
        /// </summary>
        private static readonly Dictionary<string, ResourceDictionary> dictCache = new();

        /// <summary>
        /// 根据资源路径和 Key 获取 DrawingImage（自动加载并缓存）
        /// </summary>
        /// <param name="key">资源字典中的 Key</param>
        /// <param name="path">资源路径 (支持 pack://application 或本地绝对/相对路径)</param>
        /// <returns>返回对应的 DrawingImage；若未找到则返回 null</returns>
        /// <exception cref="KeyNotFoundException">资源文件中不存在该 Key</exception>
        public static DrawingImage? GetIcon(string key, string? path = null)
        {
            //使用默认路径
            path ??= resourcePath;

            // 组合缓存键（路径 + Key）
            string cacheKey = $"{path}|{key}";

            // 1. 优先命中图标缓存（性能最佳路径）
            if (cache.TryGetValue(cacheKey, out var cachedIcon))
                return cachedIcon;

            // 2. 获取资源文件（若未加载则加载并缓存）
            if (!dictCache.TryGetValue(path, out var dict))
            {
                // 注意：UriKind.RelativeOrAbsolute 保证兼容 pack://、绝对路径、相对路径
                var uri = new Uri(path, UriKind.RelativeOrAbsolute);
                dict = new ResourceDictionary { Source = uri };
                dictCache[path] = dict;
            }

            // 3. 直接索引查找（避免 Contains + 二次索引开销）
            if (dict[key] is DrawingImage icon)
            {
                // 4. 命中后缓存图标，加速下次查找
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
            try
            {
                //清空ICON缓存
                dictCache.Clear();
                cache.Clear();
                // 执行处理
                await HandlerAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// 加载外部图标<br/>
        /// 不用引用资源文件也可以直接访问
        /// </summary>
        /// <param name="resourceFile">资源文件路径</param>
        public static void Loading(string resourceFile) => LoadingAsync(resourceFile).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <summary>
        /// 加载外部图标<br/>
        /// 不用引用资源文件也可以直接访问
        /// </summary>
        /// <param name="resourceFile">资源文件路径</param>
        public static async Task LoadingAsync(string resourceFile)
        {
            try
            {
                //存在则更新,不存在则添加
                resourceFileCache.AddOrUpdate(resourceFile.GetHashCode(), resourceFile, (k, v) => resourceFile);

                // 执行处理
                await HandlerAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
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

            var mergedDictionaries = app.Resources.MergedDictionaries;

            // 快速查找一级资源（包含指定路径关键字）
            var firstLevel = mergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains(resourcePathContains));

            if (firstLevel == null) return Task.CompletedTask;

            // 二级资源中查找旧资源
            var oldResource = firstLevel.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString == resourcePath);

            if (oldResource != null)
            {
                firstLevel.MergedDictionaries.Remove(oldResource);
            }

            // 构造新主资源字典
            var mainDict = new ResourceDictionary
            {
                Source = new Uri(resourcePath, UriKind.RelativeOrAbsolute)
            };

            // 将缓存资源合并入新主字典
            foreach (var item in resourceFileCache)
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

            // 合并资源（只修改这一块资源，不影响全局）
            app.Resources.BeginInit();
            firstLevel.MergedDictionaries.Add(mainDict);
            app.Resources.EndInit();

            return Task.CompletedTask;
        }
    }
}
