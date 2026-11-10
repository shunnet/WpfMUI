namespace Snet.Windows.Controls.property.wpf
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>
    /// 把属性描述文本转换成属性网格的提示内容。<br/>
    /// 转换委托给 <see cref="PropertyGrid"/> 的 <c>CreateToolTip</c>：自定义的 <see cref="PropertyGrid.ToolTipTemplate"/> 与
    /// "空描述不产生提示"的约定因此对所有布局一致生效；而绑定到 <see cref="PropertyItem.Description"/> 使提示随语言切换刷新。
    /// </summary>
    internal sealed class DescriptionToolTipConverter : IValueConverter
    {
        private readonly Func<string, object> createToolTip;

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptionToolTipConverter"/> class.
        /// </summary>
        /// <param name="createToolTip">根据描述文本创建提示内容的委托。</param>
        public DescriptionToolTipConverter(Func<string, object> createToolTip)
        {
            this.createToolTip = createToolTip ?? throw new ArgumentNullException(nameof(createToolTip));
        }

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 描述为 null 时按空串处理：CreateToolTip 对空/空白描述返回 null（即不显示提示）
            return this.createToolTip(value as string ?? string.Empty);
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
