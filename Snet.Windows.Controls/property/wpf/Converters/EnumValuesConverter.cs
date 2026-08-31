// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnumValuesConverter.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Converts an Enum to a list of the enum type values
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>
    /// Converts an Enum to a list of the enum type values
    /// </summary>
    [ValueConversion(typeof(Enum), typeof(string[]))]
    public class EnumValuesConverter : IValueConverter
    {
        /// <summary>
        /// 过滤后的枚举值列表缓存（按枚举类型，避免每次转换都做字段反射）。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, List<object>> EnumValuesCache = new ConcurrentDictionary<Type, List<object>>();

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
            if (value != null)
            {
                return GetCachedValues(value.GetType());
            }

            if (targetType == typeof(Enum))
            {
                return GetCachedValues(targetType);
            }

            return value;
        }

        /// <summary>
        /// 获取缓存的值列表（返回副本，避免调用方修改缓存）。
        /// </summary>
        /// <param name="enumType">枚举类型。</param>
        /// <returns>过滤后的值列表。</returns>
        private static List<object> GetCachedValues(Type enumType)
        {
            var cached = EnumValuesCache.GetOrAdd(enumType, t => Enum.GetValues(t).FilterOnBrowsableAttribute());
            return new List<object>(cached);
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
            throw new NotSupportedException();
        }
    }
}