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
            SkinHandler.OnSkinEventAsync -= IconsHandler.OnIconsEventHandlerAsync;
            SkinHandler.OnSkinEventAsync += IconsHandler.OnIconsEventHandlerAsync;
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
        private static ConcurrentDictionary<int, string> resourceFileCache = new ConcurrentDictionary<int, string>();

        /// <summary>
        /// 更换皮肤触发
        /// </summary>
        /// <param name="sender">源</param>
        /// <param name="e">事件</param>
        public static void OnIconsEventHandler(object? sender, EventSkinResult e)
            => OnIconsEventHandlerAsync(sender, e).GetAwaiter().GetResult();

        /// <summary>
        /// 更换皮肤触发
        /// </summary>
        /// <param name="sender">源</param>
        /// <param name="e">事件</param>
        public static async Task OnIconsEventHandlerAsync(object? sender, EventSkinResult e)
        {
            try
            {
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
        public static void Loading(string resourceFile)
        {
            try
            {
                //存在则更新,不存在则添加
                resourceFileCache.AddOrUpdate(resourceFile.GetHashCode(), resourceFile, (k, v) => resourceFile);

                // 执行处理
                HandlerAsync().ConfigureAwait(false).GetAwaiter();
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
        private static async Task HandlerAsync()
        {
            var app = Application.Current;
            if (app == null) return;

            var mergedDictionaries = app.Resources.MergedDictionaries;

            // 快速查找一级资源（包含指定路径关键字）
            var firstLevel = mergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains(resourcePathContains));

            if (firstLevel == null) return;

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
                Source = new Uri(resourcePath, UriKind.Absolute)
            };

            // 将缓存资源合并入新主字典
            foreach (var item in resourceFileCache)
            {
                var tempDict = new ResourceDictionary
                {
                    Source = new Uri(item.Value, UriKind.Absolute)
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
        }

    }
}
