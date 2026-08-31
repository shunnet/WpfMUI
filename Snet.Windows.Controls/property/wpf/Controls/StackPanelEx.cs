// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StackPanelEx.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Represents a stack panel that counts the number of visible children.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Represents a stack panel that counts the number of visible children.
    /// </summary>
    public class StackPanelEx : StackPanel
    {
        /// <summary>
        /// Identifies the <see cref="VisibleChildrenCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VisibleChildrenCountProperty = DependencyProperty.Register(
            nameof(VisibleChildrenCount),
            typeof(int),
            typeof(StackPanelEx),
            new UIPropertyMetadata(-1));

        /// <summary>
        /// 当前可见的子元素集合（增量维护，避免每次 ArrangeOverride 全量遍历）。
        /// </summary>
        private readonly HashSet<UIElement> visibleChildren = new HashSet<UIElement>();

        /// <summary>
        /// 子元素 Visibility 变化的处理器委托（AddValueChanged/RemoveValueChanged 需要同一实例）。
        /// </summary>
        private readonly EventHandler visibilityChangedHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="StackPanelEx" /> class.
        /// </summary>
        public StackPanelEx()
        {
            this.visibilityChangedHandler = this.OnChildVisibilityChanged;
        }

        /// <summary>
        /// Gets the number of visible children.
        /// </summary>
        /// <value>The visible children count.</value>
        public int VisibleChildrenCount
        {
            get
            {
                return (int)this.GetValue(VisibleChildrenCountProperty);
            }

            private set
            {
                this.SetValue(VisibleChildrenCountProperty, value);
            }
        }

        /// <summary>
        /// 子元素集合变化时挂接/移除 Visibility 钩子，并增量更新计数。
        /// </summary>
        /// <param name="visualAdded">新增的子元素。</param>
        /// <param name="visualRemoved">移除的子元素。</param>
        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            if (visualAdded is UIElement added)
            {
                this.AttachVisibilityHook(added);
                if (added.Visibility == Visibility.Visible && this.visibleChildren.Add(added))
                {
                    this.VisibleChildrenCount = this.visibleChildren.Count;
                }
            }

            if (visualRemoved is UIElement removed)
            {
                this.DetachVisibilityHook(removed);
                if (this.visibleChildren.Remove(removed))
                {
                    this.VisibleChildrenCount = this.visibleChildren.Count;
                }
            }
        }

        /// <summary>
        /// Arranges the content of a <see cref="T:System.Windows.Controls.StackPanel" /> element.
        /// </summary>
        /// <param name="arrangeSize">The <see cref="T:System.Windows.Size" /> that this element should use to arrange its child elements.</param>
        /// <returns>
        /// The <see cref="T:System.Windows.Size" /> that represents the arranged size of this <see cref="T:System.Windows.Controls.StackPanel" /> element and its child elements.
        /// </returns>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            // 计数已由子元素 Visibility 变化事件增量维护，这里仅做 O(1) 兜底同步
            this.VisibleChildrenCount = this.visibleChildren.Count;
            return base.ArrangeOverride(arrangeSize);
        }

        /// <summary>
        /// 挂接子元素的 Visibility 属性变更钩子。
        /// </summary>
        /// <param name="child">子元素。</param>
        private void AttachVisibilityHook(UIElement child)
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(UIElement.VisibilityProperty, typeof(UIElement));
            if (descriptor != null)
            {
                descriptor.AddValueChanged(child, this.visibilityChangedHandler);
            }
        }

        /// <summary>
        /// 移除子元素的 Visibility 属性变更钩子。
        /// </summary>
        /// <param name="child">子元素。</param>
        private void DetachVisibilityHook(UIElement child)
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(UIElement.VisibilityProperty, typeof(UIElement));
            if (descriptor != null)
            {
                descriptor.RemoveValueChanged(child, this.visibilityChangedHandler);
            }
        }

        /// <summary>
        /// 子元素 Visibility 变化时增量更新计数。
        /// </summary>
        /// <param name="sender">子元素。</param>
        /// <param name="e">事件参数。</param>
        private void OnChildVisibilityChanged(object sender, EventArgs e)
        {
            var child = sender as UIElement;
            if (child == null)
            {
                return;
            }

            bool isVisible = child.Visibility == Visibility.Visible;
            if (isVisible)
            {
                if (this.visibleChildren.Add(child))
                {
                    this.VisibleChildrenCount = this.visibleChildren.Count;
                }
            }
            else if (this.visibleChildren.Remove(child))
            {
                this.VisibleChildrenCount = this.visibleChildren.Count;
            }
        }
    }
}
