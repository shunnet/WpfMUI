// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Tab.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Represents a tab in a PropertyGrid.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using Snet.Windows.Controls.property.core;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Represents a tab in a <see cref="PropertyGrid" />.
    /// </summary>
    public class Tab : Observable
    {
        /// <summary>
        /// Indicates whether the tab contains errors.
        /// </summary>
        private bool hasErrors;

        /// <summary>
        /// Indicates whether the tab contains warnnigs.
        /// </summary>
        private bool hasWarnings;

        /// <summary>
        /// 每属性的错误计数（增量维护，避免每次 ErrorsChanged 全表扫描）。
        /// </summary>
        private readonly Dictionary<string, bool> propertyErrorCounts = new Dictionary<string, bool>();

        /// <summary>
        /// 错误计数是否已初始化（首次全量扫描后切换为增量更新）。
        /// </summary>
        private bool errorCountsInitialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tab" /> class.
        /// </summary>
        public Tab()
        {
            this.Groups = new List<Group>();
        }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets the groups.
        /// </summary>
        public List<Group> Groups { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this tab contains properties with errors.
        /// </summary>
        /// <value><c>true</c> if this tab has errors; otherwise, <c>false</c>.</value>
        public bool HasErrors
        {
            get
            {
                return this.hasErrors;
            }

            set
            {
                this.SetValue(ref this.hasErrors, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this tab contains properties with warnings.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has warnings; otherwise, <c>false</c>.
        /// </value>
        public bool HasWarnings
        {
            get
            {
                return this.hasWarnings;
            }

            set
            {
                this.SetValue(ref this.hasWarnings, value);
            }
        }

        /// <summary>
        /// Gets or sets the header.
        /// </summary>
        /// <value>The header.</value>
        public string Header { get; set; }

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        public BitmapSource Icon { get; set; }

        /// <summary>
        /// Gets or sets the tab sort index.
        /// </summary>
        /// <value>The tab sort index.</value>
        public uint? TabIndex { get; set; }

        /// <summary>
        /// Determines whether the tab contains the specified property.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>
        /// <c>true</c> if the tab contains the specified property; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string propertyName)
        {
            return this.Groups.Any(g => g.Properties.Any(p => p.PropertyName == propertyName));
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Header;
        }

        /// <summary>
        /// Updates the has errors property (全量重算，兼容旧调用)。
        /// </summary>
        /// <param name="dei">The instance.</param>
        public void UpdateHasErrors(IDataErrorInfo dei)
        {
            foreach (var name in this.GetPropertyNames())
            {
                this.SetPropertyErrorCount(name, !string.IsNullOrEmpty(dei[name]));
            }

            this.errorCountsInitialized = true;
            this.UpdateHasErrorsCore();
        }

        /// <summary>
        /// Updates the has errors property (全量重算，兼容旧调用)。
        /// </summary>
        /// <param name="ndei">The instance.</param>
        public void UpdateHasErrors(INotifyDataErrorInfo ndei)
        {
            foreach (var name in this.GetPropertyNames())
            {
                this.SetPropertyErrorCount(name, HasPropertyErrors(ndei, name));
            }

            this.errorCountsInitialized = true;
            this.UpdateHasErrorsCore();
        }

        /// <summary>
        /// Updates the has errors property（增量：只重算指定属性的错误状态）。
        /// </summary>
        /// <param name="dei">The instance.</param>
        /// <param name="propertyName">发生变化的属性名。</param>
        public void UpdateHasErrors(IDataErrorInfo dei, string? propertyName)
        {
            if (!this.errorCountsInitialized)
            {
                this.InitializeErrorCounts(dei);
            }
            else if (!string.IsNullOrEmpty(propertyName))
            {
                this.SetPropertyErrorCount(propertyName, !string.IsNullOrEmpty(dei[propertyName]));
            }

            this.UpdateHasErrorsCore();
        }

        /// <summary>
        /// Updates the has errors property（增量：只重算指定属性的错误状态）。
        /// </summary>
        /// <param name="ndei">The instance.</param>
        /// <param name="propertyName">发生变化的属性名，null 或空表示对象整体错误变化，需要全量重算。</param>
        public void UpdateHasErrors(INotifyDataErrorInfo ndei, string? propertyName)
        {
            if (!this.errorCountsInitialized)
            {
                this.InitializeErrorCounts(ndei);
            }
            else if (string.IsNullOrEmpty(propertyName))
            {
                // 对象整体错误变化：全量重算
                foreach (var name in this.GetPropertyNames())
                {
                    this.SetPropertyErrorCount(name, HasPropertyErrors(ndei, name));
                }
            }
            else
            {
                this.SetPropertyErrorCount(propertyName, HasPropertyErrors(ndei, propertyName));
            }

            this.UpdateHasErrorsCore();
        }

        /// <summary>
        /// 首次调用时全量初始化错误计数。
        /// </summary>
        /// <param name="dei">数据错误信息实例。</param>
        private void InitializeErrorCounts(IDataErrorInfo dei)
        {
            foreach (var name in this.GetPropertyNames())
            {
                this.SetPropertyErrorCount(name, !string.IsNullOrEmpty(dei[name]));
            }

            this.errorCountsInitialized = true;
        }

        /// <summary>
        /// 首次调用时全量初始化错误计数。
        /// </summary>
        /// <param name="ndei">数据错误通知实例。</param>
        private void InitializeErrorCounts(INotifyDataErrorInfo ndei)
        {
            foreach (var name in this.GetPropertyNames())
            {
                this.SetPropertyErrorCount(name, HasPropertyErrors(ndei, name));
            }

            this.errorCountsInitialized = true;
        }

        /// <summary>
        /// 获取本 Tab 中所有属性的名称（保持原有顺序）。
        /// </summary>
        /// <returns>属性名序列。</returns>
        private IEnumerable<string> GetPropertyNames()
        {
            foreach (var g in this.Groups)
            {
                foreach (var p in g.Properties)
                {
                    yield return p.PropertyName;
                }
            }
        }

        /// <summary>
        /// 判断指定属性是否包含错误（对 null 结果安全）。
        /// </summary>
        /// <param name="ndei">数据错误通知实例。</param>
        /// <param name="propertyName">属性名。</param>
        /// <returns><c>true</c> 表示有错误。</returns>
        private static bool HasPropertyErrors(INotifyDataErrorInfo ndei, string propertyName)
        {
            var errors = ndei.GetErrors(propertyName);
            if (errors == null)
            {
                return false;
            }

            foreach (var error in errors)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 更新指定属性的错误状态（绝对状态：有错则记录，无错则移除）。<br/>
        /// 注意：调用方传入的是绝对布尔状态（每次全量重算），而不是增量；
        /// 此前按"有错 +1、无错 -1"增量处理会导致同一错误状态被重复累加
        /// （TargetUpdated 每次目标更新都会上报一次），清除一次后计数仍 >0，HasErrors 永久卡在 true。
        /// </summary>
        /// <param name="propertyName">属性名。</param>
        /// <param name="hasErrors">是否处于错误状态。</param>
        private void SetPropertyErrorCount(string? propertyName, bool hasErrors)
        {
            if (propertyName == null)
            {
                return;
            }

            if (hasErrors)
            {
                this.propertyErrorCounts[propertyName] = true;
            }
            else
            {
                this.propertyErrorCounts.Remove(propertyName);
            }
        }

        /// <summary>
        /// 根据错误状态字典更新 HasErrors（仅在变化时触发属性变更通知）。
        /// </summary>
        private void UpdateHasErrorsCore()
        {
            bool hasErrors = this.propertyErrorCounts.Count > 0;

            if (hasErrors != this.hasErrors)
            {
                this.HasErrors = hasErrors;
            }
        }

        /// <summary>
        /// Sort groups by <seealso cref="Group.GroupSortIndex"/>
        /// </summary>
        /// <returns></returns>
        public Tab SortGroups()
        {
            this.Groups = this.Groups.OrderBy(x => x.GroupSortIndex ?? 0).ToList();
            return this;
        }
    }
}
