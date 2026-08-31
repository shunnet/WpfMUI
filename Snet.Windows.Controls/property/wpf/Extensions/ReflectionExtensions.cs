// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReflectionExtensions.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Provides reflection extensions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Provides reflection extensions.
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// 枚举浏览过滤缓存（按枚举元素类型缓存完整值列表的过滤结果）。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, EnumBrowsableInfo> EnumBrowsableCache = new ConcurrentDictionary<Type, EnumBrowsableInfo>();

        /// <summary>
        /// Filters on the <see cref="System.ComponentModel.BrowsableAttribute" /> and <see cref="Snet.Windows.Controls.property.core.DataAnnotations.BrowsableAttribute" />.
        /// </summary>
        /// <typeparam name="T">The enumeration type.</typeparam>
        /// <param name="arr">The array.</param>
        /// <returns>
        /// The filtered values.
        /// </returns>
        public static List<object> FilterOnBrowsableAttribute<T>(this T arr) where T : IEnumerable
        {
            // 仅当传入的是完整枚举值集合（长度与定义值数量一致）时按类型缓存；
            // 其他情况（子集、非枚举）走原始逻辑，避免缓存污染。
            if (arr is Array array)
            {
                var elementType = array.GetType().GetElementType();
                if (elementType != null && elementType.IsEnum)
                {
                    var info = EnumBrowsableCache.GetOrAdd(elementType, BuildEnumBrowsableInfo);
                    if (array.Length == info.ValueCount)
                    {
                        // 返回副本，避免调用方修改缓存
                        return new List<object>(info.BrowsableValues);
                    }
                }
            }

            return FilterOnBrowsableAttributeCore(arr);
        }

        /// <summary>
        /// 构建枚举的浏览过滤缓存信息（基于完整值集合）。
        /// </summary>
        /// <param name="enumType">枚举类型。</param>
        /// <returns>缓存信息。</returns>
        private static EnumBrowsableInfo BuildEnumBrowsableInfo(Type enumType)
        {
            var values = Enum.GetValues(enumType);
            return new EnumBrowsableInfo(values.Length, FilterOnBrowsableAttributeCore(values));
        }

        /// <summary>
        /// 浏览过滤的核心逻辑（与原有行为完全一致）。
        /// </summary>
        /// <param name="arr">枚举值集合。</param>
        /// <returns>过滤后的值列表。</returns>
        private static List<object> FilterOnBrowsableAttributeCore(IEnumerable arr)
        {
            // Default empty list
            var res = new List<object>();

            // Loop each item in the enumerable
            foreach (var o in arr)
            {
                // Get the field information for the current field
                var field = o.GetType().GetField(
                    o.ToString(), BindingFlags.Static | BindingFlags.GetField | BindingFlags.Public);
                if (field != null)
                {
                    // Get the Browsable attribute, if it is declared for this field
                    var browsable = field.GetCustomAttributes<System.ComponentModel.BrowsableAttribute>(true).FirstOrDefault();
                    if (browsable != null)
                    {
                        // It is declared, is it true or false?
                        if (browsable.Browsable)
                        {
                            // Is true - field is not hidden so add it to the result.
                            res.Add(o);
                        }

                        continue;
                    }

                    var browsable2 = field.GetCustomAttributes<core.DataAnnotations.BrowsableAttribute>(true).FirstOrDefault();
                    if (browsable2 != null)
                    {
                        // It is declared, is it true or false?
                        if (browsable2.Browsable)
                        {
                            // Is true - field is not hidden so add it to the result.
                            res.Add(o);
                        }

                        continue;
                    }

                    // Not declared so add it to the result
                    res.Add(o);
                }
                else
                {
                    // Can't evaluate, include it.
                    res.Add(o);
                }
            }

            return res;
        }

        /// <summary>
        /// 缓存的枚举浏览过滤信息。
        /// </summary>
        private sealed class EnumBrowsableInfo
        {
            public EnumBrowsableInfo(int valueCount, List<object> browsableValues)
            {
                this.ValueCount = valueCount;
                this.BrowsableValues = browsableValues;
            }

            /// <summary>
            /// Gets the number of defined enum values.
            /// </summary>
            public int ValueCount { get; }

            /// <summary>
            /// Gets the filtered browsable values.
            /// </summary>
            public List<object> BrowsableValues { get; }
        }

        /// <summary>
        /// Gets the attributes of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of attribute to get.</typeparam>
        /// <param name="fieldInfo">The field info.</param>
        /// <param name="inherit">The inherit flag.</param>
        /// <returns>
        /// The attributes enumeration.
        /// </returns>
        public static IEnumerable<T> GetCustomAttributes<T>(this FieldInfo fieldInfo, bool inherit)
        {
            return fieldInfo.GetCustomAttributes(typeof(T), inherit).Cast<T>();
        }

        /// <summary>
        /// Return the first attribute of a given type for the specified property descriptor.
        /// </summary>
        /// <typeparam name="T">An attribute type.</typeparam>
        /// <param name="descriptor">The property descriptor.</param>
        /// <returns>The first attribute of the specified type.</returns>
        public static T GetFirstAttributeOrDefault<T>(this System.ComponentModel.PropertyDescriptor descriptor) where T : Attribute
        {
            return descriptor.Attributes.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Gets the value of the first attribute of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the attribute.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="descriptor">The descriptor.</param>
        /// <param name="func">The mapping function.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The value returned from the mapping function, or the default value if the attribute was not found.</returns>
        public static TResult GetAttributeValue<T, TResult>(this System.ComponentModel.PropertyDescriptor descriptor, Func<T, TResult> func, TResult defaultValue = default(TResult)) where T : Attribute
        {
            var a = descriptor.Attributes.OfType<T>().FirstOrDefault();
            return a != null ? func(a) : defaultValue;
        }

        /// <summary>
        /// Gets the first attribute of the specified type.
        /// </summary>
        /// <param name="descriptor">The descriptor.</param>
        /// <param name="attributeType">Type of the attribute.</param>
        /// <returns>The first attribute of the specified type.</returns>
        public static Attribute GetFirstAttributeOrDefault(this System.ComponentModel.PropertyDescriptor descriptor, Type attributeType)
        {
            return descriptor.Attributes.Cast<Attribute>().FirstOrDefault(attribute => attribute.GetType().IsAssignableFrom(attributeType));
        }

        /// <summary>
        /// Determines whether the property is read-only.
        /// </summary>
        /// <param name="pd">The property descriptor.</param>
        /// <returns><c>true</c> if the property is read-only; otherwise, <c>false</c>.</returns>
        public static bool IsReadOnly(this System.ComponentModel.PropertyDescriptor pd)
        {
            var a = pd.GetFirstAttributeOrDefault<core.DataAnnotations.ReadOnlyAttribute>();
            if (a != null)
            {
                return a.IsReadOnly;
            }

            return pd.IsReadOnly;
        }

        /// <summary>
        /// Gets the category for the specified property.
        /// </summary>
        /// <param name="pd">The property descriptor.</param>
        /// <returns>The category.</returns>
        public static string GetCategory(this System.ComponentModel.PropertyDescriptor pd)
        {
            var a = pd.GetFirstAttributeOrDefault<core.DataAnnotations.CategoryAttribute>();
            if (a != null)
            {
                return a.Category;
            }

            return pd.Category;
        }

        /// <summary>
        /// Gets the description for the specified property.
        /// </summary>
        /// <param name="pd">The property descriptor.</param>
        /// <returns>The description.</returns>
        public static string GetDescription(this System.ComponentModel.PropertyDescriptor pd)
        {
            var a = pd.GetFirstAttributeOrDefault<core.DataAnnotations.DescriptionAttribute>();
            if (a != null)
            {
                return a.Description;
            }

            return pd.Description;
        }

        /// <summary>
        /// Gets the display name for the specified property.
        /// </summary>
        /// <param name="pd">The property descriptor.</param>
        /// <returns>The display name.</returns>
        public static string GetDisplayName(this System.ComponentModel.PropertyDescriptor pd)
        {
            var a = pd.GetFirstAttributeOrDefault<core.DataAnnotations.DisplayNameAttribute>();
            if (a != null)
            {
                return a.DisplayName;
            }

            return pd.DisplayName;
        }
    }
}