using MaterialDesignThemes.Wpf;
using Snet.Unility;
using Snet.Windows.Core.data;
using Snet.Windows.Core.@enum;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace Snet.Windows.Core.handler
{
    /// <summary>
    /// 皮肤处理器类，用于设置和获取应用程序皮肤主题，并支持同步与异步事件通知
    /// </summary>
    public class SkinHandler
    {
        #region 事件定义

        /// <summary>
        /// 同步皮肤切换事件
        /// </summary>
        public static event EventHandler<EventSkinResult> OnSkinEvent;

        /// <summary>
        /// 异步皮肤切换事件（通过包装器调用）
        /// </summary>
        public static event EventHandlerAsync<EventSkinResult> OnSkinEventAsync
        {
            add => OnSkinEventWrapperAsync.AddHandler(value);
            remove => OnSkinEventWrapperAsync.RemoveHandler(value);
        }

        /// <summary>
        /// 异步事件包装器实例
        /// </summary>
        private static EventingWrapperAsync<EventSkinResult> OnSkinEventWrapperAsync;

        /// <summary>
        /// 内部方法：统一触发同步与异步事件
        /// </summary>
        private static void OnSkinEventHandler(object? sender, EventSkinResult e)
        {
            OnSkinEvent?.Invoke(sender, e);
            OnSkinEventWrapperAsync.InvokeAsync(sender, e);
        }

        #endregion

        #region 私有字段

        /// <summary>
        /// 皮肤配置文件路径
        /// </summary>
        private static readonly string _pathSkin = Path.Combine(WindowHandler.BasePath, "skin.json");

        #endregion

        #region 私有方法
        private static PaletteHelper paletteHelper = new PaletteHelper();
        /// <summary>
        /// 修改当前主题样式（由调用者指定修改内容）
        /// </summary>
        private static void ModifyTheme(Action<Theme> modificationAction)
        {
            Theme theme = paletteHelper.GetTheme();
            modificationAction?.Invoke(theme);
            paletteHelper.SetTheme(theme);
        }
        /// <summary>
        /// 修改当前主题样式（由调用者指定修改内容） 
        /// </summary>
        private static async Task ModifyThemeAsync(Func<Theme, Task> modificationFunc)
        {
            Theme theme = paletteHelper.GetTheme();
            if (modificationFunc != null)
                await modificationFunc(theme);
            paletteHelper.SetTheme(theme);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置指定皮肤类型的主题样式并保存
        /// </summary>
        public static void SetSkin(SkinType skinType, bool notice = true)
        {
            try
            {
                setSkin(skinType, notice);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// 私有设置皮肤
        /// </summary>
        /// <param name="skinType">皮肤类型</param>
        /// <param name="notice">是否通知</param>
        private static void setSkin(SkinType skinType, bool notice)
        {
            //格式
            string format = "pack://application:,,,/Snet.Windows.Core;component/themes/{0}Theme.xaml";
            //白天资源
            string light = string.Format(format, "Light");
            //黑夜资源
            string dark = string.Format(format, "Dark");

            // 新的资源地址
            string newResource = string.Format(format, skinType);

            //新的资源对象
            ResourceDictionary newResourceDictionary = new ResourceDictionary { Source = new Uri(newResource, UriKind.RelativeOrAbsolute) };

            //检索的旧资源对象
            ResourceDictionary oldResourceDictionary = default;

            //检索资源
            foreach (var item in Application.Current.Resources.MergedDictionaries)
            {
                if (item.Source != null)
                {
                    switch (skinType)
                    {
                        case SkinType.Dark:
                            if (item.Source.AbsoluteUri == light)
                                oldResourceDictionary = item;
                            break;
                        case SkinType.Light:
                            if (item.Source.AbsoluteUri == dark)
                                oldResourceDictionary = item;
                            break;
                    }
                }
            }

            //替换资源
            ReplaceResources(newResourceDictionary, oldResourceDictionary);

            // 修改 MaterialDesign 主题
            UpdateMaterialDesignThemeAsync(skinType);

            //是否通知
            if (notice)
            {
                // 通知事件订阅者
                OnSkinEventHandler(skinType, new EventSkinResult(true, Snet.Core.handler.LanguageHandler.GetLanguageValue("皮肤设置成功", new("Snet.Windows.Controls", "Language", "Snet.Windows.Controls.dll")), skinType));
            }

            // 持久化保存皮肤设置
            SaveAsync(skinType);
        }

        /// <summary>
        /// 替换资源（线程安全、最小化 UI 闪烁）<br/>
        /// 注意：不支持异步调用，此操作必须在 UI 线程上执行。
        /// </summary>
        /// <param name="newDict">新资源</param>
        /// <param name="oldDict">旧资源</param>
        public static void ReplaceResources(ResourceDictionary newDict, ResourceDictionary oldDict)
        {
            var app = Application.Current;
            if (app == null || newDict == null) return;

            // UI线程执行
            if (!app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.Invoke(() => ReplaceResources(newDict, oldDict));
                return;
            }

            // 已存在相同资源，不需要替换
            if (oldDict != null && newDict.Source == oldDict.Source)
                return;

            var dictionaries = app.Resources.MergedDictionaries;

            // 先移除旧资源（若存在）
            if (oldDict != null)
            {
                var existing = dictionaries.FirstOrDefault(d => d.Source == oldDict.Source);
                if (existing != null)
                {
                    dictionaries.Remove(existing);
                }
            }

            // 如果已存在相同资源地址，不重复添加
            if (!dictionaries.Any(d => d.Source == newDict.Source))
            {
                dictionaries.Add(newDict);
            }
        }

        /// <summary>
        /// 修改主题模板
        /// </summary>
        /// <param name="skinType"></param>
        public static async Task UpdateMaterialDesignThemeAsync(SkinType skinType)
        {
            await ModifyThemeAsync(theme =>
             {
                 return Task.Run(() =>
                 {
                     if (theme is Theme internalTheme)
                     {
                         switch (skinType)
                         {
                             case SkinType.Dark:
                                 internalTheme.SetDarkTheme();//模板
                                 internalTheme.SetPrimaryColor((Color)ColorConverter.ConvertFromString("#505050")); //主要颜色
                                 internalTheme.SetSecondaryColor(Colors.White);//次要颜色
                                 break;
                             case SkinType.Light:
                                 internalTheme.SetLightTheme();//模板
                                 internalTheme.SetPrimaryColor((Color)ColorConverter.ConvertFromString("#F6F6F6"));//主要颜色
                                 internalTheme.SetSecondaryColor(Colors.Black);//次要颜色
                                 break;
                         }
                     }
                 });
             });
        }

        /// <summary>
        /// 获取当前皮肤类型（从本地配置读取）
        /// </summary>
        public static async Task<SkinType> GetSkinAsync() => await ObtainAsync();

        /// <summary>
        /// 获取当前皮肤类型（从本地配置读取）
        /// </summary>
        public static SkinType GetSkin() => Obtain();

        /// <summary>
        /// 获取本地配置中的皮肤设置（若无则创建默认配置）
        /// </summary>
        public static SkinType Obtain()
        {
            try
            {
                if (!File.Exists(_pathSkin))
                {
                    Save(SkinType.Dark); // 默认保存为 Dark
                }
                var model = File.ReadAllText(_pathSkin).ToJsonEntity<UseSkinModel>();
                return model.SkinType;
            }
            catch
            {
                return SkinType.Dark; // 容错返回默认值
            }
        }

        /// <summary>
        /// 获取本地配置中的皮肤设置（若无则创建默认配置）
        /// </summary>
        public static async Task<SkinType> ObtainAsync()
        {
            try
            {
                if (!File.Exists(_pathSkin))
                {
                    await SaveAsync(SkinType.Dark); // 默认保存为 Dark
                }
                var model = File.ReadAllText(_pathSkin).ToJsonEntity<UseSkinModel>();
                return model.SkinType;
            }
            catch
            {
                return SkinType.Dark; // 容错返回默认值
            }
        }

        /// <summary>
        /// 保存当前皮肤设置到本地配置文件
        /// </summary>
        public static void Save(SkinType skinType)
        {
            // 确保路径存在
            if (!Directory.Exists(WindowHandler.BasePath))
            {
                Directory.CreateDirectory(WindowHandler.BasePath);
            }

            File.WriteAllText(_pathSkin, new UseSkinModel(skinType).ToJson());
        }

        /// <summary>
        /// 保存当前皮肤设置到本地配置文件
        /// </summary>
        public static async Task SaveAsync(SkinType skinType)
        {
            // 确保路径存在
            if (!Directory.Exists(WindowHandler.BasePath))
            {
                Directory.CreateDirectory(WindowHandler.BasePath);
            }

            await File.WriteAllTextAsync(_pathSkin, new UseSkinModel(skinType).ToJson());
        }

        #endregion
    }
}
