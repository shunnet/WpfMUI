using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Snet.Windows.Controls.pagebar
{
    /// <summary>
    /// 页码条子项
    /// </summary>
    public class PageBarItem : ContentControl
    {
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
        /// 值
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(PageBarItem), new PropertyMetadata(default(int)));

        /// <summary>
        /// 值
        /// </summary>
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// 当前颜色
        /// </summary>
        public static readonly DependencyProperty CurrentBrushProperty =
            DependencyProperty.Register("CurrentBrush", typeof(Brush), typeof(PageBarItem), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// 当前颜色
        /// </summary>
        public Brush CurrentBrush
        {
            get { return (Brush)GetValue(CurrentBrushProperty); }
            set { SetValue(CurrentBrushProperty, value); }
        }

        #endregion 依赖属性
    }

    /// <summary>
    /// 是否相等
    /// </summary>
    public class IsEqualConverter : IMultiValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values[0].Equals(values[1]);
        }

        /// <inheritdoc/>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}