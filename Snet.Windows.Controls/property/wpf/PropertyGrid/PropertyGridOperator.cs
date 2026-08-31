// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PropertyGridOperator.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Cretes a model for the PropertyGrid control.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using Snet.Model.@enum;
    using Snet.Utility;
    using Snet.Windows.Controls.property.core.DataAnnotations;
    using Snet.Windows.Controls.property.wpf.Operators;
    using Snet.Windows.Core.handler;
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Data;
    using DataType = System.ComponentModel.DataAnnotations.DataType;

    /// <summary>
    /// Creates a model for the <see cref="PropertyGrid" /> control.
    /// </summary>
    public class PropertyGridOperator : DefaultLocalizableOperator, IPropertyGridOperator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyGridOperator" /> class.
        /// </summary>
        public PropertyGridOperator()
        {
            this.EnabledPattern = "Is{0}Enabled";
            this.VisiblePattern = "Is{0}Visible";
            this.OptionalPattern = "Use{0}";
            this.ModifyCamelCaseDisplayNames = true;
            this.InheritCategories = true;
        }

        /// <summary>
        /// 当前活动的、需要随语言变化刷新的属性项注册表（弱引用，避免语言订阅持有整个属性网格导致泄漏）。
        /// </summary>
        private static readonly List<LanguageItemRegistration> ActiveLanguageItems = new List<LanguageItemRegistration>();

        /// <summary>
        /// 保护 <see cref="ActiveLanguageItems"/> 的锁。
        /// </summary>
        private static readonly object LanguageItemsLock = new object();

        /// <summary>
        /// 反射缓存：每个类型的 MetadataTypeAttribute（伙伴类）。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, MetadataTypeAttribute> MetadataTypeAttributeCache = new ConcurrentDictionary<Type, MetadataTypeAttribute>();

        /// <summary>
        /// 反射缓存：每个类型的可浏览属性描述符列表（保持原有顺序）。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, PropertyDescriptor[]> BrowsablePropertiesCache = new ConcurrentDictionary<Type, PropertyDescriptor[]>();

        /// <summary>
        /// 反射缓存：每个类型的 AreBrowsableAttributesJustTrue 结果。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, bool> BrowsableJustTrueCache = new ConcurrentDictionary<Type, bool>();

        /// <summary>
        /// 反射缓存：PropertyInfo（按 (类型, 属性名, 属性类型) 键，属性类型可为 null）。
        /// </summary>
        private static readonly ConcurrentDictionary<PropertyLookupKey, PropertyInfo> PropertyInfoCache = new ConcurrentDictionary<PropertyLookupKey, PropertyInfo>();

        /// <summary>
        /// Initializes static members of the <see cref="PropertyGridOperator" /> class.
        /// </summary>
        static PropertyGridOperator()
        {
            // 语言事件只静态注册一次：语言变化时统一刷新当前活动的属性项（避免每个属性每次重建都重复订阅导致泄漏）
            Snet.Core.handler.LanguageHandler.OnLanguageEventAsync += (s, e) => OnLanguageEvent(s, e);
        }



        /// <summary>
        /// Gets or sets the default name of the category.
        /// </summary>
        /// <value>The default name of the category.</value>
        public string DefaultCategoryName { get; set; }

        /// <summary>
        /// Gets or sets the default name of the tab.
        /// </summary>
        /// <value>The default name of the tab.</value>
        public string DefaultTabName { get; set; }

        /// <summary>
        /// Gets or sets the enabled pattern.
        /// </summary>
        /// <value>The enabled pattern.</value>
        public string EnabledPattern { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether each property should inherit the category attribute from the property declared before.
        /// </summary>
        public bool InheritCategories { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to add spaces at the camel bumps of the display names.
        /// </summary>
        /// <value><c>true</c> if display names should be modified; otherwise, <c>false</c> .</value>
        public bool ModifyCamelCaseDisplayNames { get; set; }

        /// <summary>
        /// Gets or sets the optional pattern.
        /// </summary>
        /// <value>The optional pattern.</value>
        public string OptionalPattern { get; set; }

        /// <summary>
        /// Gets or sets the visible pattern.
        /// </summary>
        /// <value>The visible pattern.</value>
        public string VisiblePattern { get; set; }

        /// <summary>
        /// Gets or sets the current category.
        /// </summary>
        /// <value>The current category.</value>
        protected string CurrentCategory { get; set; }

        /// <summary>
        /// Gets or sets the declaring type of the current category.
        /// </summary>
        /// <value>The type of the current category.</value>
        protected Type CurrentCategoryDeclaringType { get; set; }

        /// <summary>
        /// Gets or sets the type of the current component.
        /// </summary>
        /// <value>The type of the current component.</value>
        /// <remarks>This is used to avoid that Category attributes are inherited from superclass to subclass.</remarks>
        protected Type CurrentDeclaringType { get; set; }

        /// <summary>
        /// Creates the property model.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="isEnumerable">if set to <c>true</c> [is enumerable].</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// A sorted list of <see cref="Tab" /> .
        /// </returns>
        public virtual IEnumerable<Tab> CreateModel(object instance, bool isEnumerable, IPropertyGridOptions options)
        {
            if (instance == null)
            {
                return null;
            }

            this.Reset();

            var tabs = new Dictionary<string, Tab>();
            foreach (var pi in this.CreatePropertyItems(instance, options).OrderBy(t => t.SortIndex))
            {
                var tabHeader = pi.Tab ?? string.Empty;
                if (!tabs.ContainsKey(tabHeader))
                {
                    tabs.Add(tabHeader, new Tab { Header = pi.Tab });
                }

                var tab = tabs[tabHeader];
                var category = pi.Category;
                var group = tab.Groups.FirstOrDefault(g => g.Header == category);
                if (group == null)
                {
                    group = new Group { Header = pi.Category, Name = pi.CategoryIdentifier };
                    tab.Groups.Add(group);
                }

                #region Set tab sort index

                if (tab.TabIndex == null)
                {
                    tab.TabIndex = pi.TabSortIndex;
                }
                else if (pi.TabSortIndex != null && pi.TabSortIndex != tab.TabIndex)
                {
                    throw new ApplicationException(
                        String.Format("Two or more different tab indecies ({0} and {1}) are set for same tab '{2}'.",
                            tab.TabIndex, pi.TabSortIndex, tabHeader
                    ));
                }

                #endregion

                #region Set group sort index

                if (group.GroupSortIndex == null)
                {
                    group.GroupSortIndex = pi.GroupSortIndex;
                }
                else if (pi.GroupSortIndex != null && pi.GroupSortIndex != group.GroupSortIndex)
                {
                    throw new ApplicationException(
                        String.Format("Two or more different group indecies ({0} and {1}) are set for same group '{2}'.",
                            group.GroupSortIndex, pi.GroupSortIndex, group.Name
                    ));
                }

                #endregion

                group.Properties.Add(pi);
            }

            return tabs.Values
                .OrderBy(t => t.TabIndex ?? 0) // sorting tabs
                .Select(t => t.SortGroups()) // sorting groups inside tab
                .ToList();
        }

        /// <summary>
        /// Creates a property item.
        /// </summary>
        /// <param name="pd">The property descriptor.</param>
        /// <param name="propertyDescriptors">The property descriptors.</param>
        /// <param name="instance">The instance.</param>
        /// <returns>
        /// A property item.
        /// </returns>
        public virtual PropertyItem CreatePropertyItem(PropertyDescriptor pd, PropertyDescriptorCollection propertyDescriptors, object instance)
        {
            var pi = this.CreateCore(pd, propertyDescriptors);
            this.SetProperties(pi, instance);
            return pi;
        }

        /// <summary>
        /// Resets this factory.
        /// </summary>
        public void Reset()
        {
            this.CurrentCategory = null;
            this.CurrentDeclaringType = null;
            this.CurrentCategoryDeclaringType = null;
        }

        /// <summary>
        /// Creates property items for all properties in the specified object.
        /// </summary>
        /// <param name="instance">The object instance.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// Enumeration of PropertyItem.
        /// </returns>
        protected virtual IEnumerable<PropertyItem> CreatePropertyItems(object instance, IPropertyGridOptions options)
        {
            var properties = this.GetPropertyCollection(instance);
            foreach (var pd in this.GetVisibleProperties(properties, instance, options))
            {
                yield return this.CreatePropertyItem(pd, properties, instance);
            }
        }

        /// <summary>
        /// Gets the property descriptor collection for the specified object.
        /// </summary>
        /// <param name="instance">The object instance.</param>
        /// <returns>The property collection</returns>
        protected virtual PropertyDescriptorCollection GetPropertyCollection(object instance)
        {
            var instanceType = instance.GetType();

            // 缓存 MetadataTypeAttribute（伙伴类）的反射查找结果
            var metadataTypeAttribute = MetadataTypeAttributeCache.GetOrAdd(instanceType, t =>
                t.GetCustomAttributes(typeof(MetadataTypeAttribute), true)
                 .OfType<MetadataTypeAttribute>().FirstOrDefault());
            PropertyDescriptorCollection properties;
            if (metadataTypeAttribute != null)
            {
                // use the metadata type for reflection
                instanceType = metadataTypeAttribute.MetadataClassType;
                properties = TypeDescriptor.GetProperties(instanceType);
            }
            else
            {
                properties = TypeDescriptor.GetProperties(instance);
            }

            return properties;
        }

        /// <summary>
        /// Gets the visible properties from the specified property descriptor collection.
        /// </summary>
        /// <param name="properties">The property descriptor collection.</param>
        /// <param name="instance">The object instance.</param>
        /// <param name="options">The options.</param>
        /// <returns>A sequence of property descriptors.</returns>
        protected IEnumerable<PropertyDescriptor> GetVisibleProperties(PropertyDescriptorCollection properties, object instance, IPropertyGridOptions options)
        {
            var instanceType = instance.GetType();

            foreach (PropertyDescriptor pd in this.GetBrowsablePropertiesCached(instance, properties))
            {
                if (options.ShowDeclaredOnly && pd.ComponentType != instanceType)
                {
                    continue;
                }

                // Read-only properties
                if (!options.ShowReadOnlyProperties && pd.IsReadOnly())
                {
                    continue;
                }

                // If RequiredAttribute is set, skip properties that don't have the given attribute
                if (options.RequiredAttribute != null && pd.GetFirstAttributeOrDefault(options.RequiredAttribute) == null)
                {
                    continue;
                }

                yield return pd;
            }
        }

        /// <summary>
        /// Gets the visible properties from the specified property descriptor collection.
        /// </summary>
        /// <param name="properties">The property descriptor collection.</param>
        /// <returns>A sequence of property descriptors.</returns>
        protected IEnumerable<PropertyDescriptor> GetBrowsableProperties(PropertyDescriptorCollection properties)
        {
            bool justTrue = AreBrowsableAttributesJustTrue(properties);
            return GetBrowsablePropertiesCore(properties, justTrue);
        }

        /// <summary>
        /// 按类型缓存可浏览属性列表（保持原有顺序与过滤逻辑）。
        /// ItemsBag 的属性集合是按实例生成的（取决于 BiggestType），不能按类型缓存；
        /// 仅当类未被派生类重写时才启用缓存，避免改变派生类的行为。
        /// </summary>
        /// <param name="instance">对象实例。</param>
        /// <param name="properties">属性描述符集合。</param>
        /// <returns>可浏览的属性描述符序列。</returns>
        private IEnumerable<PropertyDescriptor> GetBrowsablePropertiesCached(object instance, PropertyDescriptorCollection properties)
        {
            var instanceType = instance.GetType();
            if (this.GetType() != typeof(PropertyGridOperator) || typeof(ItemsBag).IsAssignableFrom(instanceType))
            {
                return this.GetBrowsableProperties(properties);
            }

            bool justTrue = BrowsableJustTrueCache.GetOrAdd(instanceType, t => this.AreBrowsableAttributesJustTrue(properties));
            PropertyDescriptor[] list = BrowsablePropertiesCache.GetOrAdd(instanceType, t => GetBrowsablePropertiesCore(properties, justTrue).ToArray());
            return list;
        }

        /// <summary>
        /// 可浏览属性过滤的核心逻辑（与原有行为完全一致）。
        /// </summary>
        /// <param name="properties">属性描述符集合。</param>
        /// <param name="justTrue">是否所有 Browsable 特性均为 true（需要 opt-in）。</param>
        /// <returns>可浏览的属性描述符序列。</returns>
        private static IEnumerable<PropertyDescriptor> GetBrowsablePropertiesCore(PropertyDescriptorCollection properties, bool justTrue)
        {
            foreach (PropertyDescriptor pd in properties)
            {
                var portableBrowsableAttribute = pd.GetFirstAttributeOrDefault<core.DataAnnotations.BrowsableAttribute>();
                var systemBrowsableAttribute = pd.GetFirstAttributeOrDefault<System.ComponentModel.BrowsableAttribute>();

                // If all BrowsableAttributes in the properties are set to true, the default will be changed to false, e.g. you need to opt-in.
                if (justTrue)
                {
                    // Skip properties not marked with [Browsable()]
                    if (portableBrowsableAttribute == null && systemBrowsableAttribute == null)
                    {
                        continue;
                    }

                    // Skip properties not marked with [Browsable(true)]
                    if (portableBrowsableAttribute != null && !portableBrowsableAttribute.Browsable)
                    {
                        continue;
                    }

                    // Skip properties not marked with [Browsable(true)]
                    if (systemBrowsableAttribute != null && !systemBrowsableAttribute.Browsable)
                    {
                        continue;
                    }
                }
                // if any BrowsableAttribute in the properties are set to false, the default is true, e.g. you need to opt-out.
                else
                {
                    // Skip properties marked with [Snet.Windows.Controls.property.core.DataAnnotations.Browsable(false)]
                    if (portableBrowsableAttribute != null && !portableBrowsableAttribute.Browsable)
                    {
                        continue;
                    }

                    // Skip properties marked with [System.ComponentModel.Browsable(false)]
                    if (!pd.IsBrowsable)
                    {
                        continue;
                    }
                }

                yield return pd;
            }
        }

        /// <summary>
        /// Iterates over <see cref="PropertyDescriptorCollection"/> and determines whether the value of <see cref="System.ComponentModel.BrowsableAttribute"/>
        /// or <see cref="DataAnnotations.BrowsableAttribute"/>, for those <see cref="PropertyDescriptor"/>s with such Attributes, is exclusively <see cref="true"/>
        /// </summary>
        /// <param name="propertyDescriptors">The collection of property descriptors.</param>
        /// <returns>
        /// A boolean
        /// </returns>
        protected bool AreBrowsableAttributesJustTrue(PropertyDescriptorCollection propertyDescriptors)
        {
            var attributes = propertyDescriptors.OfType<PropertyDescriptor>()
              .Select(pd => Tuple.Create(pd.GetFirstAttributeOrDefault<core.DataAnnotations.BrowsableAttribute>(), pd.GetFirstAttributeOrDefault<System.ComponentModel.BrowsableAttribute>()))
              .ToArray();

            bool isAnyFalse = attributes.Any(a => (a.Item1 != null && a.Item1.Browsable == false) || (a.Item2 != null && a.Item2.Browsable == false));
            bool isAnyTrue = attributes.Any(a => (a.Item1 != null && a.Item1.Browsable) || (a.Item2 != null && a.Item2.Browsable));

            return isAnyTrue && !isAnyFalse;
        }

        /// <summary>
        /// Creates the property item instance.
        /// </summary>
        /// <param name="pd">The property descriptor.</param>
        /// <param name="propertyDescriptors">The collection of property descriptors.</param>
        /// <returns>
        /// A property item.
        /// </returns>
        protected virtual PropertyItem CreateCore(PropertyDescriptor pd, PropertyDescriptorCollection propertyDescriptors)
        {
            return new PropertyItem(pd, propertyDescriptors);
        }

        /// <summary>
        /// Gets the category for the specified property.
        /// </summary>
        /// <param name="pd">The property descriptor.</param>
        /// <param name="declaringType">The declaring type.</param>
        /// <returns>
        /// A category string.
        /// </returns>
        protected virtual string GetCategory(PropertyDescriptor pd, Type declaringType)
        {
            return pd.GetCategory();
        }

        /// <summary>
        /// Gets the description for the specified property.
        /// </summary>
        /// <param name="pd">The property descriptor.</param>
        /// <param name="declaringType">The declaring type.</param>
        /// <returns>
        /// A description string.
        /// </returns>
        protected virtual string GetDescription(PropertyDescriptor pd, Type declaringType)
        {
            return pd.GetDescription();
        }

        /// <summary>
        /// Gets the display name for the specified property.
        /// </summary>
        /// <param name="pd">The property descriptor.</param>
        /// <param name="declaringType">The declaring type.</param>
        /// <returns>
        /// A display name string.
        /// </returns>
        protected virtual string GetDisplayName(PropertyDescriptor pd, Type declaringType)
        {
            var displayName = pd.GetDisplayName();

            if (this.ModifyCamelCaseDisplayNames && displayName == pd.Name)
            {
                displayName = StringUtilities.FromCamelCase(displayName);
            }

            return displayName;
        }

        /// <summary>
        /// Sets the properties.
        /// </summary>
        /// <param name="pi">The property item.</param>
        /// <param name="instance">The instance.</param>
        protected virtual void SetProperties(PropertyItem pi, object instance)
        {
            var tabName = this.DefaultTabName ?? instance.GetType().Name;
            var categoryName = this.DefaultCategoryName;

            // find the declaring type
            var declaringType = pi.Descriptor.ComponentType;
            var propertyInfo = GetPropertyCached(instance.GetType(), pi.Descriptor.Name, pi.Descriptor.PropertyType);
            if (propertyInfo != null)
            {
                declaringType = propertyInfo.DeclaringType;
            }

            if (declaringType != this.CurrentDeclaringType)
            {
                this.CurrentCategory = null;
            }

            this.CurrentDeclaringType = declaringType;

            if (!this.InheritCategories)
            {
                this.CurrentCategory = null;
            }

            var ca = pi.GetAttribute<System.ComponentModel.CategoryAttribute>();
            if (ca != null)
            {
                this.CurrentCategory = ca.Category;
                this.CurrentCategoryDeclaringType = declaringType;
            }

            var ca2 = pi.GetAttribute<core.DataAnnotations.CategoryAttribute>();
            if (ca2 != null)
            {
                this.CurrentCategory = ca2.Category;
                this.CurrentCategoryDeclaringType = declaringType;
            }

            var category = this.CurrentCategory ?? (this.DefaultCategoryName ?? this.GetCategory(pi.Descriptor, declaringType));

            if (category != null)
            {
                var items = category.Split('|');
                if (items.Length == 2)
                {
                    tabName = items[0];
                    categoryName = items[1];
                }

                if (items.Length == 1)
                {
                    categoryName = items[0];
                }
            }

            var displayName = this.GetDisplayName(pi.Descriptor, declaringType);
            var description = this.GetDescription(pi.Descriptor, declaringType);

            pi.CategoryIdentifier = categoryName;

            // set tab/group sort index
            pi.TabSortIndex = ca2?.TabSortIndex;
            pi.GroupSortIndex = ca2?.GroupSortIndex;

            // 语言变化事件：注册到静态弱引用列表（事件只静态注册一次，避免每个属性每次重建都重复订阅导致泄漏）
            SetLang(pi, declaringType, displayName, description, LanguageHandler.GetLanguage());
            this.RegisterLanguageItem(pi, declaringType, displayName, description);

            pi.Category = this.GetLocalizedString(categoryName, this.CurrentCategoryDeclaringType);
            pi.Tab = this.GetLocalizedString(tabName, this.CurrentCategoryDeclaringType);

            pi.IsReadOnly = pi.Descriptor.IsReadOnly();

            // Find descriptors by convention
            pi.IsEnabledDescriptor = pi.GetDescriptor(string.Format(this.EnabledPattern, pi.PropertyName));
            var isVisibleDescriptor = pi.GetDescriptor(string.Format(this.VisiblePattern, pi.PropertyName));
            if (isVisibleDescriptor != null)
            {
                pi.IsVisibleDescriptor = isVisibleDescriptor;
                pi.IsVisibleValue = true;
            }

            pi.OptionalDescriptor = pi.GetDescriptor(string.Format(this.OptionalPattern, pi.PropertyName));

            foreach (Attribute attribute in pi.Descriptor.Attributes)
            {
                this.SetAttribute(attribute, pi, instance);
            }

            pi.IsOptional = pi.IsOptional || pi.OptionalDescriptor != null;

            if (pi.Descriptor.PropertyType == typeof(TimeSpan) && pi.Converter == null)
            {
                pi.Converter = new TimeSpanToStringConverter();
                pi.ConverterParameter = pi.FormatString;
            }

            var underlyingType = Nullable.GetUnderlyingType(pi.Descriptor.PropertyType);
            if ((pi.Descriptor.PropertyType == typeof(DateTime) || underlyingType == typeof(DateTime))
                && pi.Converter == null
                && !string.IsNullOrWhiteSpace(pi.FormatString))
            {
                pi.Converter = new DateTimeToStringConverter();
                pi.ConverterParameter = pi.FormatString;
            }
        }

        /// <summary>
        /// 设置语言
        /// </summary>
        private void SetLang(PropertyItem pi, Type declaringType, string displayName, string description, LanguageType language)
        {
            // snet 语言加载设置
            switch (language)
            {
                case Model.@enum.LanguageType.zh:
                    pi.DisplayName = this.GetLocalizedString(description, declaringType);
                    if (pi.DisplayName.IsNullOrWhiteSpace())
                    {
                        pi.DisplayName = pi.PropertyName;
                    }
                    pi.Description = pi.PropertyName;
                    break;
                case Model.@enum.LanguageType.en:
                    pi.DisplayName = this.GetLocalizedString(displayName, declaringType);
                    pi.Description = this.GetLocalizedString(description, declaringType);
                    if (pi.Description.IsNullOrWhiteSpace())
                    {
                        pi.Description = pi.PropertyName;
                    }
                    break;
            }
        }

        /// <summary>
        /// 注册一个需要随语言变化刷新的属性项。
        /// </summary>
        /// <param name="pi">属性项。</param>
        /// <param name="declaringType">声明类型。</param>
        /// <param name="displayName">显示名。</param>
        /// <param name="description">描述。</param>
        private void RegisterLanguageItem(PropertyItem pi, Type declaringType, string displayName, string description)
        {
            lock (LanguageItemsLock)
            {
                // 列表过大时先清理失效项，避免无界增长
                if (ActiveLanguageItems.Count > 1024)
                {
                    ActiveLanguageItems.RemoveAll(r => !r.ItemReference.IsAlive || !r.OperatorReference.IsAlive);
                }

                ActiveLanguageItems.Add(new LanguageItemRegistration(this, pi, declaringType, displayName, description));
            }
        }

        /// <summary>
        /// 语言变化事件处理器（静态只注册一次）：遍历当前活动的属性项并重新应用语言。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">事件参数。</param>
        /// <returns>已完成的任务。</returns>
        /// <summary>
        /// 语言事件触发（语言切换完成时由 Snet.Core 发出）。<br/>
        /// 注意：此处必须读取 Snet.Core 的**内存态**语言（事件触发时已是最新），
        /// 不能用 Snet.Windows.Core.LanguageHandler.GetLanguage()——它读 language.json 文件，
        /// 而 SetLanguageAsync 中文件写入发生在语言切换事件之后，会导致读取到旧语言（属性框显示滞后）。
        /// </summary>
        private static Task OnLanguageEvent(object sender, object e)
        {
            var language = Snet.Core.handler.LanguageHandler.GetLanguage();
            List<LanguageItemRegistration> alive;
            lock (LanguageItemsLock)
            {
                alive = new List<LanguageItemRegistration>(ActiveLanguageItems.Count);
                for (int i = ActiveLanguageItems.Count - 1; i >= 0; i--)
                {
                    var registration = ActiveLanguageItems[i];
                    if (registration.ItemReference.IsAlive && registration.OperatorReference.IsAlive)
                    {
                        alive.Add(registration);
                    }
                    else
                    {
                        ActiveLanguageItems.RemoveAt(i);
                    }
                }
            }

            foreach (var registration in alive)
            {
                var item = registration.ItemReference.Target as PropertyItem;
                var operatorInstance = registration.OperatorReference.Target as PropertyGridOperator;
                if (item != null && operatorInstance != null)
                {
                    operatorInstance.SetLang(item, registration.DeclaringType, registration.DisplayName, registration.Description, language);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 反射缓存：获取属性的 PropertyInfo（按 (类型, 属性名, 属性类型) 缓存）。
        /// </summary>
        /// <param name="type">对象类型。</param>
        /// <param name="name">属性名。</param>
        /// <param name="propertyType">属性类型，可为 null（表示不限定类型）。</param>
        /// <returns>属性的 <see cref="PropertyInfo"/>，未找到时返回 <c>null</c>。</returns>
        private static PropertyInfo GetPropertyCached(Type type, string name, Type propertyType)
        {
            if (type == null || name == null)
            {
                return null;
            }

            var key = new PropertyLookupKey(type, name, propertyType);
            return PropertyInfoCache.GetOrAdd(key, k => k.PropertyType != null ? k.Type.GetProperty(k.Name, k.PropertyType) : k.Type.GetProperty(k.Name));
        }

        /// <summary>
        /// PropertyInfo 缓存的键。
        /// </summary>
        private struct PropertyLookupKey : IEquatable<PropertyLookupKey>
        {
            public PropertyLookupKey(Type type, string name, Type propertyType)
            {
                this.Type = type;
                this.Name = name;
                this.PropertyType = propertyType;
            }

            public Type Type { get; }

            public string Name { get; }

            public Type PropertyType { get; }

            public bool Equals(PropertyLookupKey other)
            {
                return this.Type == other.Type
                       && string.Equals(this.Name, other.Name, StringComparison.Ordinal)
                       && this.PropertyType == other.PropertyType;
            }

            public override bool Equals(object obj)
            {
                return obj is PropertyLookupKey other && this.Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = this.Type != null ? this.Type.GetHashCode() : 0;
                    hash = (hash * 397) ^ (this.Name != null ? StringComparer.Ordinal.GetHashCode(this.Name) : 0);
                    hash = (hash * 397) ^ (this.PropertyType != null ? this.PropertyType.GetHashCode() : 0);
                    return hash;
                }
            }
        }

        /// <summary>
        /// 记录一个需要随语言变化刷新的属性项（弱引用，避免语言订阅持有整个属性网格导致泄漏）。
        /// </summary>
        private sealed class LanguageItemRegistration
        {
            public LanguageItemRegistration(PropertyGridOperator operatorInstance, PropertyItem item, Type declaringType, string displayName, string description)
            {
                this.ItemReference = new WeakReference(item);
                this.OperatorReference = new WeakReference(operatorInstance);
                this.DeclaringType = declaringType;
                this.DisplayName = displayName;
                this.Description = description;
            }

            /// <summary>
            /// Gets the weak reference to the property item.
            /// </summary>
            public WeakReference ItemReference { get; }

            /// <summary>
            /// Gets the weak reference to the operator instance.
            /// </summary>
            public WeakReference OperatorReference { get; }

            /// <summary>
            /// Gets the declaring type.
            /// </summary>
            public Type DeclaringType { get; }

            /// <summary>
            /// Gets the display name.
            /// </summary>
            public string DisplayName { get; }

            /// <summary>
            /// Gets the description.
            /// </summary>
            public string Description { get; }
        }

        /// <summary>
        /// Sets the attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="pi">The pi.</param>
        /// <param name="instance">The instance.</param>
        protected virtual void SetAttribute(Attribute attribute, PropertyItem pi, object instance)
        {
            var ssa = attribute as SelectorStyleAttribute;
            if (ssa != null)
            {
                pi.SelectorStyle = ssa.SelectorStyle;
            }

            var svpa = attribute as SelectedValuePathAttribute;
            if (svpa != null)
            {
                pi.SelectedValuePath = svpa.Path;
            }

            var dmpa = attribute as DisplayMemberPathAttribute;
            if (dmpa != null)
            {
                pi.DisplayMemberPath = dmpa.Path;
            }

            var f = attribute as FontAttribute;
            if (f != null)
            {
                pi.FontSize = f.FontSize;
                pi.FontWeight = f.FontWeight;
                pi.FontFamily = f.FontFamily;
            }

            var fp = attribute as FontPreviewAttribute;
            if (fp != null)
            {
                pi.PreviewFonts = true;
                pi.FontSize = fp.Size;
                pi.FontWeight = fp.Weight;
                pi.FontFamilyPropertyDescriptor = pi.GetDescriptor(fp.FontFamilyPropertyName);
            }

            if (attribute is FontFamilySelectorAttribute)
            {
                pi.IsFontFamilySelector = true;
            }

            var ifpa = attribute as InputFilePathAttribute;
            if (ifpa != null)
            {
                pi.IsFilePath = true;
                pi.IsFileOpenDialog = true;
                pi.FilePathFilter = ifpa.Filter;
                pi.FilePathDefaultExtension = ifpa.DefaultExtension;
            }

            var ofpa = attribute as OutputFilePathAttribute;
            if (ofpa != null)
            {
                pi.IsFilePath = true;
                pi.IsFileOpenDialog = false;
                pi.FilePathFilter = ofpa.Filter;
                pi.FilePathDefaultExtension = ofpa.DefaultExtension;
            }

            if (attribute is DirectoryPathAttribute)
            {
                pi.IsDirectoryPath = true;
            }

            var da = attribute as DataTypeAttribute;
            if (da != null)
            {
                pi.DataTypes.Add(da.DataType);
                switch (da.DataType)
                {
                    case DataType.MultilineText:
                        pi.AcceptsReturn = true;
                        break;
                    case DataType.Password:
                        pi.IsPassword = true;
                        break;
                }
            }

            var cpa = attribute as ColumnsPropertyAttribute;
            if (cpa != null)
            {
                var descriptor = pi.GetDescriptor(cpa.PropertyName);
                var columns = descriptor?.GetValue(instance) as IEnumerable<Column>;

                if (columns != null)
                {
                    var glc = new GridLengthConverter();
                    foreach (Column column in columns)
                    {
                        IValueConverter converter = null;
                        Type elementType = null;
                        object toolTip = null;
                        if (pi.ActualPropertyType.HasElementType)
                            elementType = pi.ActualPropertyType.GetElementType();
                        else if (typeof(ICollection).IsAssignableFrom(pi.ActualPropertyType))
                        {
                            try
                            {
                                Type[] genericArguments = pi.ActualPropertyType.GetGenericArguments();
                                if (genericArguments.Length > 0)
                                    elementType = genericArguments[0];
                            }
                            catch
                            {
                            }
                        }

                        if (elementType != null)
                        {
                            var columnProperty = GetPropertyCached(elementType, column.PropertyName, null);
                            object[] converterAttributes = columnProperty?.GetCustomAttributes(typeof(ConverterAttribute), true);
                            if (converterAttributes != null && converterAttributes.Length > 0)
                            {
                                Type converterType = ((ConverterAttribute)converterAttributes[0]).ConverterType;
                                if (converterType != null)
                                {
                                    converter = Activator.CreateInstance(converterType) as IValueConverter;
                                }
                            }

                            object[] descriptionAttributes = columnProperty?.GetCustomAttributes(typeof(core.DataAnnotations.DescriptionAttribute), true);
                            if (descriptionAttributes != null && descriptionAttributes.Length > 0)
                            {
                                toolTip = ((core.DataAnnotations.DescriptionAttribute)descriptionAttributes[0]).Description;
                            }
                        }

                        var cd = new ColumnDefinition
                        {
                            PropertyName = column.PropertyName,
                            Header = this.GetLocalizedString(column.Header, declaringType: elementType),
                            FormatString = column.FormatString,
                            Width = (GridLength)(glc.ConvertFromInvariantString(column.Width) ?? GridLength.Auto),
                            IsReadOnly = column.IsReadOnly,
                            HorizontalAlignment = StringUtilities.ToHorizontalAlignment(column.Alignment.ToString(CultureInfo.InvariantCulture)),
                            Converter = converter,
                            Tooltip = toolTip,
                        };

                        if (column.ItemsSourcePropertyName != null)
                        {
                            // use instance.GetType to be able to fetch static properties also
                            var p = instance.GetType().GetProperties().FirstOrDefault(x => x.Name == column.ItemsSourcePropertyName);
                            cd.ItemsSource = p?.GetValue(instance) as IEnumerable;
                        }

                        pi.Columns.Add(cd);
                    }
                }
            }

            var la = attribute as ListAttribute;
            if (la != null)
            {
                pi.ListCanAdd = la.CanAdd;
                pi.ListCanRemove = la.CanRemove;
                pi.ListMaximumNumberOfItems = la.MaximumNumberOfItems;
            }

            var ida = attribute as InputDirectionAttribute;
            if (ida != null)
            {
                pi.InputDirection = ida.InputDirection;
            }

            var eia = attribute as EasyInsertAttribute;
            if (eia != null)
            {
                pi.IsEasyInsertByKeyboardEnabled = eia.EasyInsertByKeyboard;
                pi.IsEasyInsertByMouseEnabled = eia.EasyInsertByMouse;
            }

            var sia = attribute as SortIndexAttribute;
            if (sia != null)
            {
                pi.SortIndex = sia.SortIndex;
            }

            var eba = attribute as EnableByAttribute;
            if (eba != null)
            {
                pi.IsEnabledDescriptor = pi.GetDescriptor(eba.PropertyName);
                pi.IsEnabledValue = eba.PropertyValue;
            }

            var vba = attribute as VisibleByAttribute;
            if (vba != null)
            {
                pi.IsVisibleDescriptor = pi.GetDescriptor(vba.PropertyName);
                pi.IsVisibleValue = vba.PropertyValue;
            }

            var oa = attribute as OptionalAttribute;
            if (oa != null)
            {
                pi.IsOptional = true;
                if (oa.PropertyName != null)
                {
                    pi.OptionalDescriptor = pi.GetDescriptor(oa.PropertyName);
                }
            }

            var ila = attribute as IndentationLevelAttribute;
            if (ila != null)
            {
                pi.IndentationLevel = ila.IndentationLevel;
            }

            var ra = attribute as EnableByRadioButtonAttribute;
            if (ra != null)
            {
                pi.RadioDescriptor = pi.GetDescriptor(ra.PropertyName);
                pi.RadioValue = ra.Value;
            }

            if (attribute is CommentAttribute)
            {
                pi.IsComment = true;
            }

            if (attribute is ContentAttribute)
            {
                pi.IsContent = true;
            }

            var ea = attribute as System.ComponentModel.DataAnnotations.EditableAttribute;
            if (ea != null)
            {
                pi.IsEditable = ea.AllowEdit;
            }

            var ea2 = attribute as core.DataAnnotations.EditableAttribute;
            if (ea2 != null)
            {
                pi.IsEditable = ea2.AllowEdit;
            }

            if (attribute is AutoUpdateTextAttribute)
            {
                pi.AutoUpdateText = true;
            }

            var ispa = attribute as ItemsSourcePropertyAttribute;
            if (ispa != null)
            {
                pi.ItemsSourceDescriptor = pi.GetDescriptor(ispa.PropertyName);
            }

            var liispa = attribute as ListItemItemsSourcePropertyAttribute;
            if (liispa != null)
            {
                var p = TypeDescriptor.GetProperties(instance)[liispa.PropertyName];
                var listItemItemsSource = p != null ? p.GetValue(instance) as IEnumerable : null;
                pi.ListItemItemsSource = listItemItemsSource;
            }

            var clpa = attribute as CheckableItemsAttribute;
            if (clpa != null)
            {
                pi.CheckableItemsIsCheckedPropertyName = clpa.IsCheckedPropertyName;
                pi.CheckableItemsContentPropertyName = clpa.ContentPropertyName;
            }

            var rpa = attribute as BasePathPropertyAttribute;
            if (rpa != null)
            {
                pi.RelativePathDescriptor = pi.GetDescriptor(rpa.BasePathPropertyName);
            }

            var fa = attribute as FilterPropertyAttribute;
            if (fa != null)
            {
                pi.FilterDescriptor = pi.GetDescriptor(fa.PropertyName);
            }

            var dea = attribute as DefaultExtensionPropertyAttribute;
            if (dea != null)
            {
                pi.DefaultExtensionDescriptor = pi.GetDescriptor(dea.PropertyName);
            }

            var fsa = attribute as FormatStringAttribute;
            if (fsa != null)
            {
                pi.FormatString = fsa.FormatString;
            }

            var coa = attribute as ConverterAttribute;
            if (coa != null)
            {
                pi.Converter = Activator.CreateInstance(coa.ConverterType) as IValueConverter;
            }

            var pa = attribute as ProgressAttribute;
            if (pa != null)
            {
                pi.IsProgress = true;
                pi.ProgressMinimum = pa.Minimum;
                pi.ProgressMaximum = pa.Maximum;
            }

            var sa = attribute as SlidableAttribute;
            if (sa != null)
            {
                pi.IsSlidable = true;
                pi.SliderMinimum = sa.Minimum;
                pi.SliderMaximum = sa.Maximum;
                pi.SliderSmallChange = sa.SmallChange;
                pi.SliderLargeChange = sa.LargeChange;
                pi.SliderSnapToTicks = sa.SnapToTicks;
                pi.SliderTickFrequency = sa.TickFrequency;
            }

            var spa = attribute as SpinnableAttribute;
            if (spa != null)
            {
                pi.IsSpinnable = true;
                pi.SpinMinimum = spa.Minimum;
                pi.SpinMaximum = spa.Maximum;
                pi.SpinSmallChange = spa.SmallChange;
                pi.SpinLargeChange = spa.LargeChange;
            }

            var wpa = attribute as WidePropertyAttribute;
            if (wpa != null)
            {
                pi.HeaderPlacement = wpa.ShowHeader ? HeaderPlacement.Above : HeaderPlacement.Hidden;
            }

            var wia = attribute as WidthAttribute;
            if (wia != null)
            {
                pi.Width = wia.Width;
            }

            var hpa = attribute as HeaderPlacementAttribute;
            if (hpa != null)
            {
                pi.HeaderPlacement = hpa.HeaderPlacement;
            }

            var ha = attribute as HorizontalAlignmentAttribute;
            if (ha != null)
            {
                pi.HorizontalAlignment = ha.HorizontalAlignment;
            }

            var hea = attribute as HeightAttribute;
            if (hea != null)
            {
                pi.Height = hea.Height;
                pi.MinimumHeight = hea.MinimumHeight;
                pi.MaximumHeight = hea.MaximumHeight;
                pi.AcceptsReturn = true;
            }

            var fta = attribute as FillTabAttribute;
            if (fta != null)
            {
                pi.FillTab = true;
                pi.AcceptsReturn = true;
            }
        }
    }
}