// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ItemsBagTypeDescriptor.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Provides a custom type descriptor for the ItemsBag.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>
    /// Provides a custom type descriptor for the <see cref="ItemsBag" />.
    /// </summary>
    public class ItemsBagTypeDescriptor : CustomTypeDescriptor
    {
        /// <summary>
        /// 属性描述符集合缓存（按 BiggestType 缓存）。
        /// ItemsBag 的 Objects 在构造后不再变化，因此按 BiggestType 缓存是安全的；
        /// 若派生类会修改对象列表，则需要额外失效逻辑。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, PropertyDescriptorCollection> PropertiesCache =
            new ConcurrentDictionary<Type, PropertyDescriptorCollection>();

        /// <summary>
        /// The bag.
        /// </summary>
        private readonly ItemsBag bag;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsBagTypeDescriptor" /> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="instance">The instance.</param>
        public ItemsBagTypeDescriptor(ICustomTypeDescriptor parent, object instance)
            : base(parent)
        {
            this.bag = (ItemsBag)instance;
        }

        /// <summary>
        /// Get the properties of the items bag.
        /// </summary>
        /// <returns>
        /// The property descriptor collection.
        /// </returns>
        public override PropertyDescriptorCollection GetProperties()
        {
            // 按 BiggestType 缓存描述符集合，避免每次 GetProperties 都新建 N 个 descriptor
            return PropertiesCache.GetOrAdd(this.bag.BiggestType, t =>
            {
                var result = new List<PropertyDescriptor>();
                foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(t))
                {
                    result.Add(new ItemsBagPropertyDescriptor(pd, t));
                }

                return new PropertyDescriptorCollection(result.ToArray());
            });
        }
    }
}