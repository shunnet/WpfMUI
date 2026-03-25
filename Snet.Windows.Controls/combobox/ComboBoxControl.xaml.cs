using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Snet.Windows.Controls.combobox
{
    /// <summary>
    /// 自定义下拉框控件<br/>
    /// 支持图标、占位提示、数据源绑定和选中项双向绑定<br/>
    /// 通过依赖属性实现 MVVM 友好的数据绑定
    /// </summary>
    public partial class ComboBoxControl : UserControl
    {
        /// <summary>
        /// 构造函数：初始化下拉框控件组件
        /// </summary>
        public ComboBoxControl()
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
        /// 控制下拉框的高度，默认值为 30 像素
        /// </summary>
        public static readonly DependencyProperty HeightProperty =
            DependencyProperty.Register(nameof(Height), typeof(double), typeof(ComboBoxControl), new PropertyMetadata(30d));

        /// <summary>
        /// 获取或设置下拉框左侧图标
        /// </summary>
        public ImageSource Icon
        {
            get => (ImageSource)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
        /// <summary>
        /// 图标依赖属性<br/>
        /// 下拉框左侧显示的图标图像源
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(ComboBoxControl), new PropertyMetadata(null));

        /// <summary>
        /// 获取或设置占位提示内容<br/>
        /// 当无选中项时显示的提示信息
        /// </summary>
        public object Hint
        {
            get => (object)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }
        /// <summary>
        /// 占位提示依赖属性<br/>
        /// 下拉框无选中项时显示的水印提示
        /// </summary>
        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register(nameof(Hint), typeof(object), typeof(ComboBoxControl), new PropertyMetadata(null));

        /// <summary>
        /// 获取或设置显示成员路径<br/>
        /// 指定数据源中用于显示的属性名称
        /// </summary>
        public string DisplayMemberPath
        {
            get => (string)GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }
        /// <summary>
        /// 显示成员路径依赖属性<br/>
        /// 对应 ComboBox.DisplayMemberPath，指定显示属性
        /// </summary>
        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(ComboBoxControl), new PropertyMetadata(string.Empty));

        /// <summary>
        /// 获取或设置数据源<br/>
        /// 绑定到下拉列表的数据集合
        /// </summary>
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        /// <summary>
        /// 数据源依赖属性<br/>
        /// 提供下拉列表的选项数据
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
           DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ComboBoxControl), new PropertyMetadata(null));

        /// <summary>
        /// 获取或设置当前选中项<br/>
        /// 支持双向绑定（BindsTwoWayByDefault）
        /// </summary>
        public object SelectedItem
        {
            get => (object)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }
        /// <summary>
        /// 选中项依赖属性<br/>
        /// 双向绑定，自动同步 ViewModel 中的选中数据
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(ComboBoxControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    }
}
