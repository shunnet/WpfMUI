using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Snet.Log;
using Snet.Model.@enum;
using Snet.Windows.Core.data;
using Snet.Windows.Core.@enum;
using Snet.Windows.Core.handler;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.Windows.Threading;
using static Snet.Windows.Core.handler.WindowHandler;

namespace Snet.Windows.Core
{
    /// <summary>
    /// 自定义窗口基类，提供皮肤切换、语言切换、窗口控制等基础功能
    /// </summary>
    public class WindowBase : System.Windows.Window
    {
        #region 依赖属性

        /// <summary>
        /// 语言切换功能
        /// </summary>
        public static readonly DependencyProperty LanguageEnabledProperty =
            DependencyProperty.Register("LanguageEnabled", typeof(bool), typeof(WindowBase), new UIPropertyMetadata(true));

        /// <summary>
        /// 皮肤切换功能
        /// </summary>
        public static readonly DependencyProperty SkinEnabledProperty =
            DependencyProperty.Register("SkinEnabled", typeof(bool), typeof(WindowBase), new UIPropertyMetadata(true));

        /// <summary>
        /// 标题靠左
        /// </summary>
        public static readonly DependencyProperty TitleLeftProperty =
            DependencyProperty.Register("TitleLeft", typeof(bool), typeof(WindowBase), new UIPropertyMetadata(true));

        /// <summary>
        /// 底部版本显示
        /// </summary>
        public static readonly DependencyProperty VerEnabledProperty =
            DependencyProperty.Register("VerEnabled", typeof(bool), typeof(WindowBase), new UIPropertyMetadata(true));

        /// <summary>
        /// 加载动画<br/>
        /// 窗体加载完成在显示
        /// </summary>
        public static readonly DependencyProperty LoadAnimationEnabledProperty =
            DependencyProperty.Register("LoadAnimationEnabled", typeof(bool), typeof(WindowBase), new UIPropertyMetadata(false));

        #endregion

        #region 控件字段

        private Button? closeButton;         // 关闭按钮
        private Button? maximizeButton;     // 最大化按钮
        private Button? minimizeButton;      // 最小化按钮
        private Button? normalButton;        // 还原按钮
        private TextBlock? systemVer;            // 系统版本标签
        private FrameworkElement? ver;       // 版本元素

        #endregion

        #region 构造函数和静态构造函数

