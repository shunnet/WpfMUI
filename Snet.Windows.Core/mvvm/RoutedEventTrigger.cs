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

        // 保存事件处理器引用，用于后续解绑
        private RoutedEventHandler _routedEventHandler;

        /// <summary>
        /// 返回事件名字符串，用于行为系统识别事件名称（必须实现）
        /// </summary>
        protected override string GetEventName()
        {
            if (RoutedEventName == null)
                throw new InvalidOperationException("RoutedEventTrigger 的 RoutedEvent 属性未设置。");

            return RoutedEventName.Name;
        }

        /// <summary>
        /// 当行为附加到控件上时触发，绑定路由事件处理器
        /// </summary>
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
            eventSource.AddHandler(RoutedEventName, _routedEventHandler);
        }

        /// <summary>
        /// 当行为被移除时触发，解绑路由事件处理器
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();

            var eventSource = ResolveEventSource();
            if (eventSource != null && _routedEventHandler != null)
            {
                eventSource.RemoveHandler(RoutedEventName, _routedEventHandler);
            }
        }

        /// <summary>
        /// 当路由事件触发时，调用行为系统的 OnEvent 触发动作链（如命令）
        /// </summary>
        private void OnRoutedEvent(object sender, RoutedEventArgs args)
        {
            OnEvent(args); // 触发行为链（InvokeCommandAction 等）
        }

        /// <summary>
        /// 解析附加事件源（支持 FrameworkElement、FrameworkContentElement 或其行为）
        /// </summary>
        private dynamic ResolveEventSource()
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
                if (((IAttachedObject)behavior).AssociatedObject is FrameworkElement fe2)
                    return fe2;

                if (((IAttachedObject)behavior).AssociatedObject is FrameworkContentElement fce2)
                    return fce2;
            }

            return null;
        }
    }
}
