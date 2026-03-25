using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Snet.Windows.Controls.button
{
    /// <summary>
    /// 自定义按钮控件<br/>
    /// 支持圆角、图标和命令绑定的增强型按钮<br/>
    /// 通过依赖属性实现 MVVM 友好的数据绑定
    /// </summary>
    public partial class ButtonControl : UserControl
    {
        /// <summary>
        /// 构造函数：初始化按钮控件组件
        /// </summary>
        public ButtonControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 圆角半径依赖属性<br/>
        /// 控制按钮四个角的圆角大小，默认值为 8
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(ButtonControl), new PropertyMetadata(new CornerRadius(8)));

        /// <summary>
        /// 获取或设置按钮的圆角半径
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// 命令依赖属性<br/>
        /// 绑定到 ViewModel 中的 ICommand，点击按钮时自动执行
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ButtonControl), new PropertyMetadata(null));

        /// <summary>
        /// 获取或设置按钮点击时执行的命令
        /// </summary>
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// 内容文本依赖属性<br/>
        /// 按钮上显示的文字内容
        /// </summary>
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content), typeof(string), typeof(ButtonControl), new PropertyMetadata(string.Empty));

        /// <summary>
        /// 获取或设置按钮显示的文本内容
        /// </summary>
        public string Content
        {
            get => (string)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// 图标依赖属性<br/>
        /// 按钮上显示的图标图像源
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(ButtonControl), new PropertyMetadata(null));

        /// <summary>
        /// 获取或设置按钮的图标图像源
        /// </summary>
        public ImageSource Icon
        {
            get => (ImageSource)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
    }
}
