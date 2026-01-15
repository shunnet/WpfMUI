using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Snet.Windows.Controls.textbox
{
    /// <summary>
    /// TextBoxControl.xaml 的交互逻辑
    /// </summary>
    public partial class TextBoxControl : UserControl
    {
        public TextBoxControl()
        {
            InitializeComponent();
        }

        public double Height
        {
            get => (double)GetValue(HeightProperty);
            set => SetValue(HeightProperty, value);
        }
        public static readonly DependencyProperty HeightProperty =
            DependencyProperty.Register(nameof(Height), typeof(double), typeof(TextBoxControl), new PropertyMetadata(30d));

        public ImageSource Icon
        {
            get => (ImageSource)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(TextBoxControl), new PropertyMetadata(null));

        public object Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(object), typeof(TextBoxControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }
        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register(nameof(Hint), typeof(string), typeof(TextBoxControl), new PropertyMetadata(string.Empty));


        public bool ClearButtonEnabled
        {
            get => (bool)GetValue(ClearButtonEnabledProperty);
            set => SetValue(ClearButtonEnabledProperty, value);
        }
        public static readonly DependencyProperty ClearButtonEnabledProperty =
            DependencyProperty.Register(nameof(ClearButtonEnabled), typeof(bool), typeof(TextBoxControl), new PropertyMetadata(true));
    }
}
