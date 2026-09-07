#region Copyright information
// <copyright file="ParentChangedNotifier.cs">
//     Licensed under Microsoft Public License (Ms-PL)
//     https://github.com/Snet.Windows.Core.localize.core/Snet.Windows.Core.localize.core/blob/master/LICENSE
// </copyright>
// <author>Uwe Mayer</author>
#endregion

namespace Snet.Windows.Core.localize.core.Base
{
    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Data;
    #endregion

    /// <summary>
    /// A class that helps listening to changes on the Parent property of FrameworkElement objects.
    /// </summary>
    public class ParentChangedNotifier : DependencyObject, IDisposable
    {
        #region Parent property
        /// <summary>
        /// An attached property that will take over control of change notification.
        /// </summary>
        public static DependencyProperty ParentProperty = DependencyProperty.RegisterAttached("Parent", typeof(DependencyObject), typeof(ParentChangedNotifier), new PropertyMetadata(ParentChanged));

        /// <summary>
        /// Get method for the attached property.
        /// </summary>
        /// <param name="element">The target FrameworkElement object.</param>
        /// <returns>The target's parent FrameworkElement object.</returns>
        public static FrameworkElement GetParent(FrameworkElement element)
        {
            return element.GetValueSync<FrameworkElement>(ParentProperty);
        }

        /// <summary>
        /// Set method for the attached property.
        /// </summary>
        /// <param name="element">The target FrameworkElement object.</param>
        /// <param name="value">The target's parent FrameworkElement object.</param>
        public static void SetParent(FrameworkElement element, FrameworkElement value)
        {
            element.SetValueSync(ParentProperty, value);
        }
        #endregion

        #region ParentChanged callback
        /// <summary>
        /// The callback for changes of the attached Parent property.
        /// </summary>
        /// <param name="obj">The sender.</param>
        /// <param name="args">The argument.</param>
        private static void ParentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is FrameworkElement notifier)
            {
                // Direct dictionary lookup (O(1)) instead of a linear Keys.SingleOrDefault scan.
                if (OnParentChangedList.TryGetValue(notifier, out var actions))
                {
                    List<Action> list;
                    lock (actions)
                    {
                        list = new List<Action>(actions);
                    }
                    foreach (var OnParentChanged in list)
                        OnParentChanged();
                }
            }
        }
        #endregion

        /// <summary>
        /// A static list of actions that should be performed on parent change events.
        /// <para>- Entries are added by each call of the constructor.</para>
        /// <para>- All elements are called by the parent changed callback with the particular sender as the key.</para>
        /// </summary>
        private static readonly ConditionalWeakTable<DependencyObject, List<Action>> OnParentChangedList = new();

        /// <summary>
        /// The element this notifier is bound to. Needed to release the binding and Action entry.
        /// </summary>
        private WeakReference element = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="element">The element whose Parent property should be listened to.</param>
        /// <param name="onParentChanged">The action that will be performed upon change events.</param>
        public ParentChangedNotifier(FrameworkElement element, Action onParentChanged)
        {
            this.element = new WeakReference(element);

            if (onParentChanged != null)
            {
                // Key the list directly by the element object - O(1) add/lookup.
                var actions = OnParentChangedList.GetValue(element, static _ => new List<Action>());
                lock (actions)
                {
                    actions.Add(onParentChanged);
                }
            }

            // 元素尚未挂载（ContextMenu、未初始化的 DataGrid 列头等）时直接注册
            // FindAncestor 绑定会因找不到父级而输出绑定错误日志。改为等待 Loaded
            // 后再注册：元素挂载后父级已确定，绑定一次成功，错误日志消除。
            // ContextMenu 不在逻辑/可视树中，永远不会触发 Loaded，因此也不会注册绑定，
            // 其本地化上下文由宿主（PlacementTarget）提供，不受影响。
            if (element.IsLoaded)
            {
                if (element.CheckAccess())
                    SetBinding();
                else
                    element.Dispatcher.Invoke(new Action(SetBinding));
            }
            else
            {
                element.Loaded += Element_Loaded;
            }
        }

        private void Element_Loaded(object sender, RoutedEventArgs e)
        {
            if (element != null && element.IsAlive && element.Target is FrameworkElement frameworkElement)
            {
                frameworkElement.Loaded -= Element_Loaded;
                SetBinding();
            }
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~ParentChangedNotifier()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes all used resources of the instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        /// <param name="isDisposing">
        /// <see langword="true" /> if calls from Dispose() method.
        /// <see langword="false" /> if calls from finalizer.
        /// </param>
        protected virtual void Dispose(bool isDisposing)
        {
            var weakElement = element;

            // Guard against double dispose (element is set to null after the first one).
            if (weakElement == null)
                return;

            var weakElementReference = weakElement.Target;

            if (weakElementReference is DependencyObject key && OnParentChangedList.TryGetValue(key, out var list))
            {
                lock (list)
                {
                    list.Clear();
                }
                OnParentChangedList.Remove(key);
            }

            if (isDisposing)
            {
                if (weakElementReference == null || !weakElement.IsAlive)
                {
                    element = null;
                    return;
                }

                try
                {
                    ((FrameworkElement)weakElementReference).ClearValue(ParentProperty);
                }
                finally
                {
                    element = null;
                }
            }
        }

        private void SetBinding()
        {
            if (element?.Target is not FrameworkElement frameworkElement)
            {
                return;
            }

            var binding = new Binding("Parent")
            {
                RelativeSource = new RelativeSource()
                {
                    Mode = RelativeSourceMode.FindAncestor,
                    AncestorType = typeof(FrameworkElement),
                    AncestorLevel = 1
                },
                // 元素尚未挂载（ContextMenu、未初始化的 Header 等）时 FindAncestor 找不到源，
                // 设置 FallbackValue 使绑定失败时静默回退为 null，避免 WPF 输出绑定错误日志；
                // 元素挂载后绑定会自动重新解析，ParentChanged 通知仍能正常触发。
                FallbackValue = null
            };
            BindingOperations.SetBinding(frameworkElement, ParentProperty, binding);
        }
    }
}
