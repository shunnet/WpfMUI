// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnumDescriptionConverter.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Converts Enum instances to description string instances.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Data;

    /// <summary>
    /// Converts <see cref="Enum" /> instances to description <see cref="string" /> instances.
    /// </summary>
    [ValueConversion(typeof(object), typeof(string))]
    public class EnumDescriptionConverter : IValueConverter
    {
        /// <summary>
        /// 枚举值到描述文本的缓存（按枚举类型，避免每次转换都做字段反射）。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Dictionary<object, string>> EnumDescriptionCache =
            new ConcurrentDictionary<Type, Dictionary<object, string>>();

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
            if (value == null)
            {
                return string.Empty;
            }

            // Default, non-converted result.
            string result = value.ToString();

            var valueType = value.GetType();
            if (!valueType.IsEnum)
            {
                return result;
            }

            var descriptions = EnumDescriptionCache.GetOrAdd(valueType, BuildDescriptionMap);
            if (descriptions.TryGetValue(value, out var description))
            {
                result = description;
            }

            return result;
        }

        /// <summary>
        /// 构建枚举值到描述文本的映射（保留原有"第一个匹配字段"的语义）。
        /// </summary>
        /// <param name="enumType">枚举类型。</param>
        /// <returns>值到描述的映射。</returns>
        private static Dictionary<object, string> BuildDescriptionMap(Type enumType)
        {
            var map = new Dictionary<object, string>();
            var fields = enumType.GetFields(BindingFlags.Static | BindingFlags.GetField | BindingFlags.Public);
            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(null);
                if (map.ContainsKey(fieldValue))
                {
                    // 重复枚举值：保留第一个匹配（与原有 FirstOrDefault 语义一致）
                    continue;
                }

                string description = null;
                var descriptionAttribute = field.GetCustomAttributes<System.ComponentModel.DescriptionAttribute>(true).FirstOrDefault();
                if (descriptionAttribute != null)
                {
                    description = descriptionAttribute.Description;
                }

                var descriptionAttribute2 = field.GetCustomAttributes<Snet.Windows.Controls.property.core.DataAnnotations.DescriptionAttribute>(true).FirstOrDefault();
                if (descriptionAttribute2 != null)
                {
                    description = descriptionAttribute2.Description;
                }

                map.Add(fieldValue, description ?? fieldValue.ToString());
            }

            return map;
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