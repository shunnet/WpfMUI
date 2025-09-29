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

        private Button? closeButton;              // �رհ�ť
        private Button? maximizeNormalButton;     // ��󻯻�ԭ��ť
        private Button? minimizeButton;           // ��С����ť
        private TextBlock? systemVer;             // ϵͳ�汾��ǩ
        private FrameworkElement? ver;            // �汾Ԫ��

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

        #region �����Ļ���Űױ�����
        // ���� WindowChrome ���󣬱���ÿ���ظ���ȡ
        private WindowChrome _chrome;

        // ����Ƿ����ڽ��б߿��޸�����ֹ�ظ������߼�
        private bool _isResizing;


        /// <summary>
        /// �Ƴ����ڱ߿����ò�����ܺ��Ϊ 0������̬����ָ��ӳ�
        /// </summary>
        /// <param name="e">���ڳߴ�仯�¼�����</param>
        /// <returns>�ָ��߿�ǰ��Ҫ�ӳٵ�ʱ�䣨���룩</returns>
        private int RemoveBorderAndCalcDelay(SizeChangedEventArgs e)
        {
            // ȷ�� _chrome �ѳ�ʼ��
            _chrome ??= WindowChrome.GetWindowChrome(this);

            // ��ʱ�Ƴ��߿򣬷�ֹ���ְױ�
            _chrome.GlassFrameThickness = new Thickness(0);

            // ���ݳߴ�仯���ȶ�̬�����ӳ�ʱ�䣨�仯Խ���ӳ�Խ����
            double sizeDelta = Math.Max(
                Math.Abs(e.NewSize.Width - e.PreviousSize.Width),
                Math.Abs(e.NewSize.Height - e.PreviousSize.Height)
            );

            // �ӳ�ʱ�䷶Χ������ [100ms, 150ms]����ֹ���̻����
            return (int)Math.Clamp(sizeDelta * 0.5, 100, 200);
        }

        /// <summary>
        /// �ָ����ڱ߿����ò�����ܺ��Ϊ 1��
        /// </summary>
        private void RestoreBorder()
        {
            _chrome ??= WindowChrome.GetWindowChrome(this);
            _chrome.GlassFrameThickness = new Thickness(1);
        }

        /// <summary>
        /// ���ڴ�С�ı��¼������ڴ��ڡ��Ŵ�ʱ�����ױ��޸��߼�
        /// </summary>
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // ������С��ߴ�δ�仯�������账��
            if (e.NewSize.Width <= e.PreviousSize.Width &&
                e.NewSize.Height <= e.PreviousSize.Height)
                return;

            // ����ǰ�Ѿ����޸������У���ֱ�ӷ��أ������ظ�����
            if (_isResizing)
                return;

            _isResizing = true;

            // ���Ƴ��߿򲢼��㶯̬�ӳ�ʱ��
            int delay = RemoveBorderAndCalcDelay(e);

            // ʹ�ú�̨�����ӳٻָ��߿�
            _ = Task.Run(async () =>
            {
                try
                {
                    // �ȴ�һ��ʱ�䣬ȷ�����ڵ��������
                    await Task.Delay(delay);

                    // �ص� UI �߳�ִ�б߿�ָ�����
                    await Dispatcher.InvokeAsync(() =>
                    {
                        // �������ѹرջ�δ���أ���ֱ���˳�
                        if (!IsLoaded)
                        {
                            _isResizing = false;
                            return;
                        }

                        // �ָ��߿��޸��ױ�����
                        RestoreBorder();

                        _isResizing = false;
                    });
                }
                catch
                {
                    // �����쳣ʱ��֤״̬�ָ�����������
                    _isResizing = false;
                }
            });
        }
        #endregion

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
            _ = LoadAnimationAsync(LoadAnimationEnabled);
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
        /// ���� ResizeMode ���°�ť�Ŀɼ���
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
        /// �������������ж����������첽�ȴ���
        /// </summary>
        /// <param name="status">�Ƿ����ö���</param>
        /// <param name="cancellationToken">��ѡ��ȡ�����</param>
        public async Task LoadAnimationAsync(bool status, CancellationToken cancellationToken = default)
        {
            if (!status) return;

            // 1. ��ȡģ���е�Ԫ��
            if (GetTemplateChild("PART_ClientArea") is not UIElement clientArea ||
                GetTemplateChild("PART_LoadAnimation") is not UIElement animationArea)
                return;

            // 2. ��ʼ��״̬
            animationArea.Opacity = 1;
            animationArea.Visibility = Visibility.Visible;

            clientArea.Opacity = 0;
            clientArea.Visibility = Visibility.Visible; // opacity = 0 ʱ�Կɲ��벼��
            clientArea.IsEnabled = false;

            var duration = TimeSpan.FromMilliseconds(2000);

            await Task.Delay(duration, cancellationToken);

            // 3. ���ж���
            var fadeOutTask = AnimateAsync(
                animationArea,
                UIElement.OpacityProperty,
                new DoubleAnimation(1, 0, duration) { FillBehavior = FillBehavior.Stop },
                setFinalValue: false, // ����������ǿ�Ʊ��� 0������������ Visibility
                cancellationToken: cancellationToken);

            var fadeInTask = AnimateAsync(
                clientArea,
                UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, duration) { FillBehavior = FillBehavior.HoldEnd },
                setFinalValue: true, // ����������ǿ�Ʊ��� 1
                cancellationToken: cancellationToken);

            // 4. �ȴ��������
            await Task.WhenAll(fadeOutTask, fadeInTask);

            // 5. ������ɺ���
            animationArea.Visibility = Visibility.Collapsed;
            animationArea.Opacity = 1; // ����ΪĬ�ϣ�������һ�ζ�������Ч
            clientArea.IsEnabled = true;
        }

        /// <summary>
        /// �첽ִ�ж��������ڶ�����ɺ󷵻�
        /// </summary>
        /// <param name="target">Ŀ����󣬱����� DependencyObject���� Grid��Border��UserControl �ȣ�</param>
        /// <param name="property">Ҫ�������������ԣ��� UIElement.OpacityProperty��</param>
        /// <param name="animation">����ʵ����DoubleAnimation��ColorAnimation �ȣ�</param>
        /// <param name="setFinalValue">�Ƿ��ڶ�����ɺ�ǿ����������ֵ������ FillBehavior.Stop ��λ</param>
        /// <param name="cancellationToken">��ѡ��ȡ�����</param>
        private Task AnimateAsync(DependencyObject target, DependencyProperty property, DoubleAnimation animation, bool setFinalValue = false, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<object?>();

            // 1. �пմ���
            if (target == null || property == null || animation == null)
            {
                tcs.SetResult(null);
                return tcs.Task;
            }

            // 2. ȷ�Ͽ���ִ�ж���
            if (target is not IAnimatable animatable)
            {
                tcs.SetResult(null);
                return tcs.Task;
            }

            // 3. ע��ȡ��
            if (cancellationToken != default)
            {
                cancellationToken.Register(() =>
                {
                    animatable.BeginAnimation(property, null); // ֹͣ����
                    tcs.TrySetCanceled();
                });
            }

            // 4. �������ʱ����
            animation.Completed += (s, e) =>
            {
                if (setFinalValue && animation.To.HasValue)
                {
                    // ǿ����������ֵ�����⶯��ֹͣ��ֵ����λ
                    target.SetValue(property, animation.To.Value);
                }
                tcs.TrySetResult(null);
            };

            // 5. ��������
            animatable.BeginAnimation(property, animation);

            return tcs.Task;
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
        private void InitializeTemplateControls()
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
            maximizeNormalButton = GetTemplateChild("PART_MaximizeNormalButton") as Button;
            closeButton = GetTemplateChild("PART_CloseButton") as Button;

            UpdateButtonVisibility(maximizeNormalButton, minimizeButton);

            AddClickHandler(minimizeButton, OnWindowMinimizing);
            AddClickHandler(maximizeNormalButton, OnWindowStateRestoring);
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

        // DPI ���ű�����Ĭ�� 96 DPI = 1.0��
        private double _dpiX = 1.0;
        private double _dpiY = 1.0;

        // ��ȡ����������ʾ����ѡ���
        private const int MONITOR_DEFAULTTONEAREST = 2;

        /// <summary>
        /// ���µ�ǰ���ڵ� DPI ���ű���
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
                _dpiX = _dpiY = 1.0; // ��ȡʧ�������ΪĬ��ֵ
            }
        }

        /// <summary>
        /// ������Ϣ�������
        /// </summary>
        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_GETMINMAXINFO = 0x0024; // ���ƴ�����С/����С
            const int WM_EXITSIZEMOVE = 0x0232;  // ���ڽ����ƶ��������С

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
        /// ��ⴰ���Ƿ������Ҹ߶ȵ��ڹ�������߶�<br/>
        /// ��������ǿ����󻯣��޸� Win11 Բ�Ƕ�ʧ����
        /// </summary>
        private void UpdateSnapState(IntPtr hwnd)
        {
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero) return;

            MONITORINFO mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(monitor, mi)) return;

            int workHeight = mi.rcWork.bottom - mi.rcWork.top;
            double winHeight = ActualHeight;

            // �ݲ� 2 ���أ����⸡�㾫�����
            if (Math.Abs(workHeight - winHeight) <= 2.0)
                WindowState = WindowState.Maximized;
        }

        /// <summary>
        /// ���� WM_GETMINMAXINFO ��Ϣ�����ƴ��ڴ�С��֧�� DPI ����
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

            // �������ʱ��λ�ã��������ʾ�����Ͻǣ�
            mmi.ptMaxPosition.x = Math.Abs(rcWork.left - rcMonitor.left);
            mmi.ptMaxPosition.y = Math.Abs(rcWork.top - rcMonitor.top) + 1;

            // �������ʱ�Ĵ�С����������С���ų���������
            mmi.ptMaxSize.x = Math.Abs(rcWork.right - rcWork.left);
            mmi.ptMaxSize.y = Math.Abs(rcWork.bottom - rcWork.top) - 1;

            // ���ô�����С�����Ŵ�С������ DPI ���ţ�
            mmi.ptMinTrackSize.x = (int)(MinWidth * _dpiX);
            mmi.ptMinTrackSize.y = (int)(MinHeight * _dpiY);

            Marshal.StructureToPtr(mmi, lParam, true);
            return true;
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