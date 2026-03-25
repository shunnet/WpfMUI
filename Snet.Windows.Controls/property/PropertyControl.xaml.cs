using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Snet.Windows.Controls.property
{
    /// <summary>
    /// 属性控件面板的交互逻辑。<br/>
    /// 提供基础数据绑定、导入/导出命令和按钮可见性控制。
    /// </summary>
    public partial class PropertyControl : UserControl
    {

        /// <summary>
        /// 初始化 PropertyControl 实例。
        /// </summary>
        public PropertyControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 获取基础数据属性的当前值。
        /// </summary>
        /// <returns>基础数据对象。</returns>
        public object GetBasics()
        {
            return GetValue(BasicsDataProperty);
        }

        /// <summary>
        /// 设置基础数据属性的值。
        /// </summary>
        /// <param name="value">要设置的基础数据对象。</param>
        public void SetBasics(object value)
        {
            SetValue(BasicsDataProperty, value);
        }

        /// <summary>
        /// 获取或设置导出命令。<br/>
        /// 用于将属性数据导出到外部文件或格式。
        /// </summary>
        public ICommand ExpCommand
        {
            get => (ICommand)GetValue(ExpCommandProperty);
            set => SetValue(ExpCommandProperty, value);
        }
        public static readonly DependencyProperty ExpCommandProperty =
            DependencyProperty.Register(nameof(ExpCommand), typeof(ICommand), typeof(PropertyControl), new PropertyMetadata(null));

        /// <summary>
        /// 获取或设置导入命令。<br/>
        /// 用于从外部文件或格式导入属性数据。
        /// </summary>
        public ICommand IncCommand
        {
            get => (ICommand)GetValue(IncCommandProperty);
            set => SetValue(IncCommandProperty, value);
        }
        public static readonly DependencyProperty IncCommandProperty =
            DependencyProperty.Register(nameof(IncCommand), typeof(ICommand), typeof(PropertyControl), new PropertyMetadata(null));

        /// <summary>
        /// 获取或设置基础数据对象。<br/>
        /// 支持双向绑定，用于 PropertyGrid 显示和编辑属性。
        /// </summary>
        public object BasicsData
        {
            get => GetValue(BasicsDataProperty);
            set => SetValue(BasicsDataProperty, value);
        }
        public static readonly DependencyProperty BasicsDataProperty =
            DependencyProperty.Register(nameof(BasicsData), typeof(object), typeof(PropertyControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// 获取或设置导入/导出按钮的显示状态。<br/>
        /// 默认为 Collapsed（隐藏）。
        /// </summary>
        public Visibility ButtonVisibility
        {
            get => (Visibility)GetValue(ButtonVisibilityProperty);
            set => SetValue(ButtonVisibilityProperty, value);
        }
        public static readonly DependencyProperty ButtonVisibilityProperty = DependencyProperty.Register(nameof(ButtonVisibility), typeof(Visibility), typeof(PropertyControl), new PropertyMetadata(Visibility.Collapsed));



    }
}
