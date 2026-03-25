using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Snet.Windows.Controls.textbox
{
    /// <summary>
    /// 自定义文本输入控件<br/>
    /// 支持图标、占位提示、清除按钮和高度设置<br/>
    /// 通过依赖属性实现 MVVM 友好的双向数据绑定
    /// </summary>
    public partial class TextBoxControl : UserControl
    {
        /// <summary>
        /// 构造函数：初始化文本输入控件组件
        /// </summary>
        public TextBoxControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 获取或设置控件高度
        /// </summary>
        public double Height
        {
            get => (double)GetValue(HeightProperty);
            set => SetValue(HeightProperty, value);
        }
        /// <summary>
        /// 高度依赖属性<br/>
        /// 控制文本框的高度，默认值为 30 像素
        /// </summary>
        public static readonly DependencyProperty HeightProperty =
            DependencyProperty.Register(nameof(Height), typeof(double), typeof(TextBoxControl), new PropertyMetadata(30d));

        /// <summary>
        /// 获取或设置文本框左侧图标
        /// </summary>
        public ImageSource Icon
        {
            get => (ImageSource)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
        /// <summary>
        /// 图标依赖属性<br/>
        /// 文本框左侧显示的图标图像源
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(TextBoxControl), new PropertyMetadata(null));

        /// <summary>
        /// 获取或设置文本框的内容<br/>
        /// 支持双向绑定（BindsTwoWayByDefault）
        /// </summary>
        public object Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        /// <summary>
        /// 文本内容依赖属性<br/>
        /// 双向绑定，类型为 object 以兼容不同数据类型
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(object), typeof(TextBoxControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// 获取或设置占位提示文本<br/>
        /// 当文本框为空时显示的灰色提示文字
        /// </summary>
        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }
        /// <summary>
        /// 占位提示依赖属性<br/>
        /// 文本框为空时显示的水印提示文字
        /// </summary>
        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register(nameof(Hint), typeof(string), typeof(TextBoxControl), new PropertyMetadata(string.Empty));

        /// <summary>
        /// 获取或设置是否启用清除按钮<br/>
        /// 为 true 时文本框右侧显示清除按钮
        /// </summary>
        public bool ClearButtonEnabled
        {
            get => (bool)GetValue(ClearButtonEnabledProperty);
            set => SetValue(ClearButtonEnabledProperty, value);
        }
        /// <summary>
        /// 清除按钮启用依赖属性<br/>
        /// 默认启用，点击后可快速清空文本内容
        /// </summary>
        public static readonly DependencyProperty ClearButtonEnabledProperty =
            DependencyProperty.Register(nameof(ClearButtonEnabled), typeof(bool), typeof(TextBoxControl), new PropertyMetadata(true));
    }
}
