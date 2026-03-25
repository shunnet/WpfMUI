using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Snet.Windows.Controls.pagebar
{
    /// <summary>
    /// 页码条子项控件，表示分页栏中的单个页码按钮。<br/>
    /// 支持页码值、点击命令和当前选中状态的颜色标记。
    /// </summary>
    public class PageBarItem : ContentControl
    {
        /// <summary>
        /// 静态构造函数，重写默认样式键以支持自定义模板
        /// </summary>
        static PageBarItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PageBarItem), new FrameworkPropertyMetadata(typeof(PageBarItem)));
        }

        #region 命令

        /// <summary>
        /// 点击页码命令
        /// </summary>
        public static readonly DependencyProperty PageNumberCommandProperty =
            DependencyProperty.Register("PageNumberCommand", typeof(IAsyncRelayCommand), typeof(PageBarItem));

        /// <summary>
        /// 点击页码命令
        /// </summary>
        public IAsyncRelayCommand PageNumberCommand
        {
            get { return (IAsyncRelayCommand)GetValue(PageNumberCommandProperty); }
            set { SetValue(PageNumberCommandProperty, value); }
        }

        #endregion 命令

        #region 依赖属性

        /// <summary>
        /// 页码值依赖属性
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(PageBarItem), new PropertyMetadata(default(int)));

        /// <summary>
        /// 获取或设置当前页码项的页码值
        /// </summary>
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// 当前选中状态的高亮画笔依赖属性
        /// </summary>
        public static readonly DependencyProperty CurrentBrushProperty =
            DependencyProperty.Register("CurrentBrush", typeof(Brush), typeof(PageBarItem), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// 获取或设置当前选中页码的高亮颜色画笔
        /// </summary>
        public Brush CurrentBrush
        {
            get { return (Brush)GetValue(CurrentBrushProperty); }
            set { SetValue(CurrentBrushProperty, value); }
        }

        #endregion 依赖属性
    }

    /// <summary>
    /// 值相等比较转换器<br/>
    /// 用于多值绑定中比较两个值是否相等，返回布尔值<br/>
    /// 通常用于页码条中判断当前页码是否为选中状态
    /// </summary>
    public class IsEqualConverter : IMultiValueConverter
    {
        /// <summary>
        /// 将多个绑定值转换为单个布尔值<br/>
        /// 比较 values[0] 与 values[1] 是否相等
        /// </summary>
        /// <param name="values">绑定值数组，至少包含两个元素</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换器参数</param>
        /// <param name="culture">区域性信息</param>
        /// <returns>两个值相等返回 true，否则返回 false</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return false;
            return values[0].Equals(values[1]);
        }

        /// <summary>
        /// 反向转换（不支持）<br/>
        /// 多值绑定的反向转换无实际意义，返回空数组
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Array.Empty<object>();
        }
    }
}