using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace Snet.Windows.Core.mvvm
{
    /// <summary>
    /// 路由事件触发器类（用于 WPF 交互行为系统）
    /// <para>支持将任意 RoutedEvent 转换为可触发命令的行为触发器</para>
    /// <para>兼容 Microsoft.Xaml.Behaviors.Wpf</para>
    /// </summary>
    public class RoutedEventTrigger : EventTriggerBase<DependencyObject>
    {
        /// <summary>
        /// 要监听的路由事件（例如：Button.ClickEvent / TreeViewItem.ExpandedEvent）
        /// </summary>
        public RoutedEvent RoutedEventName { get; set; }

        /// <summary>
        /// 保存事件处理器引用，用于后续解绑
        /// </summary>
        private RoutedEventHandler? _routedEventHandler;

        /// <summary>
        /// 返回事件名字符串，用于行为系统识别事件名称（必须实现）
        /// </summary>
        /// <returns>路由事件的名称字符串</returns>
        /// <exception cref="InvalidOperationException">当 RoutedEventName 未设置时抛出</exception>
        protected override string GetEventName()
        {
            if (RoutedEventName == null)
                throw new InvalidOperationException("RoutedEventTrigger 的 RoutedEvent 属性未设置。");

            return RoutedEventName.Name;
        }

        /// <summary>
        /// 当行为附加到控件上时触发，绑定路由事件处理器。<br/>
        /// 解析实际的事件源（支持 FrameworkElement、FrameworkContentElement 以及 Behavior 包装对象），
        /// 并为其注册路由事件处理器。
        /// </summary>
        /// <exception cref="InvalidOperationException">当无法解析事件源或 RoutedEventName 未设置时抛出</exception>
        protected override void OnAttached()
        {
            base.OnAttached();

            // 解析要绑定事件的实际控件对象（支持 FrameworkElement 和 FrameworkContentElement）
            var eventSource = ResolveEventSource();

            if (eventSource == null)
            {
                throw new InvalidOperationException("RoutedEventTrigger 只能附加到 FrameworkElement 或 FrameworkContentElement 或其行为（Behavior）上。");
            }

            if (RoutedEventName == null)
            {
                throw new InvalidOperationException("请设置 RoutedEventTrigger 的 RoutedEvent 属性。");
            }

            // 注册事件处理器
            _routedEventHandler = new RoutedEventHandler(OnRoutedEvent);
            AddRoutedEventHandler(eventSource, RoutedEventName, _routedEventHandler);
        }

        /// <summary>
        /// 当行为被移除时触发，解绑路由事件处理器以防止内存泄漏
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();

            var eventSource = ResolveEventSource();
            if (eventSource != null && _routedEventHandler != null)
            {
                RemoveRoutedEventHandler(eventSource, RoutedEventName, _routedEventHandler);
                _routedEventHandler = null;
            }
        }

        /// <summary>
        /// 当路由事件触发时，调用行为系统的 OnEvent 触发动作链（如命令绑定）
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="args">路由事件参数</param>
        private void OnRoutedEvent(object sender, RoutedEventArgs args)
        {
            OnEvent(args);
        }

        /// <summary>
        /// 解析附加事件源对象。<br/>
        /// 依次检查 AssociatedObject 是否为 FrameworkElement、FrameworkContentElement，
        /// 或者是 Behavior 包装器（进一步获取其绑定的实际对象）。<br/>
        /// 返回 DependencyObject 类型，避免使用 dynamic 带来的 DLR 性能开销。
        /// </summary>
        /// <returns>解析后的事件源对象；若不支持则返回 null</returns>
        private DependencyObject? ResolveEventSource()
        {
            // 如果附加到的是 FrameworkElement，直接返回
            if (AssociatedObject is FrameworkElement fe)
                return fe;

            // 如果附加到的是 FrameworkContentElement，直接返回
            if (AssociatedObject is FrameworkContentElement fce)
                return fce;

            // 如果附加到的是 Behavior，则查找其绑定的对象
            if (AssociatedObject is Behavior behavior)
            {
                var associated = ((IAttachedObject)behavior).AssociatedObject;

                if (associated is FrameworkElement fe2)
                    return fe2;

                if (associated is FrameworkContentElement fce2)
                    return fce2;
            }

            return null;
        }

        /// <summary>
        /// 为目标对象注册路由事件处理器（支持 UIElement 和 ContentElement）。<br/>
        /// 替代 dynamic 调用，避免 DLR 开销，提升运行时性能。
        /// </summary>
        /// <param name="target">事件源对象</param>
        /// <param name="routedEvent">要注册的路由事件</param>
        /// <param name="handler">事件处理器</param>
        private static void AddRoutedEventHandler(DependencyObject target, RoutedEvent routedEvent, RoutedEventHandler handler)
        {
            if (target is UIElement uiElement)
                uiElement.AddHandler(routedEvent, handler);
            else if (target is ContentElement contentElement)
                contentElement.AddHandler(routedEvent, handler);
        }

        /// <summary>
        /// 从目标对象移除路由事件处理器（支持 UIElement 和 ContentElement）。<br/>
        /// 替代 dynamic 调用，避免 DLR 开销，提升运行时性能。
        /// </summary>
        /// <param name="target">事件源对象</param>
        /// <param name="routedEvent">要移除的路由事件</param>
        /// <param name="handler">事件处理器</param>
        private static void RemoveRoutedEventHandler(DependencyObject target, RoutedEvent routedEvent, RoutedEventHandler handler)
        {
            if (target is UIElement uiElement)
                uiElement.RemoveHandler(routedEvent, handler);
            else if (target is ContentElement contentElement)
                contentElement.RemoveHandler(routedEvent, handler);
        }
    }
}
