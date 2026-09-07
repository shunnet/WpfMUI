using System.Globalization;
using System.Windows.Data;

namespace Snet.Windows.Controls.converter
{
    /// <summary>
    /// 用于将绑定值与 RadioButton 的 IsChecked 状态进行双向转换。
    /// 支持将选中的 RadioButton 的参数值绑定回源对象。
    /// </summary>
    public class RadioButtonConverter : IValueConverter
    {
        /// <summary>
        /// 将绑定的值转换为 RadioButton 的 IsChecked 属性（true/false）。
        /// </summary>
        /// <param name="value">当前绑定的数据源的值</param>
        /// <param name="targetType">目标属性类型（通常是 bool）</param>
        /// <param name="parameter">RadioButton 的参数（表示该按钮的值）</param>
        /// <param name="culture">区域性信息</param>
        /// <returns>如果绑定值与参数相等，则返回 true，否则返回 false</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 注意：XAML 中 ConverterParameter=0 传入的是字符串 "0"（ConverterParameter 是 object 类型，不会被类型转换），
            // 而绑定源（如 DataFormat/DataType）是 int。int.Equals(string) 恒为 false，
            // 直接 Equals 比较会导致 RadioButton 永远无法勾选/勾选后被立即打回（选中后无反应），
            // 因此必须统一转字符串再比较。
            return value != null && parameter != null
                && value.ToString().Equals(parameter.ToString(), StringComparison.Ordinal);
        }

        /// <summary>
        /// 将 RadioButton 的 IsChecked 值转换回绑定源的值。
        /// </summary>
        /// <param name="value">RadioButton 的 IsChecked 值（通常是 bool）</param>
        /// <param name="targetType">源属性类型（通常是 string 或 enum）</param>
        /// <param name="parameter">RadioButton 的参数（代表选中的值）</param>
        /// <param name="culture">区域性信息</param>
        /// <returns>
        /// 如果 IsChecked 为 true，返回 parameter；
        /// 否则返回 Binding.DoNothing（不更新源）。
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool isChecked && isChecked)
                ? parameter?.ToString()
                : Binding.DoNothing;
        }
    }
}