        /// <summary>
        /// 静态构造函数，重写样式属性元数据
        /// </summary>
        static WindowBase()
        {
            // 设置初始皮肤
            SkinHandler.SetSkin(SkinHandler.GetSkin(), false);
            // 设置初始语言
            LanguageHandler.SetLanguage(LanguageHandler.GetLanguage());
            StyleProperty.OverrideMetadata(typeof(WindowBase), new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceStyle)));
        }

        /// <summary>
        /// 构造函数，初始化窗口基础设置
        /// </summary>
        public WindowBase()
        {
            //绑定点击命令
            LanguageCommand = new AsyncRelayCommand(OnLanguageCommand);
            SkinCommand = new AsyncRelayCommand(OnSkinCommand);
            this.Loaded += OnLoaded;
            this.SizeChanged += OnSizeChanged;
            this.SourceInitialized += OnSourceInitialized;
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 设置初始皮肤
                SkinHandler.SetSkin(SkinHandler.GetSkin(), true);

            }, DispatcherPriority.Loaded);
        }

        #endregion

        #region 命令绑定
        /// <summary>
        /// 皮肤切换命令
        /// </summary>
        public static readonly DependencyProperty SkinCommandProperty = DependencyProperty.Register(nameof(SkinCommand), typeof(AsyncRelayCommand), typeof(WindowBase), new PropertyMetadata(null));
        public IAsyncRelayCommand SkinCommand
        {
            get => (AsyncRelayCommand)GetValue(SkinCommandProperty);
            set => SetValue(SkinCommandProperty, value);
        }
        private async Task OnSkinCommand()
            => SkinHandler.SetSkin(SkinHandler.GetSkin() == SkinType.Dark ? SkinType.Light : SkinType.Dark);


        /// <summary>
        /// 语言切换命令
        /// </summary>
        public static readonly DependencyProperty LanguageCommandProperty = DependencyProperty.Register(nameof(LanguageCommand), typeof(AsyncRelayCommand), typeof(WindowBase), new PropertyMetadata(null));
        public IAsyncRelayCommand LanguageCommand
        {
            get => (AsyncRelayCommand)GetValue(LanguageCommandProperty);
            set => SetValue(LanguageCommandProperty, value);
        }
        private async Task OnLanguageCommand()
            => LanguageHandler.SetLanguage(LanguageHandler.GetLanguage() == LanguageType.zh ? LanguageType.en : LanguageType.zh);


        #endregion

        #region 事件
        private bool _isResizing;
        /// <summary>
        /// 窗体大小改变，仅在变大时处理白边修复
        /// </summary>
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isResizing)
                return;
            if (e.NewSize.Width <= e.PreviousSize.Width && e.NewSize.Height <= e.PreviousSize.Height)
                return;

            _isResizing = true;

            // 先取消边框和玻璃区，防止白边
            WindowChrome.GetWindowChrome(this).GlassFrameThickness = new Thickness(0);

            _ = Task.Run(async () =>
            {
                try
                {
                    // 等待一段时间，确保窗口大小调整完成
                    await Task.Delay(135);

                    // 在 UI 线程恢复边框
                    await Dispatcher.InvokeAsync(() =>
                    {
                        // 避免窗口已关闭后再访问
                        if (!IsLoaded) return;

                        // 恢复边框和玻璃区
                        WindowChrome.GetWindowChrome(this).GlassFrameThickness = new Thickness(1);

                        _isResizing = false;
                    });
                }
                catch
                {
                    _isResizing = false;
                }
            });
        }

        /// <summary>
        /// 窗体加载完成
        /// </summary>
        /// <param name="sender">源</param>
        /// <param name="e">事件</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //设置统一圆角
            var resources = Application.Current.Resources;
            double value = (double)resources["WindowCornerRadius_Double"];
            resources["TopCornerRadius"] = new CornerRadius(value, value, 0, 0);
            resources["DownCornerRadius"] = new CornerRadius(0, 0, value, value);
            resources["CloseCornerRadius"] = new CornerRadius(0, value, 0, 0);
            resources["WindowCornerRadius"] = new CornerRadius(value);

            // 修改注册表优化显示效果
            UpdateRegistry();

            //自动缩放功能
            AutoAdjustAsync();

            //查看是什么加速
#if DEBUG
            int tier = (RenderCapability.Tier >> 16);
            string message;
            switch (tier)
            {
                case 0:
                    message = "Tier 0：无硬件加速，完全使用软件渲染。";
                    break;
                case 1:
                    message = "Tier 1：部分硬件加速，性能有限。";
                    break;
                case 2:
                    message = "Tier 2：完全硬件加速，使用 GPU 渲染。";
                    break;
                default:
                    message = "未知渲染等级。";
                    break;
            }
            LogHelper.Verbose(message);
