using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Snet.Windows.Controls.property
{
    /// <summary>
    /// PropertyControl.xaml 的交互逻辑
    /// </summary>
    public partial class PropertyControl : UserControl
    {

        public PropertyControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 导出命令
        /// </summary>
        public ICommand ExpCommand
        {
            get => (ICommand)GetValue(ExpCommandProperty);
            set => SetValue(ExpCommandProperty, value);
        }
        public static readonly DependencyProperty ExpCommandProperty =
            DependencyProperty.Register(nameof(ExpCommand), typeof(ICommand), typeof(PropertyControl), new PropertyMetadata(null));

        /// <summary>
        /// 导入命令
        /// </summary>
        public ICommand IncCommand
        {
            get => (ICommand)GetValue(IncCommandProperty);
            set => SetValue(IncCommandProperty, value);
        }
        public static readonly DependencyProperty IncCommandProperty =
            DependencyProperty.Register(nameof(IncCommand), typeof(ICommand), typeof(PropertyControl), new PropertyMetadata(null));

        /// <summary>
        /// 基础数据
        /// </summary>
        public object BasicsData
        {
            get => GetValue(BasicsDataProperty);
            set => SetValue(BasicsDataProperty, value);
        }
        public static readonly DependencyProperty BasicsDataProperty =
            DependencyProperty.Register(nameof(BasicsData), typeof(object), typeof(PropertyControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    }
}
