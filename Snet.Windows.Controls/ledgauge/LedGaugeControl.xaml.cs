using System.ComponentModel;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Snet.Windows.Controls.ledgauge
{
    /// <summary>
    /// LedGaugeControl.xaml 的交互逻辑
    /// </summary>
    public partial class LedGaugeControl : UserControl
    {
        private bool isFlashingStateOn;

        private System.Timers.Timer flashTimer;

        private RadialGradientBrush lampBrush;

        private Tuple<double, double>[] gradientProfile = new Tuple<double, double>[6]
        {
        new Tuple<double, double>(1.0, 0.0),
        new Tuple<double, double>(0.898, 0.5),
        new Tuple<double, double>(0.799, 0.66),
        new Tuple<double, double>(0.7, 0.9),
        new Tuple<double, double>(0.51, 0.95),
        new Tuple<double, double>(0.382, 1.0)
        };

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

        public LedGaugeControl()
        {
            InitializeComponent();
            lampBrush = (RadialGradientBrush)Lamp.Fill;
            base.Loaded += LedGauge_Loaded;
            base.Unloaded += LedGauge_Unloaded;
            base.IsEnabledChanged += OnIsEnabledChanged;
            base.IsVisibleChanged += OnIsVisibleChanged;
        }

        private static void OnIsFlatPropertyChnaged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LedGaugeControl ledGauge;
            if ((ledGauge = dependencyObject as LedGaugeControl) != null)
            {
                RadialGradientBrush radialGradientBrush = new RadialGradientBrush
                {
                    Center = new Point(0.1, 0.0),
                    RadiusX = 1.0,
                    RadiusY = 1.0,
                    GradientOrigin = new Point(0.5, 0.0)
                };
                radialGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(byte.MaxValue, 244, 244, 244), 0.2));
                if (!ledGauge.IsFlat)
                {
                    radialGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(byte.MaxValue, 68, 68, 68), 1.0));
                }
                ledGauge.Border.Fill = radialGradientBrush;
            }
        }

        private static void OnColorPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LedGaugeControl ledGauge;
            if ((ledGauge = dependencyObject as LedGaugeControl) != null)
            {
                ledGauge.ApplyColor((Color)e.NewValue, ledGauge.IsOn, ledGauge.IsEnabled);
            }
        }

        private static void OnIsOnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LedGaugeControl ledGauge;
            if ((ledGauge = dependencyObject as LedGaugeControl) != null)
            {
                ledGauge.ApplyColor(ledGauge.Color, (bool)e.NewValue, ledGauge.IsEnabled);
            }
        }

        private static void OnIsFlashingPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LedGaugeControl ledGauge;
            if ((ledGauge = dependencyObject as LedGaugeControl) == null)
            {
                return;
            }
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

        private static void OnFlashingIntervalPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LedGaugeControl ledGauge;
            if ((ledGauge = dependencyObject as LedGaugeControl) != null)
            {
                ledGauge.isFlashingIntervalChangePending = true;
            }
        }

        private static void OnOnLightnessPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LedGaugeControl ledGauge;
            if ((ledGauge = dependencyObject as LedGaugeControl) != null)
            {
                ledGauge.ApplyColor(ledGauge.Color, ledGauge.IsOn, ledGauge.IsEnabled);
            }
        }

        private static void OffOffLightnessPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LedGaugeControl ledGauge;
            if ((ledGauge = dependencyObject as LedGaugeControl) != null)
            {
                ledGauge.ApplyColor(ledGauge.Color, ledGauge.IsOn, ledGauge.IsEnabled);
            }
        }

        private void LedGauge_Loaded(object sender, RoutedEventArgs e)
        {
            flashTimer = new System.Timers.Timer(FlashingInterval);
            flashTimer.Elapsed += OnTimerTick;
            if (base.IsVisible && base.IsEnabled && IsFlashing)
            {
                flashTimer.Start();
            }
        }

        private void LedGauge_Unloaded(object sender, RoutedEventArgs e)
        {
            if (flashTimer == null)
                return;

            flashTimer.Stop();
            flashTimer.Elapsed -= OnTimerTick;
            flashTimer.Dispose();
            flashTimer = null;
        }

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

        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            if (isFlashingIntervalChangePending && flashTimer != null)
            {
                flashTimer.Stop();
                flashTimer.Interval = FlashingInterval;
                flashTimer.Start();
            }
            isFlashingStateOn = !isFlashingStateOn;
            base.Dispatcher.Invoke(delegate
            {
                ApplyColor(Color, isFlashingStateOn, base.IsEnabled);
            }, DispatcherPriority.Render);
        }
    }
}