#endif
        }

        /// <summary>
        /// 引发此事件是为了支持与Win32的互操作
        /// </summary>
        /// <param name="sender">源</param>
        /// <param name="e">事件</param>
        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            UpdateDpi(); // 初始化时调用一次
        }
        #endregion

        #region 属性访问器

        /// <summary>
        /// 获取或设置是否启用语言切换功能
        /// </summary>
        public bool LanguageEnabled
        {
            get => (bool)GetValue(LanguageEnabledProperty);
            set => SetValue(LanguageEnabledProperty, value);
        }

        /// <summary>
        /// 获取或设置是否启用皮肤切换功能
        /// </summary>
        public bool SkinEnabled
        {
            get => (bool)GetValue(SkinEnabledProperty);
            set => SetValue(SkinEnabledProperty, value);
        }

        /// <summary>
        /// 获取或设置标题是否靠左显示
        /// </summary>
        public bool TitleLeft
        {
            get => (bool)GetValue(TitleLeftProperty);
            set => SetValue(TitleLeftProperty, value);
        }

        /// <summary>
        /// 获取或设置是否显示版本号
        /// </summary>
        public bool VerEnabled
        {
            get => (bool)GetValue(VerEnabledProperty);
            set => SetValue(VerEnabledProperty, value);
        }

        /// <summary>
        /// 加载动画
        /// </summary>
        public bool LoadAnimationEnabled
        {
            get => (bool)GetValue(LoadAnimationEnabledProperty);
            set => SetValue(LoadAnimationEnabledProperty, value);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 窗口抖动效果
        /// </summary>
        /// <param name="window">要抖动的窗口，如果为null则使用当前活动窗口</param>
        public static void WindowShake(System.Windows.Window window = null)
        {
            window ??= Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(o => o.IsActive);
            if (window == null) return;

            var doubleAnimation = new DoubleAnimation
            {
                From = window.Left,
                To = window.Left + 15,
                Duration = TimeSpan.FromMilliseconds(50),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3),
                FillBehavior = FillBehavior.Stop
            };
            window.BeginAnimation(LeftProperty, doubleAnimation);
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 应用模板时调用，初始化窗口控件
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            LoadAnimation(LoadAnimationEnabled);
            InitializeTemplateControls();
        }

        /// <summary>
        /// 窗口源初始化时调用，设置窗口消息处理
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
            base.OnSourceInitialized(e);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 动画启动
        /// </summary>
        /// <param name="status">状态</param>
        public async Task LoadAnimation(bool status)
        {
            if (status)
            {
                if (GetTemplateChild("PART_ClientArea") is UIElement clientArea && GetTemplateChild("PART_LoadAnimation") is UIElement animationArea)
                {
                    // 确保一开始动画区域是可见的，客户端区域是隐藏的
                    animationArea.Opacity = 1;
                    animationArea.Visibility = Visibility.Visible;

                    clientArea.Opacity = 0;
                    clientArea.Visibility = Visibility.Visible; // opacity 为 0 时不会显示实际内容
                    clientArea.IsEnabled = false;
                    // 创建 clientArea 的淡入动画
                    var clientFadeIn = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(2000),
                        FillBehavior = FillBehavior.HoldEnd
                    };

                    // 创建 animationArea 的淡出动画
                    var animationFadeOut = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(2000),
                        FillBehavior = FillBehavior.Stop
                    };

                    animationFadeOut.Completed += (s, e) =>
                    {
                        // 动画完成后真正隐藏 animationArea
                        animationArea.Visibility = Visibility.Collapsed;
                        animationArea.Opacity = 1; // 重置为默认（避免下一次动画不生效）
                        clientArea.IsEnabled = true;
                    };
                    await Task.Delay(3000);
                    // 启动动画
                    clientArea.BeginAnimation(UIElement.OpacityProperty, clientFadeIn);
                    animationArea.BeginAnimation(UIElement.OpacityProperty, animationFadeOut);
                }
            }
        }

        /// <summary>
        /// 自动根据当前屏幕与 DPI 缩放窗口大小，并保持窗口居中与四周等边距视觉效果。
        /// </summary>
        private Task AutoAdjustAsync()
        {
            // 获取显示器信息
            GetCursorPos(out POINT_struct pt);
            IntPtr hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);

            MONITORINFO mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            GetMonitorInfo(hMonitor, mi);

            // 计算物理像素尺寸
            int pxWidth = mi.rcWork.right - mi.rcWork.left;
            int pxHeight = mi.rcWork.bottom - mi.rcWork.top;

            // 获取DPI缩放
            GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _);
            double dpiScale = dpiX / 96.0;

            // 计算窗口物理像素尺寸
            double pxWindowWidth = ActualWidth * dpiScale;
            double pxWindowHeight = ActualHeight * dpiScale;

            // 如果窗口物理尺寸小于等于屏幕工作区，直接居中显示
            if (pxWindowWidth <= pxWidth && pxWindowHeight <= pxHeight)
            {
                return Task.CompletedTask;
            }

            // 需要缩放的情况
            double fitScale = Math.Min(
                Math.Min(pxWidth / pxWindowWidth, pxHeight / pxWindowHeight),
                0.7);

            Dispatcher.Invoke(() =>
            {
                LayoutTransform = new ScaleTransform(fitScale, fitScale);
                Width = ActualWidth * fitScale;
                Height = ActualHeight * fitScale;

                // 计算缩放后的物理尺寸
                double scaledPxWidth = Width * dpiScale;
                double scaledPxHeight = Height * dpiScale;

                // 居中显示
                Left = mi.rcWork.left + (pxWidth - scaledPxWidth) / 2;
                Top = mi.rcWork.top + (pxHeight - scaledPxHeight) / 2;
            });

            return Task.CompletedTask;
        }


        /// <summary>
        /// 初始化模板中的控件并设置事件
        /// </summary>
        private async Task InitializeTemplateControls()
        {
            // 设置标题对齐方式
            if (!TitleLeft && GetTemplateChild("PART_CaptionText") is TextBlock captionText)
            {
                captionText.HorizontalAlignment = HorizontalAlignment.Center;
            }

            // 初始化窗口控制按钮
            InitializeWindowButtons();

            // 初始化版本显示
            InitializeVersionDisplay();
        }

        /// <summary>
        /// 初始化窗口控制按钮（最小化、最大化/还原、关闭）
        /// </summary>
        private void InitializeWindowButtons()
        {
            minimizeButton = GetTemplateChild("PART_MinimizeButton") as Button;
            maximizeButton = GetTemplateChild("PART_MaximizeButton") as Button;
            normalButton = GetTemplateChild("PART_NormalButton") as Button;
            closeButton = GetTemplateChild("PART_CloseButton") as Button;

            AddClickHandler(minimizeButton, OnWindowMinimizing);
            AddClickHandler(maximizeButton, OnWindowStateRestoring);
            AddClickHandler(normalButton, OnWindowStateRestoring);
            AddClickHandler(closeButton, OnWindowClosing);
        }

        /// <summary>
        /// 初始化版本显示
        /// </summary>
        private void InitializeVersionDisplay()
        {
            systemVer = GetTemplateChild("System_Ver") as TextBlock;
            ver = GetTemplateChild("PART_Ver") as FrameworkElement;

            if (VerEnabled && systemVer != null)
            {
                systemVer.Text = System.Diagnostics.FileVersionInfo
                    .GetVersionInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)
                    .FileVersion;
            }
            else if (ver != null)
            {
                ver.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 强制样式回调
        /// </summary>
        private static object OnCoerceStyle(DependencyObject d, object baseValue)
        {
            if (null == baseValue)
            {
                baseValue = (d as FrameworkElement).TryFindResource(typeof(WindowBase));
            }
            return baseValue;
        }

        #endregion

        #region 窗口控制事件处理

        /// <summary>
        /// 关闭窗口事件处理
        /// </summary>
        private void OnWindowClosing(object sender, RoutedEventArgs e)
            => this.Close();

        /// <summary>
        /// 最小化窗口事件处理
        /// </summary>
        private void OnWindowMinimizing(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        /// <summary>
        /// 最大化/还原窗口事件处理
        /// </summary>
        private void OnWindowStateRestoring(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        #endregion

        #region 语言和皮肤操作

        /// <summary>
        /// 语言切换事件处理
        /// </summary>
        private void OnLanguage(object sender, RoutedEventArgs e)
        {
            var language = LanguageHandler.GetLanguage() == LanguageType.zh
                ? LanguageType.en
                : LanguageType.zh;

            LanguageHandler.SetLanguage(language);
        }

        /// <summary>
        /// 皮肤切换事件处理
        /// </summary>
        private void OnSkin(object sender, RoutedEventArgs e)
        {
            var skin = SkinHandler.GetSkin() == SkinType.Dark
                ? SkinType.Light
                : SkinType.Dark;
            Application.Current.Dispatcher.InvokeAsync(() => SkinHandler.SetSkin(skin));
        }

        #endregion

        #region 注册表操作（优化显示效果）

        private readonly string registrieIni = $"{WindowHandler.BasePath}\\registrie.ini";

        private readonly List<RegistryModel> registries = new()
        {
            // 设置为"自定义视觉效果"
            new RegistryModel("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects",
                "VisualFXSetting", 3, RegistryValueKind.DWord),
            // 修改 VisualEffects 子项
            new RegistryModel("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\\DragFullWindows",
                "DefaultApplied", 0, RegistryValueKind.DWord),
            // 修改 Control Panel\Desktop
            new RegistryModel("Control Panel\\Desktop",
                "DragFullWindows", "0", RegistryValueKind.String),
        };

        /// <summary>
        /// 修改注册表优化窗口拖动显示效果
        /// </summary>
        private void UpdateRegistry()
        {
            if (File.Exists(registrieIni)) return;

            foreach (var item in registries)
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(item.Path, true);
                    if (key == null)
                    {
                        continue;
                    }

                    var value = key.GetValue(item.Name);
                    if (value == null)
                    {
                        continue;
                    }

                    if (value.ToString() != item.Value.ToString())
                    {
                        key.SetValue(item.Name, item.Value, item.ValueType);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"修改注册表 {item.Name} 异常：{ex.Message}", exception: ex);
                }
            }

            // 通知系统设置已更改
            const uint SPI_SETDRAGFULLWINDOWS = 0x0025;
            const uint SPIF_UPDATEINIFILE = 0x01;
            const uint SPIF_SENDCHANGE = 0x02;
            SystemParametersInfo(SPI_SETDRAGFULLWINDOWS, 0, null, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

            //文件夹不存在则创建
            if (!Directory.Exists(WindowHandler.BasePath))
            {
                Directory.CreateDirectory(WindowHandler.BasePath);
            }

            // 创建标记文件
            File.WriteAllText(registrieIni,
                "1. 修改注册表使软件呈现效果更佳\r\n" +
                "2. 此文件存在代表注册表已修改成功\r\n" +
                "3. 请勿删除!!!", Encoding.UTF8);
        }

        #endregion

        #region 窗口消息处理（双击放大缩小、单击长按移动）

        private double _dpiX = 1.0, _dpiY = 1.0;

        /// <summary>
        /// 更新当前窗口的 DPI 缩放比（建议在 SourceInitialized 或 Loaded 中调用）
        /// </summary>
        private void UpdateDpi()
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            _dpiX = dpi.DpiScaleX;
            _dpiY = dpi.DpiScaleY;
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_EXITSIZEMOVE = 0x0232;
            const int WM_GETMINMAXINFO = 0x0024;
            if (msg == WM_GETMINMAXINFO)
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }
            if (msg == WM_EXITSIZEMOVE)
            {
                UpdateSnapState(hwnd); // 这里才去判断是否贴边
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 检测窗口是否已经贴顶且高度等于工作区域高度，
        /// 如果是，则主动设置窗口状态为 Maximized，用于解决贴边但未最大化时系统视觉圆角被移除的问题。
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        private void UpdateSnapState(IntPtr hwnd)
        {
            // 获取当前窗口所在的显示器（如果在多显示器环境中）
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero)
                return;

            // 获取显示器的工作区域（排除任务栏、停靠栏）
            MONITORINFO mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(monitor, mi))
                return;

            int workHeight = mi.rcWork.bottom - mi.rcWork.top;
            double winHeight = this.Height;
            double height = workHeight - winHeight;
            if (height <= 2)
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        /// <summary>
        /// 处理最大化限制和最小尺寸限制（支持 DPI 缩放）
        /// </summary>
        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            MONITORINFO monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(monitor, monitorInfo))
                return;

            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

            RECT rcWorkArea = monitorInfo.rcWork;
            RECT rcMonitorArea = monitorInfo.rcMonitor;

            // 设置最大化位置（排除任务栏区域）
            mmi.ptMaxPosition.x = rcWorkArea.left - rcMonitorArea.left;
            mmi.ptMaxPosition.y = rcWorkArea.top - rcMonitorArea.top;

            mmi.ptMaxSize.x = rcWorkArea.right - rcWorkArea.left;
            mmi.ptMaxSize.y = rcWorkArea.bottom - rcWorkArea.top - 1;

            // 最小宽高，支持 DPI 缩放（_dpiX/_dpiY 已缓存）
            mmi.ptMinTrackSize.x = (int)(this.MinWidth * _dpiX);
            mmi.ptMinTrackSize.y = (int)(this.MinHeight * _dpiY);

            Marshal.StructureToPtr(mmi, lParam, false);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 为按钮添加点击事件处理程序的扩展方法
        /// </summary>
        private static void AddClickHandler(Button button, RoutedEventHandler handler)
        {
            if (button != null)
            {
                button.Click += handler;
            }
        }
        #endregion
    }
}