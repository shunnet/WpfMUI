using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Snet.Windows.Controls.ledgauge
{
    /// <summary>
    /// LED 指示灯控件，支持颜色、开关状态、闪烁、亮度调节和平面模式。<br/>
    /// 通过径向渐变画笔模拟 LED 灯光效果，支持定时器驱动的闪烁动画。
    /// </summary>
    public partial class LedGaugeControl : UserControl
    {
        /// <summary>闪烁状态标志（true 为亮，false 为灰）</summary>
        private bool isFlashingStateOn;

        /// <summary>闪烁定时器，用于控制 LED 灯闪烁频率</summary>
        private System.Timers.Timer? flashTimer;

        /// <summary>灯光径向渐变画笔（缓存引用以避免重复查找）</summary>
        private RadialGradientBrush lampBrush;

        /// <summary>
        /// 渐变配置数组，定义 LED 灯光的亮度分布和渐变位置。<br/>
        /// Item1 = 亮度系数，Item2 = 渐变停靠点位置（0.0 ~ 1.0）
        /// </summary>
        private readonly Tuple<double, double>[] gradientProfile =
        [
            new(1.0, 0.0),
            new(0.898, 0.5),
            new(0.799, 0.66),
            new(0.7, 0.9),
            new(0.51, 0.95),
            new(0.382, 1.0)
        ];

        /// <summary>标记闪烁间隔是否待更新，避免在定时器回调外直接修改间隔</summary>
        private bool isFlashingIntervalChangePending;

        public static readonly DependencyProperty IsFlatProperty = DependencyProperty.Register("IsFlat", typeof(bool), typeof(LedGaugeControl), new FrameworkPropertyMetadata(false, OnIsFlatPropertyChnaged));

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register("Color", typeof(Color), typeof(LedGaugeControl), new FrameworkPropertyMetadata(Colors.Green, FrameworkPropertyMetadataOptions.None, OnColorPropertyChanged));

        public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register("IsOn", typeof(bool), typeof(LedGaugeControl), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None, OnIsOnPropertyChanged));

        public static readonly DependencyProperty IsFlashingProperty = DependencyProperty.Register("IsFlashing", typeof(bool), typeof(LedGaugeControl), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None, OnIsFlashingPropertyChanged));

        public static readonly DependencyProperty FlashingIntervalProperty = DependencyProperty.Register("FlashingInterval", typeof(double), typeof(LedGaugeControl), new FrameworkPropertyMetadata(500.0, FrameworkPropertyMetadataOptions.None, OnFlashingIntervalPropertyChanged));

        public static readonly DependencyProperty OnLightnessProperty = DependencyProperty.Register("OnLightness", typeof(double), typeof(LedGaugeControl), new FrameworkPropertyMetadata(0.5, FrameworkPropertyMetadataOptions.None, OnOnLightnessPropertyChanged));

        public static readonly DependencyProperty OffLightnessProperty = DependencyProperty.Register("OffLightness", typeof(double), typeof(LedGaugeControl), new FrameworkPropertyMetadata(0.3, FrameworkPropertyMetadataOptions.None, OffOffLightnessPropertyChanged));


        public bool IsFlat
        {
            get
            {
                return (bool)GetValue(IsFlatProperty);
            }
            set
            {
                SetValue(IsFlatProperty, value);
            }
        }

        [Description("LED灯的颜色。从颜色中只读取色相，LED的饱和度和亮度是自动的。")]
        [Category("Common")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color Color
        {
            get
            {
                return (Color)GetValue(ColorProperty);
            }
            set
            {
                SetValue(ColorProperty, value);
            }
        }

        [Description("如果LED灯亮则为True，如果灯灭则为false")]
        [Category("Common")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool IsOn
        {
            get
            {
                return (bool)GetValue(IsOnProperty);
            }
            set
            {
                SetValue(IsOnProperty, value);
            }
        }

        [Description("让LED灯闪烁")]
        [Category("Common")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool IsFlashing
        {
            get
            {
                return (bool)GetValue(IsFlashingProperty);
            }
            set
            {
                SetValue(IsFlashingProperty, value);
            }
        }

        [Description("让LED灯闪烁间隔以毫秒为单位")]
        [Category("Common")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public double FlashingInterval
        {
            get
            {
                return (double)GetValue(FlashingIntervalProperty);
            }
            set
            {
                SetValue(FlashingIntervalProperty, value);
            }
        }

        [Description("灯亮时LED亮度")]
        [Category("Common")]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public double OnLightness
        {
            get
            {
                return (double)GetValue(OnLightnessProperty);
            }
            set
            {
                SetValue(OnLightnessProperty, value);
            }
        }

        [Description("灯灭时LED亮度")]
        [Category("Common")]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public double OffLightness
        {
            get
            {
                return (double)GetValue(OffLightnessProperty);
            }
            set
            {
                SetValue(OffLightnessProperty, value);
            }
        }

        /// <summary>
        /// 初始化 LED 指示灯控件。<br/>
        /// 缓存灯光径向渐变画笔引用，注册加载、卸载、启用状态和可见性变更事件。
        /// </summary>
        public LedGaugeControl()
        {
            InitializeComponent();
            lampBrush = (RadialGradientBrush)Lamp.Fill;
            base.Loaded += LedGauge_Loaded;
            base.Unloaded += LedGauge_Unloaded;
            base.IsEnabledChanged += OnIsEnabledChanged;
            base.IsVisibleChanged += OnIsVisibleChanged;
        }

        /// <summary>
        /// IsFlat 属性变更回调。<br/>
        /// 平面模式下去除边框的外圈暗色渐变，仅保留内圈高光。
        /// </summary>
        /// <param name="dependencyObject">发生属性变更的对象</param>
        /// <param name="e">属性变更事件参数</param>
        private static void OnIsFlatPropertyChnaged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not LedGaugeControl ledGauge) return;

            var radialGradientBrush = new RadialGradientBrush
            {
                Center = new Point(0.1, 0.0),
                RadiusX = 1.0,
                RadiusY = 1.0,
                GradientOrigin = new Point(0.5, 0.0)
            };
            radialGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 244, 244, 244), 0.2));
            if (!ledGauge.IsFlat)
            {
                radialGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 68, 68, 68), 1.0));
            }
            ledGauge.Border.Fill = radialGradientBrush;
        }

        /// <summary>
        /// Color 属性变更回调，重新应用新颜色到 LED 灯光
        /// </summary>
        private static void OnColorPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is LedGaugeControl ledGauge)
            {
                ledGauge.ApplyColor((Color)e.NewValue, ledGauge.IsOn, ledGauge.IsEnabled);
            }
        }

        /// <summary>
        /// IsOn 属性变更回调，根据开关状态重新计算亮度
        /// </summary>
        private static void OnIsOnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is LedGaugeControl ledGauge)
            {
                ledGauge.ApplyColor(ledGauge.Color, (bool)e.NewValue, ledGauge.IsEnabled);
            }
        }

        /// <summary>
        /// IsFlashing 属性变更回调。<br/>
        /// 启动闪烁时开始定时器，停止闪烁时停止定时器并恢复灯光状态。
        /// </summary>
        private static void OnIsFlashingPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not LedGaugeControl ledGauge) return;

            if ((bool)e.NewValue)
            {
                if (ledGauge.IsEnabled)
                {
                    ledGauge.isFlashingStateOn = true;
                    ledGauge.ApplyColor(ledGauge.Color, true, true);
                    ledGauge.flashTimer?.Start();
                }
            }
            else
            {
                ledGauge.flashTimer?.Stop();
                ledGauge.ApplyColor(ledGauge.Color, ledGauge.IsOn, ledGauge.IsEnabled);
            }
        }

        /// <summary>
        /// FlashingInterval 属性变更回调，标记间隔待更新，实际更新在下次定时器回调中执行
        /// </summary>
        private static void OnFlashingIntervalPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is LedGaugeControl ledGauge)
            {
                ledGauge.isFlashingIntervalChangePending = true;
            }
        }

        /// <summary>
        /// OnLightness 属性变更回调，重新计算开灯时的亮度
        /// </summary>
        private static void OnOnLightnessPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is LedGaugeControl ledGauge)
            {
                ledGauge.ApplyColor(ledGauge.Color, ledGauge.IsOn, ledGauge.IsEnabled);
            }
        }

        /// <summary>
        /// OffLightness 属性变更回调，重新计算关灯时的亮度
        /// </summary>
        private static void OffOffLightnessPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is LedGaugeControl ledGauge)
            {
                ledGauge.ApplyColor(ledGauge.Color, ledGauge.IsOn, ledGauge.IsEnabled);
            }
        }

        /// <summary>
        /// 控件加载完成事件<br/>
        /// 初始化闪烁定时器，如果控件可见且启用闪烁则自动开始
        /// </summary>
        private void LedGauge_Loaded(object sender, RoutedEventArgs e)
        {
            flashTimer = new System.Timers.Timer(FlashingInterval);
            flashTimer.Elapsed += OnTimerTick;
            if (base.IsVisible && base.IsEnabled && IsFlashing)
            {
                flashTimer.Start();
            }
        }

        /// <summary>
        /// 控件卸载事件<br/>
        /// 安全释放闪烁定时器资源，取消事件订阅并释放定时器
        /// </summary>
        private void LedGauge_Unloaded(object sender, RoutedEventArgs e)
        {
            if (flashTimer == null)
                return;

            flashTimer.Stop();
            flashTimer.Elapsed -= OnTimerTick;
            flashTimer.Dispose();
            flashTimer = null;
        }

        /// <summary>
        /// 应用 LED 灯光颜色<br/>
        /// 根据基色、开关状态和启用状态计算渐变停点，更新 LED 灯光的径向渐变画笔
        /// </summary>
        /// <param name="baseColor">基础颜色，用于计算灯光色相</param>
        /// <param name="isOn">灯是否开启，影响亮度计算</param>
        /// <param name="isEnabled">控件是否启用，禁用时降低饱和度并提高亮度</param>
        private void ApplyColor(Color baseColor, bool isOn, bool isEnabled)
        {
            ColorModel colorModelConverter = new ColorModel(System.Drawing.Color.FromArgb(baseColor.A, baseColor.R, baseColor.G, baseColor.B));
            List<GradientStop> list = new List<GradientStop>(gradientProfile.Length);
            Tuple<double, double>[] array = gradientProfile;
            foreach (Tuple<double, double> tuple in array)
            {
                if (isEnabled)
                {
                    colorModelConverter.Lightness = tuple.Item1 * (isOn ? OnLightness : OffLightness);
                }
                else
                {
                    colorModelConverter.Saturation = 0.0;
                    colorModelConverter.Lightness = tuple.Item1 * (isOn ? OffLightness : OnLightness);
                    colorModelConverter.Lightness += 0.3;
                }
                list.Add(new GradientStop(System.Windows.Media.Color.FromArgb(colorModelConverter.Color.A, colorModelConverter.Color.R, colorModelConverter.Color.G, colorModelConverter.Color.B), tuple.Item2));
                if (IsFlat)
                {
                    break;
                }
            }
            lampBrush.GradientStops = new GradientStopCollection(list);
        }
        /// <summary>
        /// 控件启用状态变更事件<br/>
        /// 启用时恢复灯光和透明度，禁用时停止闪烁并降低透明度
        /// </summary>
        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                ApplyColor(Color, IsOn || IsFlashing, base.IsEnabled);
                base.Opacity *= 2.0;
                if (IsFlashing)
                {
                    flashTimer?.Start();
                }
            }
            else
            {
                flashTimer?.Stop();
                ApplyColor(Color, IsOn, base.IsEnabled);
                base.Opacity *= 0.5;
            }
        }

        /// <summary>
        /// 控件可见性变更事件<br/>
        /// 不可见时停止闪烁定时器，可见时恢复闪烁
        /// </summary>
        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                flashTimer?.Stop();
            }
            else if (IsFlashing)
            {
                flashTimer?.Start();
            }
        }

        /// <summary>
        /// 闪烁定时器回调事件<br/>
        /// 处理闪烁间隔变更和灯光状态切换
        /// </summary>
        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            if (isFlashingIntervalChangePending && flashTimer != null)
            {
                flashTimer.Stop();
                flashTimer.Interval = FlashingInterval;
                flashTimer.Start();
                isFlashingIntervalChangePending = false;
            }
            isFlashingStateOn = !isFlashingStateOn;
            base.Dispatcher.Invoke(delegate
            {
                ApplyColor(Color, isFlashingStateOn, base.IsEnabled);
            }, DispatcherPriority.Render);
        }
    }
}
