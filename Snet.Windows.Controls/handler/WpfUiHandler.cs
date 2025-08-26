using Snet.Core.handler;
using Snet.Model.data;
using Snet.Unility;
using Snet.Windows.Core.@enum;
using Snet.Windows.Core.handler;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Snet.Windows.Controls.handler
{
    /// <summary>
    /// WPFUI的汉堡菜单处理类
    /// </summary>
    public static class WpfUiHandler
    {
        /// <summary>
        /// WPFUI的皮肤处理
        /// </summary>
        /// <param name="grid">表格</param>
        /// <param name="skin">皮肤</param>
        public static async Task WpfUI_SkinUpdate(this Grid grid, SkinType? skin)
        {
            await grid.Dispatcher.InvokeAsync(() =>
            {
                //设置默认样式
                skin ??= SkinType.Dark;

                //格式
                string format = "pack://application:,,,/Wpf.Ui;component/Resources/Theme/{0}.xaml";

                // 新的资源地址
                string newResource = string.Format(format, skin);

                //新的资源对象
                ResourceDictionary newResourceDictionary = new ResourceDictionary { Source = new Uri(newResource, UriKind.RelativeOrAbsolute) };

                //检索的旧资源对象
                ResourceDictionary oldResourceDictionary = default;

                //检索资源
                foreach (var item in Application.Current.Resources.MergedDictionaries)
                {
                    if (item.Source != null)
                    {
                        string light = string.Format(format, "Light");
                        string dark = string.Format(format, "Dark");

                        switch (skin)
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

                // 替换资源
                grid.ReplaceResources(newResourceDictionary, oldResourceDictionary);
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 替换资源
        /// </summary>
        /// <param name="app">grid控件</param>
        /// <param name="newDict">新资源</param>
        /// <param name="oldDict">旧资源</param>
        private static void ReplaceResources(this Grid app, ResourceDictionary newDict, ResourceDictionary oldDict)
        {
            app.Resources.BeginInit();
            try
            {
                if (newDict != null && !app.Resources.MergedDictionaries.Contains(newDict))
                {
                    app.Resources.MergedDictionaries.Add(newDict);
                }
                if (oldDict != null && app.Resources.MergedDictionaries.Contains(oldDict))
                {
                    app.Resources.MergedDictionaries.Remove(oldDict);
                }
            }
            finally
            {
                app.Resources.EndInit();
            }
        }
        /// <summary>
        /// WPFUI的皮肤处理
        /// </summary>
        /// <param name="skin">皮肤</param>
        /// <param name="grid">表格</param>
        public static async Task WpfUI_SkinUpdate(this SkinType? skin, Grid grid) => await WpfUI_SkinUpdate(grid, skin);

        /// <summary>
        /// 创建汉堡菜单的控件
        /// </summary>
        /// <param name="key">
        /// 名称<br/>
        /// multilingual = true 时，使用多语言，此属性就是多语言的键值<br/>
        /// multilingual = false 时，此属性则是正常显示的值
        /// </param>
        /// <param name="icon">图片</param>
        /// <param name="type">对应的界面</param>
        /// <param name="multilingual">多语言的情况下 model 必填</param>
        /// <param name="model">语言模型</param>
        /// <returns></returns>
        public static NavigationViewItem CreationControl(string key, SymbolRegular icon, Type type, bool multilingual = false, LanguageModel? model = null)
        {
            NavigationViewItem item = new NavigationViewItem()
            {
                NavigationCacheMode = NavigationCacheMode.Enabled,
                Icon = new SymbolIcon { Symbol = icon },
                TargetPageType = type,
                TargetPageTag = key,
            };
            if (multilingual)
            {
                item.Content = model.GetLanguageValue(key);
                item.ContentStringFormat = key;
            }
            else
            {
                item.Content = key;
            }
            return item;
        }

        /// <summary>
        /// 设置汉堡菜单默认项,只允许设置一次
        /// </summary>
        /// <param name="window">窗口</param>
        /// <param name="navigation">汉堡菜单对象</param>
        /// <param name="type">默认打开的界面</param>
        /// <param name="model">语言模型</param>
        /// <param name="autoZoom">设置一个值，窗体大于此值则自动打开菜单，反之隐藏，0 不使用此功能</param>
        public static void SelectNavigationViewDefaultItem(this Window window, NavigationView navigation, Type type, LanguageModel model, int autoZoom)
        {
            //窗体加载完成后设置默认打开界面
            window.Loaded += (object sender, System.Windows.RoutedEventArgs e)
                => navigation.Navigate(type);

            if (autoZoom != 0)
            {
                //自动设置汉堡菜单的宽度
                window.SizeChanged += (object sender, SizeChangedEventArgs e)
                    => navigation.IsPaneOpen = e.NewSize.Width > autoZoom;
            }

            //获取包裹汉堡菜单的Grid
            Grid? grid = window.FindName("NavigationViewControlsGrid") as Grid;

            //设置汉堡菜单皮肤
            SkinHandler.OnSkinEventAsync += async (object? sender, Windows.Core.data.EventSkinResult e)
                => await grid?.WpfUI_SkinUpdate(e.Skin);

            //语言切换
            Snet.Core.handler.LanguageHandler.OnLanguageEventAsync += (object? sender, Snet.Model.data.EventLanguageResult e)
                => LanguageHandler_OnLanguageEventAsync(sender, e, navigation, model);
        }

        /// <summary>
        /// 语言切换通知事件
        /// </summary>
        private static async Task LanguageHandler_OnLanguageEventAsync(object? sender, Snet.Model.data.EventLanguageResult e, NavigationView navigation, LanguageModel model)
        {
            foreach (NavigationViewItem item in navigation.MenuItems)
            {
                if (!item.ContentStringFormat.IsNullOrWhiteSpace())
                {
                    item.Content = await model.GetLanguageValueAsync(item.ContentStringFormat);
                }
                if (item.MenuItems.Count > 0)
                {
                    foreach (NavigationViewItem subItem in item.MenuItems)
                    {
                        if (!subItem.ContentStringFormat.IsNullOrWhiteSpace())
                        {
                            subItem.Content = await model.GetLanguageValueAsync(subItem.ContentStringFormat);
                        }
                    }
                }
            }

            foreach (NavigationViewItem item in navigation.FooterMenuItems)
            {
                if (!item.ContentStringFormat.IsNullOrWhiteSpace())
                {
                    item.Content = await model.GetLanguageValueAsync(item.ContentStringFormat);
                }
                if (item.MenuItems.Count > 0)
                {
                    foreach (NavigationViewItem subItem in item.MenuItems)
                    {
                        if (!subItem.ContentStringFormat.IsNullOrWhiteSpace())
                        {
                            subItem.Content = await model.GetLanguageValueAsync(subItem.ContentStringFormat);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 设置汉堡菜单默认项
        /// </summary>
        /// <param name="navigation">汉堡菜单对象</param>
        /// <param name="window">窗口</param>
        /// <param name="type">默认打开的界面</param>
        /// <param name="model">语言模型</param>
        /// <<param name="autoZoom">设置一个值，窗体大于此值则自动打开菜单，反之隐藏，0 不使用此功能</param>
        public static void SelectNavigationViewDefaultItem(this NavigationView navigation, Window window, Type type, LanguageModel model, int autoZoom)
            => SelectNavigationViewDefaultItem(window, navigation, type, model, autoZoom);


    }
}
