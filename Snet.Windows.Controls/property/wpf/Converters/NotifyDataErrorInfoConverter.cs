// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataErrorInfoConverter.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Gets the error message for the bound data.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Gets the error message for the bound data.
    /// </summary>
    /// <remarks>Note that the instance and column(property) name must be provided in the constructor. There is probably a better way to do this....</remarks>
    public class NotifyDataErrorInfoConverter : IValueConverter
    {
        /// <summary>
        /// The instance
        /// </summary>
        private readonly INotifyDataErrorInfo instance;

        /// <summary>
        /// The column name
        /// </summary>
        private readonly string columnName;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyDataErrorInfoConverter" /> class.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="columnName">Name of the column.</param>
        public NotifyDataErrorInfoConverter(INotifyDataErrorInfo instance, string columnName)
        {
            this.instance = instance;
            this.columnName = columnName;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns <c>null</c>, the valid <c>null</c> value is used.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 逐属性判断：可见性与内容必须描述同一个属性。此前 bool/Visibility 用的是对象级的 instance.HasErrors，
            // 只要对象上任意一个属性出错，所有行的错误控件都可见（内容为空），与内容列的逐属性语义不一致
            switch (targetType)
            {
                case object _ when targetType == typeof(bool):
                    return Tab.HasPropertyErrors(this.instance, this.columnName);
                case object _ when targetType == typeof(Visibility):
                    return Tab.HasPropertyErrors(this.instance, this.columnName) ? Visibility.Visible : Visibility.Collapsed;
                default:
                    return this.instance.GetErrors(this.columnName)?.Cast<object>().FirstOrDefault();
            }
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns <c>null</c>, the valid <c>null</c> value is used.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}