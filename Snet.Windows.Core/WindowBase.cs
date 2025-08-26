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
    /// �Զ��崰�ڻ��࣬�ṩƤ���л��������л������ڿ��ƵȻ�������
    /// </summary>
    public class WindowBase : System.Windows.Window
    {
        #region ��������

        /// <summary>
        /// �����л�����
        /// </summary>
        public static readonly DependencyProperty LanguageEnabledProperty =
            DependencyProperty.Register("LanguageEnabled", typeof(bool), typeof(WindowBase), new UIPropertyMetadata(true));

        /// <summary>
        /// Ƥ���л�����
        /// </summary>
        public static readonly DependencyProperty SkinEnabledProperty =
            DependencyProperty.Register("SkinEnabled", typeof(bool), typeof(WindowBase), new UIPropertyMetadata(true));

        /// <summary>
        /// ���⿿��
        /// </summary>
        public static readonly DependencyProperty TitleLeftProperty =
            DependencyProperty.Register("TitleLeft", typeof(bool), typeof(WindowBase), new UIPropertyMetadata(true));

        /// <summary>
        /// �ײ��汾��ʾ
        /// </summary>
        public static readonly DependencyProperty VerEnabledProperty =
            DependencyProperty.Register("VerEnabled", typeof(bool), typeof(WindowBase), new UIPropertyMetadata(true));

        /// <summary>
        /// ���ض���<br/>
        /// ��������������ʾ
        /// </summary>
        public static readonly DependencyProperty LoadAnimationEnabledProperty =
            DependencyProperty.Register("LoadAnimationEnabled", typeof(bool), typeof(WindowBase), new UIPropertyMetadata(false));

        #endregion

        #region �ؼ��ֶ�

        private Button? closeButton;         // �رհ�ť
        private Button? maximizeButton;     // ��󻯰�ť
        private Button? minimizeButton;      // ��С����ť
        private Button? normalButton;        // ��ԭ��ť
        private TextBlock? systemVer;            // ϵͳ�汾��ǩ
        private FrameworkElement? ver;       // �汾Ԫ��

        #endregion

        #region ���캯���;�̬���캯��

        /// <summary>
        /// ��̬���캯������д��ʽ����Ԫ����
        /// </summary>
        static WindowBase()
        {
            // ���ó�ʼƤ��
            SkinHandler.SetSkin(SkinHandler.GetSkin(), false);
            // ���ó�ʼ����
            LanguageHandler.SetLanguage(LanguageHandler.GetLanguage());
            StyleProperty.OverrideMetadata(typeof(WindowBase), new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceStyle)));
        }

        /// <summary>
        /// ���캯������ʼ�����ڻ�������
        /// </summary>
        public WindowBase()
        {
            //�󶨵������
            LanguageCommand = new AsyncRelayCommand(OnLanguageCommand);
            SkinCommand = new AsyncRelayCommand(OnSkinCommand);
            this.Loaded += OnLoaded;
            this.SizeChanged += OnSizeChanged;
            this.SourceInitialized += OnSourceInitialized;
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // ���ó�ʼƤ��
                SkinHandler.SetSkin(SkinHandler.GetSkin(), true);

            }, DispatcherPriority.Loaded);
        }

        #endregion

        #region �����
        /// <summary>
        /// Ƥ���л�����
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
        /// �����л�����
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

        #region �¼�
        private bool _isResizing;
        /// <summary>
        /// �����С�ı䣬���ڱ��ʱ����ױ��޸�
        /// </summary>
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isResizing)
                return;
            if (e.NewSize.Width <= e.PreviousSize.Width && e.NewSize.Height <= e.PreviousSize.Height)
                return;

            _isResizing = true;

            // ��ȡ���߿�Ͳ���������ֹ�ױ�
            WindowChrome.GetWindowChrome(this).GlassFrameThickness = new Thickness(0);

            _ = Task.Run(async () =>
            {
                try
                {
                    // �ȴ�һ��ʱ�䣬ȷ�����ڴ�С�������
                    await Task.Delay(135);

                    // �� UI �ָ̻߳��߿�
                    await Dispatcher.InvokeAsync(() =>
                    {
                        // ���ⴰ���ѹرպ��ٷ���
                        if (!IsLoaded) return;

                        // �ָ��߿�Ͳ�����
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
        /// ����������
        /// </summary>
        /// <param name="sender">Դ</param>
        /// <param name="e">�¼�</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //����ͳһԲ��
            var resources = Application.Current.Resources;
            double value = (double)resources["WindowCornerRadius_Double"];
            resources["TopCornerRadius"] = new CornerRadius(value, value, 0, 0);
            resources["DownCornerRadius"] = new CornerRadius(0, 0, value, value);
            resources["CloseCornerRadius"] = new CornerRadius(0, value, 0, 0);
            resources["WindowCornerRadius"] = new CornerRadius(value);

            // �޸�ע����Ż���ʾЧ��
            UpdateRegistry();

            //�Զ����Ź���
            AutoAdjustAsync();

            //�鿴��ʲô����
#if DEBUG
            int tier = (RenderCapability.Tier >> 16);
            string message;
            switch (tier)
            {
                case 0:
                    message = "Tier 0����Ӳ�����٣���ȫʹ�������Ⱦ��";
                    break;
                case 1:
                    message = "Tier 1������Ӳ�����٣��������ޡ�";
                    break;
                case 2:
                    message = "Tier 2����ȫӲ�����٣�ʹ�� GPU ��Ⱦ��";
                    break;
                default:
                    message = "δ֪��Ⱦ�ȼ���";
                    break;
            }
            LogHelper.Verbose(message);
#endif
        }

        /// <summary>
        /// �������¼���Ϊ��֧����Win32�Ļ�����
        /// </summary>
        /// <param name="sender">Դ</param>
        /// <param name="e">�¼�</param>
        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            UpdateDpi(); // ��ʼ��ʱ����һ��
        }
        #endregion

        #region ���Է�����

        /// <summary>
        /// ��ȡ�������Ƿ����������л�����
        /// </summary>
        public bool LanguageEnabled
        {
            get => (bool)GetValue(LanguageEnabledProperty);
            set => SetValue(LanguageEnabledProperty, value);
        }

        /// <summary>
        /// ��ȡ�������Ƿ�����Ƥ���л�����
        /// </summary>
        public bool SkinEnabled
        {
            get => (bool)GetValue(SkinEnabledProperty);
            set => SetValue(SkinEnabledProperty, value);
        }

        /// <summary>
        /// ��ȡ�����ñ����Ƿ�����ʾ
        /// </summary>
        public bool TitleLeft
        {
            get => (bool)GetValue(TitleLeftProperty);
            set => SetValue(TitleLeftProperty, value);
        }

        /// <summary>
        /// ��ȡ�������Ƿ���ʾ�汾��
        /// </summary>
        public bool VerEnabled
        {
            get => (bool)GetValue(VerEnabledProperty);
            set => SetValue(VerEnabledProperty, value);
        }

        /// <summary>
        /// ���ض���
        /// </summary>
        public bool LoadAnimationEnabled
        {
            get => (bool)GetValue(LoadAnimationEnabledProperty);
            set => SetValue(LoadAnimationEnabledProperty, value);
        }

        #endregion

        #region ��������

        /// <summary>
        /// ���ڶ���Ч��
        /// </summary>
        /// <param name="window">Ҫ�����Ĵ��ڣ����Ϊnull��ʹ�õ�ǰ�����</param>
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

        #region ��д����

        /// <summary>
        /// Ӧ��ģ��ʱ���ã���ʼ�����ڿؼ�
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            LoadAnimation(LoadAnimationEnabled);
            InitializeTemplateControls();
        }

        /// <summary>
        /// ����Դ��ʼ��ʱ���ã����ô�����Ϣ����
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
            base.OnSourceInitialized(e);
        }

        #endregion

        #region ˽�з���

        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="status">״̬</param>
        public async Task LoadAnimation(bool status)
        {
            if (status)
            {
                if (GetTemplateChild("PART_ClientArea") is UIElement clientArea && GetTemplateChild("PART_LoadAnimation") is UIElement animationArea)
                {
                    // ȷ��һ��ʼ���������ǿɼ��ģ��ͻ������������ص�
                    animationArea.Opacity = 1;
                    animationArea.Visibility = Visibility.Visible;

                    clientArea.Opacity = 0;
                    clientArea.Visibility = Visibility.Visible; // opacity Ϊ 0 ʱ������ʾʵ������
                    clientArea.IsEnabled = false;
                    // ���� clientArea �ĵ��붯��
                    var clientFadeIn = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(2000),
                        FillBehavior = FillBehavior.HoldEnd
                    };

                    // ���� animationArea �ĵ�������
                    var animationFadeOut = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(2000),
                        FillBehavior = FillBehavior.Stop
                    };

                    animationFadeOut.Completed += (s, e) =>
                    {
                        // ������ɺ��������� animationArea
                        animationArea.Visibility = Visibility.Collapsed;
                        animationArea.Opacity = 1; // ����ΪĬ�ϣ�������һ�ζ�������Ч��
                        clientArea.IsEnabled = true;
                    };
                    await Task.Delay(3000);
                    // ��������
                    clientArea.BeginAnimation(UIElement.OpacityProperty, clientFadeIn);
                    animationArea.BeginAnimation(UIElement.OpacityProperty, animationFadeOut);
                }
            }
        }

        /// <summary>
        /// �Զ����ݵ�ǰ��Ļ�� DPI ���Ŵ��ڴ�С�������ִ��ھ��������ܵȱ߾��Ӿ�Ч����
        /// </summary>
        private Task AutoAdjustAsync()
        {
            // ��ȡ��ʾ����Ϣ
            GetCursorPos(out POINT_struct pt);
            IntPtr hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);

            MONITORINFO mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            GetMonitorInfo(hMonitor, mi);

            // �����������سߴ�
            int pxWidth = mi.rcWork.right - mi.rcWork.left;
            int pxHeight = mi.rcWork.bottom - mi.rcWork.top;

            // ��ȡDPI����
            GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _);
            double dpiScale = dpiX / 96.0;

            // ���㴰���������سߴ�
            double pxWindowWidth = ActualWidth * dpiScale;
            double pxWindowHeight = ActualHeight * dpiScale;

            // �����������ߴ�С�ڵ�����Ļ��������ֱ�Ӿ�����ʾ
            if (pxWindowWidth <= pxWidth && pxWindowHeight <= pxHeight)
            {
                return Task.CompletedTask;
            }

            // ��Ҫ���ŵ����
            double fitScale = Math.Min(
                Math.Min(pxWidth / pxWindowWidth, pxHeight / pxWindowHeight),
                0.7);

            Dispatcher.Invoke(() =>
            {
                LayoutTransform = new ScaleTransform(fitScale, fitScale);
                Width = ActualWidth * fitScale;
                Height = ActualHeight * fitScale;

                // �������ź������ߴ�
                double scaledPxWidth = Width * dpiScale;
                double scaledPxHeight = Height * dpiScale;

                // ������ʾ
                Left = mi.rcWork.left + (pxWidth - scaledPxWidth) / 2;
                Top = mi.rcWork.top + (pxHeight - scaledPxHeight) / 2;
            });

            return Task.CompletedTask;
        }


        /// <summary>
        /// ��ʼ��ģ���еĿؼ��������¼�
        /// </summary>
        private async Task InitializeTemplateControls()
        {
            // ���ñ�����뷽ʽ
            if (!TitleLeft && GetTemplateChild("PART_CaptionText") is TextBlock captionText)
            {
                captionText.HorizontalAlignment = HorizontalAlignment.Center;
            }

            // ��ʼ�����ڿ��ư�ť
            InitializeWindowButtons();

            // ��ʼ���汾��ʾ
            InitializeVersionDisplay();
        }

        /// <summary>
        /// ��ʼ�����ڿ��ư�ť����С�������/��ԭ���رգ�
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
        /// ��ʼ���汾��ʾ
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
        /// ǿ����ʽ�ص�
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

        #region ���ڿ����¼�����

        /// <summary>
        /// �رմ����¼�����
        /// </summary>
        private void OnWindowClosing(object sender, RoutedEventArgs e)
            => this.Close();

        /// <summary>
        /// ��С�������¼�����
        /// </summary>
        private void OnWindowMinimizing(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        /// <summary>
        /// ���/��ԭ�����¼�����
        /// </summary>
        private void OnWindowStateRestoring(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        #endregion

        #region ���Ժ�Ƥ������

        /// <summary>
        /// �����л��¼�����
        /// </summary>
        private void OnLanguage(object sender, RoutedEventArgs e)
        {
            var language = LanguageHandler.GetLanguage() == LanguageType.zh
                ? LanguageType.en
                : LanguageType.zh;

            LanguageHandler.SetLanguage(language);
        }

        /// <summary>
        /// Ƥ���л��¼�����
        /// </summary>
        private void OnSkin(object sender, RoutedEventArgs e)
        {
            var skin = SkinHandler.GetSkin() == SkinType.Dark
                ? SkinType.Light
                : SkinType.Dark;
            Application.Current.Dispatcher.InvokeAsync(() => SkinHandler.SetSkin(skin));
        }

        #endregion

        #region ע���������Ż���ʾЧ����

        private readonly string registrieIni = $"{WindowHandler.BasePath}\\registrie.ini";

        private readonly List<RegistryModel> registries = new()
        {
            // ����Ϊ"�Զ����Ӿ�Ч��"
            new RegistryModel("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects",
                "VisualFXSetting", 3, RegistryValueKind.DWord),
            // �޸� VisualEffects ����
            new RegistryModel("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\\DragFullWindows",
                "DefaultApplied", 0, RegistryValueKind.DWord),
            // �޸� Control Panel\Desktop
            new RegistryModel("Control Panel\\Desktop",
                "DragFullWindows", "0", RegistryValueKind.String),
        };

        /// <summary>
        /// �޸�ע����Ż������϶���ʾЧ��
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
                    LogHelper.Error($"�޸�ע��� {item.Name} �쳣��{ex.Message}", exception: ex);
                }
            }

            // ֪ͨϵͳ�����Ѹ���
            const uint SPI_SETDRAGFULLWINDOWS = 0x0025;
            const uint SPIF_UPDATEINIFILE = 0x01;
            const uint SPIF_SENDCHANGE = 0x02;
            SystemParametersInfo(SPI_SETDRAGFULLWINDOWS, 0, null, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

            //�ļ��в������򴴽�
            if (!Directory.Exists(WindowHandler.BasePath))
            {
                Directory.CreateDirectory(WindowHandler.BasePath);
            }

            // ��������ļ�
            File.WriteAllText(registrieIni,
                "1. �޸�ע���ʹ�������Ч������\r\n" +
                "2. ���ļ����ڴ���ע������޸ĳɹ�\r\n" +
                "3. ����ɾ��!!!", Encoding.UTF8);
        }

        #endregion

        #region ������Ϣ����˫���Ŵ���С�����������ƶ���

        private double _dpiX = 1.0, _dpiY = 1.0;

        /// <summary>
        /// ���µ�ǰ���ڵ� DPI ���űȣ������� SourceInitialized �� Loaded �е��ã�
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
                UpdateSnapState(hwnd); // �����ȥ�ж��Ƿ�����
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// ��ⴰ���Ƿ��Ѿ������Ҹ߶ȵ��ڹ�������߶ȣ�
        /// ����ǣ����������ô���״̬Ϊ Maximized�����ڽ�����ߵ�δ���ʱϵͳ�Ӿ�Բ�Ǳ��Ƴ������⡣
        /// </summary>
        /// <param name="hwnd">���ھ��</param>
        private void UpdateSnapState(IntPtr hwnd)
        {
            // ��ȡ��ǰ�������ڵ���ʾ��������ڶ���ʾ�������У�
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero)
                return;

            // ��ȡ��ʾ���Ĺ��������ų���������ͣ������
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
        /// ����������ƺ���С�ߴ����ƣ�֧�� DPI ���ţ�
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

            // �������λ�ã��ų�����������
            mmi.ptMaxPosition.x = rcWorkArea.left - rcMonitorArea.left;
            mmi.ptMaxPosition.y = rcWorkArea.top - rcMonitorArea.top;

            mmi.ptMaxSize.x = rcWorkArea.right - rcWorkArea.left;
            mmi.ptMaxSize.y = rcWorkArea.bottom - rcWorkArea.top - 1;

            // ��С��ߣ�֧�� DPI ���ţ�_dpiX/_dpiY �ѻ��棩
            mmi.ptMinTrackSize.x = (int)(this.MinWidth * _dpiX);
            mmi.ptMinTrackSize.y = (int)(this.MinHeight * _dpiY);

            Marshal.StructureToPtr(mmi, lParam, false);
        }

        #endregion

        #region ��������

        /// <summary>
        /// Ϊ��ť��ӵ���¼�����������չ����
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