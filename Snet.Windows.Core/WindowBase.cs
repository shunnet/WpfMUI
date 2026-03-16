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

        /// <summary>
        /// 最大化时的内边距补偿（用于抵消 GlassFrameThickness 导致的边框溢出）
        /// </summary>
        public static readonly DependencyProperty MaximizeBorderThicknessProperty =
            DependencyProperty.Register("MaximizeBorderThickness", typeof(Thickness), typeof(WindowBase), new UIPropertyMetadata(new Thickness(0)));

        #endregion

        #region 控件字段

        private Button? closeButton;              // 关闭按钮
        private Button? maximizeNormalButton;     // 最大化还原按钮
        private Button? minimizeButton;           // 最小化按钮
        private Border? maximizeBorder;           // 最大化补偿边框
        private TextBlock? systemVer;             // 系统版本标签
        private FrameworkElement? ver;            // 版本元素
        private HwndSource? _hwndSource;          // 窗口句柄源（用于设置合成背景色）
        private IntPtr _hBrush;                   // 缓存的 Win32 背景画刷句柄

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
        private Task OnSkinCommand()
        {
            SkinHandler.SetSkin(SkinHandler.GetSkin() == SkinType.Dark ? SkinType.Light : SkinType.Dark);
            Dispatcher.InvokeAsync(UpdateNativeBackground, DispatcherPriority.Loaded);
            return Task.CompletedTask;
        }


        /// <summary>
        /// 语言切换命令
        /// </summary>
        public static readonly DependencyProperty LanguageCommandProperty = DependencyProperty.Register(nameof(LanguageCommand), typeof(AsyncRelayCommand), typeof(WindowBase), new PropertyMetadata(null));
        public IAsyncRelayCommand LanguageCommand
        {
            get => (AsyncRelayCommand)GetValue(LanguageCommandProperty);
            set => SetValue(LanguageCommandProperty, value);
        }
        private Task OnLanguageCommand()
        {
            LanguageHandler.SetLanguage(LanguageHandler.GetLanguage() == LanguageType.zh ? LanguageType.en : LanguageType.zh);
            return Task.CompletedTask;
        }


        #endregion

        #region 事件

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

            // 设置原生背景色与主题匹配，消除拖拽/最大化过渡时的白色闪烁
            UpdateNativeBackground();

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
            UpdateMaximizeBorderThickness(); // 计算最大化内边距
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

        /// <summary>
        /// 获取最大化时的内边距补偿值
        /// </summary>
        public Thickness MaximizeBorderThickness
        {
            get => (Thickness)GetValue(MaximizeBorderThicknessProperty);
            set => SetValue(MaximizeBorderThicknessProperty, value);
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
            _ = LoadAnimationAsync(LoadAnimationEnabled);
            InitializeTemplateControls();
        }

        /// <summary>
        /// 窗口状态变化时立即更新最大化补偿内边距，
        /// 避免 XAML Trigger 中 Binding 异步解析导致的延迟闪烁
        /// </summary>
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (maximizeBorder != null)
            {
                maximizeBorder.Padding = WindowState == WindowState.Maximized
                    ? MaximizeBorderThickness
                    : new Thickness(0);
            }
        }

        /// <summary>
        /// 窗口源初始化时调用，设置窗口消息处理
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            _hwndSource = HwndSource.FromHwnd(handle);
            _hwndSource?.AddHook(WindowProc);
            base.OnSourceInitialized(e);
        }

        #endregion

        #region 私有方法
        /// <summary>
        /// 根据 ResizeMode 更新按钮的可见性
        /// </summary>
        private void UpdateButtonVisibility(Button maximizeNormalButton, Button minimizeButton)
        {
            switch (this.ResizeMode)
            {
                case ResizeMode.CanResize:
                    maximizeNormalButton.Visibility = Visibility.Visible;
                    minimizeButton.Visibility = Visibility.Visible;
                    break;
                case ResizeMode.CanMinimize:
                    maximizeNormalButton.Visibility = Visibility.Collapsed;
                    minimizeButton.Visibility = Visibility.Visible;
                    break;
                case ResizeMode.NoResize:
                    maximizeNormalButton.Visibility = Visibility.Collapsed;
                    minimizeButton.Visibility = Visibility.Collapsed;
                    break;
                case ResizeMode.CanResizeWithGrip:
                    if (maximizeNormalButton != null && minimizeButton != null)
                    {
                        maximizeNormalButton.Visibility = Visibility.Visible;
                        minimizeButton.Visibility = Visibility.Visible;
                    }
                    break;
            }
        }

        /// <summary>
        /// 动画启动（并行动画，真正异步等待）
        /// </summary>
        /// <param name="status">是否启用动画</param>
        /// <param name="cancellationToken">可选的取消标记</param>
        public async Task LoadAnimationAsync(bool status, CancellationToken cancellationToken = default)
        {
            if (!status) return;

            // 1. 获取模板中的元素
            if (GetTemplateChild("PART_ClientArea") is not UIElement clientArea ||
                GetTemplateChild("PART_LoadAnimation") is not UIElement animationArea)
                return;

            // 2. 初始化状态
            animationArea.Opacity = 1;
            animationArea.Visibility = Visibility.Visible;

            clientArea.Opacity = 0;
            clientArea.Visibility = Visibility.Visible; // opacity = 0 时仍可参与布局
            clientArea.IsEnabled = false;

            var duration = TimeSpan.FromMilliseconds(2000);

            await Task.Delay(duration, cancellationToken);

            // 3. 并行动画
            var fadeOutTask = AnimateAsync(
                animationArea,
                UIElement.OpacityProperty,
                new DoubleAnimation(1, 0, duration) { FillBehavior = FillBehavior.Stop },
                setFinalValue: false, // 动画结束后不强制保持 0，后续会设置 Visibility
                cancellationToken: cancellationToken);

            var fadeInTask = AnimateAsync(
                clientArea,
                UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, duration) { FillBehavior = FillBehavior.HoldEnd },
                setFinalValue: true, // 动画结束后强制保持 1
                cancellationToken: cancellationToken);

            // 4. 等待动画完成
            await Task.WhenAll(fadeOutTask, fadeInTask);

            // 5. 动画完成后处理
            animationArea.Visibility = Visibility.Collapsed;
            animationArea.Opacity = 1; // 重置为默认，避免下一次动画不生效
            clientArea.IsEnabled = true;
        }

        /// <summary>
        /// 异步执行动画，并在动画完成后返回
        /// </summary>
        /// <param name="target">目标对象，必须是 DependencyObject（如 Grid、Border、UserControl 等）</param>
        /// <param name="property">要动画的依赖属性（如 UIElement.OpacityProperty）</param>
        /// <param name="animation">动画实例（DoubleAnimation、ColorAnimation 等）</param>
        /// <param name="setFinalValue">是否在动画完成后强制设置最终值，避免 FillBehavior.Stop 复位</param>
        /// <param name="cancellationToken">可选的取消标记</param>
        private Task AnimateAsync(DependencyObject target, DependencyProperty property, DoubleAnimation animation, bool setFinalValue = false, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<object?>();

            // 1. 判空处理
            if (target == null || property == null || animation == null)
            {
                tcs.SetResult(null);
                return tcs.Task;
            }

            // 2. 确认可以执行动画
            if (target is not IAnimatable animatable)
            {
                tcs.SetResult(null);
                return tcs.Task;
            }

            // 3. 注册取消
            CancellationTokenRegistration ctr = default;
            if (cancellationToken != default)
            {
                ctr = cancellationToken.Register(() =>
                {
                    animatable.BeginAnimation(property, null); // 停止动画
                    tcs.TrySetCanceled();
                });
            }

            // 4. 动画完成时处理
            animation.Completed += (s, e) =>
            {
                if (setFinalValue && animation.To.HasValue)
                {
                    // 强制设置最终值，避免动画停止后值被复位
                    target.SetValue(property, animation.To.Value);
                }
                ctr.Dispose();
                tcs.TrySetResult(null);
            };

            // 5. 启动动画
            animatable.BeginAnimation(property, animation);

            return tcs.Task;
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
        private void InitializeTemplateControls()
        {
            // 缓存最大化补偿边框
            maximizeBorder = GetTemplateChild("PART_MaximizeBorder") as Border;

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
            maximizeNormalButton = GetTemplateChild("PART_MaximizeNormalButton") as Button;
            closeButton = GetTemplateChild("PART_CloseButton") as Button;

            UpdateButtonVisibility(maximizeNormalButton, minimizeButton);

            AddClickHandler(minimizeButton, OnWindowMinimizing);
            AddClickHandler(maximizeNormalButton, OnWindowStateRestoring);
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
                var mainModule = System.Diagnostics.Process.GetCurrentProcess().MainModule;
                systemVer.Text = mainModule != null
                    ? System.Diagnostics.FileVersionInfo.GetVersionInfo(mainModule.FileName).FileVersion
                    : string.Empty;
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
        {
            if (WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);
            else
                SystemCommands.MaximizeWindow(this);
        }

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

        // DPI 缩放比例（默认 96 DPI = 1.0）
        private double _dpiX = 1.0;
        private double _dpiY = 1.0;

        /// <summary>
        /// 更新当前窗口的 DPI 缩放比例
        /// </summary>
        private void UpdateDpi()
        {
            try
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                _dpiX = dpi.DpiScaleX;
                _dpiY = dpi.DpiScaleY;
            }
            catch
            {
                _dpiX = _dpiY = 1.0; // 获取失败则回退为默认值
            }
        }

        /// <summary>
        /// 根据系统边框指标和 DPI 计算最大化时的内边距补偿值。
        /// GlassFrameThickness > 0 时，OS 最大化会将窗口向外扩展边框厚度的像素，
        /// 需要等量内边距使内容不超出工作区。
        /// </summary>
        private void UpdateMaximizeBorderThickness()
        {
            int borderPad = GetSystemMetrics(SM_CXPADDEDBORDER);
            int frameX = GetSystemMetrics(SM_CXFRAME) + borderPad;
            int frameY = GetSystemMetrics(SM_CYFRAME) + borderPad;

            // 物理像素转 DIP
            double padX = frameX / _dpiX;
            double padY = frameY / _dpiY;

            MaximizeBorderThickness = new Thickness(padX, padY, padX, padY);
        }

        /// <summary>
        /// 窗口消息处理过程
        /// </summary>
        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_GETMINMAXINFO = 0x0024; // 限制窗口最小/最大大小
            const int WM_EXITSIZEMOVE = 0x0232;  // 窗口结束移动或调整大小

            switch (msg)
            {
                case WM_GETMINMAXINFO:
                    handled = WmGetMinMaxInfo(hwnd, lParam);
                    break;
                case WM_EXITSIZEMOVE:
                    UpdateSnapState(hwnd);
                    break;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 检测窗口是否贴顶且高度等于工作区域高度<br/>
        /// 若符合则强制最大化，修复 Win11 圆角丢失问题
        /// </summary>
        private void UpdateSnapState(IntPtr hwnd)
        {
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero) return;

            MONITORINFO mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(monitor, mi)) return;

            int workHeight = mi.rcWork.bottom - mi.rcWork.top;
            // ActualHeight 是 DIP，需要乘以 DPI 缩放转换为物理像素再比较
            double winHeightPx = ActualHeight * _dpiY;

            // 容差 2 像素，避免浮点精度误差
            if (Math.Abs(workHeight - winHeightPx) <= 2.0)
                WindowState = WindowState.Maximized;
        }

        /// <summary>
        /// 处理 WM_GETMINMAXINFO 消息，限制窗口大小并支持 DPI 缩放。
        /// 通过允许窗口边框溢出工作区并在 XAML 层用 Padding 补偿，彻底消除最大化间隙。
        /// </summary>
        private bool WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero) return false;

            MONITORINFO mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(monitor, mi)) return false;

            MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

            RECT rcWork = mi.rcWork;
            RECT rcMonitor = mi.rcMonitor;

            // 计算系统边框厚度（GlassFrameThickness 导致最大化时 OS 向外扩展窗口）
            int borderPad = GetSystemMetrics(SM_CXPADDEDBORDER);
            int frameX = GetSystemMetrics(SM_CXFRAME) + borderPad;
            int frameY = GetSystemMetrics(SM_CYFRAME) + borderPad;

            // 设置最大化时的位置（允许边框溢出到工作区外）
            mmi.ptMaxPosition.x = rcWork.left - rcMonitor.left - frameX;
            mmi.ptMaxPosition.y = rcWork.top - rcMonitor.top - frameY;

            // 设置最大化时的大小（工作区 + 两侧边框溢出）
            mmi.ptMaxSize.x = (rcWork.right - rcWork.left) + 2 * frameX;
            mmi.ptMaxSize.y = (rcWork.bottom - rcWork.top) + 2 * frameY;

            // 设置窗口最小可缩放大小（考虑 DPI 缩放）
            mmi.ptMinTrackSize.x = (int)(MinWidth * _dpiX);
            mmi.ptMinTrackSize.y = (int)(MinHeight * _dpiY);

            Marshal.StructureToPtr(mmi, lParam, true);
            return true;
        }

        /// <summary>
        /// 将 Win32 窗口类背景画刷、DWM 合成背景色、DWM 标题栏颜色统一设置为当前主题色，
        /// 从根源消除拖拽调整大小和最大化/还原过渡动画时的白色闪烁，
        /// 同时保持 GlassFrameThickness=-1 不变以保留原生 DWM 阴影与动画。
        /// </summary>
        private void UpdateNativeBackground()
        {
            if (TryFindResource("WindowBackgroundBrush") is not SolidColorBrush brush) return;
            Color color = brush.Color;

            // 1. 设置 WPF 合成背景色（DWM 合成层），消除最大化/还原过渡闪白
            if (_hwndSource?.CompositionTarget != null)
                _hwndSource.CompositionTarget.BackgroundColor = color;

            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            // COLORREF = 0x00BBGGRR
            uint colorRef = (uint)(color.B << 16 | color.G << 8 | color.R);

            // 2. 设置 Win32 窗口类背景画刷 —— 消除拖拽调整大小时 OS 填充的白色区域
            IntPtr newBrush = CreateSolidBrush(colorRef);
            SetClassLongPtr(hwnd, GCLP_HBRBACKGROUND, newBrush);
            if (_hBrush != IntPtr.Zero)
                DeleteObject(_hBrush);
            _hBrush = newBrush;

            // 3. 设置 DWM 标题栏/边框颜色（Windows 11 22H2+，低版本静默忽略）
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref colorRef, sizeof(uint));
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref colorRef, sizeof(uint));
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
