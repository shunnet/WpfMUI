using MaterialDesignThemes.Wpf;
using Snet.Log;
using Snet.Model.@event;
using Snet.Utility;
using Snet.Windows.Core.data;
using Snet.Windows.Core.@enum;
using System.IO;
using System.Windows;
using Wpf.Ui.Appearance;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace Snet.Windows.Core.handler
{
    /// <summary>
    /// 皮肤（主题）处理器，统一管理应用程序的主题切换。<br/>
    /// 不支持异步操作
    /// </summary>
    public class SkinHandler
    {
        #region 事件定义

        /// <summary>
        /// 同步皮肤切换事件
        /// </summary>
        public static event EventHandler<EventSkinResult>? OnSkinEvent;

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
        private static Task OnSkinEventHandlerAsync(object? sender, EventSkinResult e)
        {
            OnSkinEvent?.Invoke(sender, e);
            return OnSkinEventWrapperAsync.InvokeAsync(sender, e);
        }

        private static void ObserveSkinEvent(Task notificationTask)
        {
            notificationTask.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    notificationTask.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"Skin change event handler failed: {ex}", "Snet.Windows.Core", ex);
                }
            });
        }

        #endregion

        #region 私有字段

        /// <summary>
        /// 皮肤配置文件路径
        /// </summary>
        private static readonly string _pathSkin = Path.Combine(WindowHandler.BasePath, "skin.json");

        /// <summary>
        /// 获取默认语言模型（使用内置资源）
        /// </summary>
        private static readonly Snet.Model.data.LanguageModel _skinLanguageModel = new("Snet.Windows.Controls", "Language", "Snet.Windows.Controls.dll");

        /// <summary>
        /// 浅色主题资源字典 URI（静态缓存，避免每次切换时重复拼接字符串）
        /// </summary>
        private static readonly string _lightThemeUri = "pack://application:,,,/Snet.Windows.Core;component/themes/LightTheme.xaml";

        /// <summary>
        /// 深色主题资源字典 URI（静态缓存，避免每次切换时重复拼接字符串）
        /// </summary>
        private static readonly string _darkThemeUri = "pack://application:,,,/Snet.Windows.Core;component/themes/DarkTheme.xaml";

        /// <summary>
        /// MaterialDesign 深色主题颜色常量（静态缓存，避免每次切换时重复调用 ColorConverter.ConvertFromString）
        /// </summary>
        private static readonly Color _darkPrimaryColor = (Color)ColorConverter.ConvertFromString("#505050");
        private static readonly Color _darkSecondaryColor = (Color)ColorConverter.ConvertFromString("#F0E8E8");
        private static readonly Color _darkPrimaryLight = (Color)ColorConverter.ConvertFromString("#616161");

        /// <summary>
        /// MaterialDesign 浅色主题颜色常量（静态缓存，避免每次切换时重复调用 ColorConverter.ConvertFromString）
        /// </summary>
        private static readonly Color _lightPrimaryColor = (Color)ColorConverter.ConvertFromString("#F5F5F5");
        private static readonly Color _lightSecondaryColor = (Color)ColorConverter.ConvertFromString("#272424");
        private static readonly Color _lightPrimaryLight = (Color)ColorConverter.ConvertFromString("#C6C6C6");
        /// <summary>
        /// 白天模板的背景颜色
        /// </summary>
        private static readonly Color _lightCardsBackground = (Color)ColorConverter.ConvertFromString("#FEFEFE");

        #endregion

        #region 私有方法

        /// <summary>
        /// MaterialDesign 调色板辅助器（单例缓存，避免重复创建）
        /// </summary>
        private static readonly PaletteHelper paletteHelper = new();

        /// <summary>
        /// 修改当前 MaterialDesign 主题样式。<br/>
        /// 获取当前主题对象，执行调用者指定的修改操作后重新应用。<br/>
        /// 注意：Theme 对象绑定 UI 线程，modificationFunc 中不可跨线程操作。<br/>
        /// 若出现异常记录日志而不是静默失败（否则皮肤切换后 MaterialDesign 颜色看似"不生效"）。
        /// </summary>
        /// <param name="modificationFunc">对 Theme 对象执行的修改操作（异步委托）</param>
        private static void ModifyTheme(Action<Theme> modificationFunc)
        {
            try
            {
                Theme theme = paletteHelper.GetTheme();
                if (modificationFunc != null)
                {
                    modificationFunc(theme);
                }
                paletteHelper.SetTheme(theme);
            }
            catch (Exception ex)
            {
                // MaterialDesign Theme 可能因资源结构问题（如缺少 IMaterialDesignThemeDictionary 且应用根资源不可写）
                // 导致 SetTheme 失败；记录日志便于定位，避免切换静默失效
                LogHelper.Error($"MaterialDesign 主题切换异常：{ex.Message}", "Snet.Windows.Core", ex);
                throw;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置指定皮肤类型的主题样式并保存
        /// </summary>
        public static void SetSkin(SkinType skinType, bool notice = true)
        {
            // 使用缓存的资源 URI，避免每次切换时重复拼接字符串
            string newResource = skinType == SkinType.Dark ? _darkThemeUri : _lightThemeUri;
            string oldThemeUri = skinType == SkinType.Dark ? _lightThemeUri : _darkThemeUri;

            //新的资源对象
            ResourceDictionary newResourceDictionary = new ResourceDictionary { Source = new Uri(newResource, UriKind.RelativeOrAbsolute) };

            //检索的旧资源对象
            ResourceDictionary oldResourceDictionary = default;

            //检索旧主题资源字典（找到即退出，避免无谓遍历）
            foreach (var item in Application.Current.Resources.MergedDictionaries)
            {
                if (item.Source != null && item.Source.AbsoluteUri == oldThemeUri)
                {
                    oldResourceDictionary = item;
                    break;
                }
            }

            //替换资源
            ReplaceResources(newResourceDictionary, oldResourceDictionary);

            // 修改 MaterialDesign 主题
            UpdateMaterialDesignTheme(skinType);

            // 修改 Wpf.Ui 主题
            UpdateWpfUI(skinType);

            // Persist before announcing success so subscribers observe the new durable state.
            Save(skinType);

            //是否通知
            if (notice)
            {
                var eventArgs = new EventSkinResult(
                    true,
                    Snet.Core.handler.LanguageHandler.GetLanguageValue("皮肤设置成功", _skinLanguageModel) ?? string.Empty,
                    skinType);
                ObserveSkinEvent(OnSkinEventHandlerAsync(skinType == SkinType.Dark ? "#505050" : "#F5F5F5", eventArgs));
            }
        }

        /// <summary>
        /// 替换全局资源（线程安全、最小化 UI 闪烁）
        /// </summary>
        /// <param name="newDict">新资源</param>
        /// <param name="oldDict">旧资源</param>
        public static void ReplaceResources(ResourceDictionary newDict, ResourceDictionary? oldDict)
        {
            var app = Application.Current;
            if (app == null || newDict == null) return;

            // 确保在 UI 线程执行
            if (!app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.Invoke(() => ReplaceResources(newDict, oldDict));
                return;
            }

            var dictionaries = app.Resources.MergedDictionaries;
            var newSource = newDict.Source;

            // 如果新旧相同，直接返回
            if (oldDict != null && newSource == oldDict.Source)
                return;

            // 移除旧资源（若存在）
            if (oldDict?.Source != null)
            {
                for (int i = dictionaries.Count - 1; i >= 0; i--)
                {
                    if (dictionaries[i].Source == oldDict.Source)
                    {
                        dictionaries.RemoveAt(i);
                        break; // 找到就退出
                    }
                }
            }

            // 添加新资源（避免重复）
            if (newSource != null)
            {
                for (int i = 0; i < dictionaries.Count; i++)
                {
                    if (dictionaries[i].Source == newSource)
                        return; // 已存在，直接退出
                }
                dictionaries.Add(newDict);
            }
        }

        /// <summary>
        /// 更新 MaterialDesign 主题样式。<br/>
        /// 根据皮肤类型设置主要色、次要色和高亮色，<br/>
        /// 并切换明暗主题模板。
        /// </summary>
        /// <param name="skinType">目标皮肤类型</param>
        public static void UpdateMaterialDesignTheme(SkinType skinType)
        {
            ModifyTheme(theme =>
            {
                // 主题对象绑定 UI 线程，不可在 Task.Run 中操作
                if (theme is Theme internalTheme)
                {
                    switch (skinType)
                    {
                        case SkinType.Dark:
                            internalTheme.SetDarkTheme();
                            internalTheme.SetPrimaryColor(_darkPrimaryColor);
                            internalTheme.SetSecondaryColor(_darkSecondaryColor);
                            internalTheme.PrimaryLight = _darkPrimaryLight;
                            break;
                        case SkinType.Light:
                            internalTheme.SetLightTheme();
                            internalTheme.SetPrimaryColor(_lightPrimaryColor);
                            internalTheme.SetSecondaryColor(_lightSecondaryColor);
                            internalTheme.PrimaryLight = _lightPrimaryLight;
                            internalTheme.Cards.Background = _lightCardsBackground;
                            break;
                    }
                }
            });
        }

        /// <summary>
        /// 更新 Wpf.Ui 主题样式。<br/>
        /// 通过 ApplicationThemeManager 应用明暗主题。
        /// </summary>
        /// <param name="skinType">目标皮肤类型</param>
        /// <returns>已完成的任务</returns>
        public static void UpdateWpfUI(SkinType skinType)
        {
            ApplicationThemeManager.Apply(skinType == SkinType.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light);
        }

        /// <summary>
        /// 获取当前皮肤类型（从本地配置读取）
        /// </summary>
        public static SkinType GetSkin()
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
        #endregion
    }
}
