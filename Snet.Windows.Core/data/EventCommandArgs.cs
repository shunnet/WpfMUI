namespace Snet.Windows.Core.data
{
    /// <summary>
    /// EventCommand 事件命令参数包装类。
    /// </summary>
    public class EventCommandArgs
    {
        /// <summary>
        /// 获取或设置事件源对象。
        /// </summary>
        public object? Source { get; set; }

        /// <summary>
        /// 获取或设置原始事件触发对象。
        /// </summary>
        public object? OriginalSource { get; set; }

        /// <summary>
        /// 获取或设置原始事件参数。
        /// </summary>
        public object? EventArgs { get; set; }

        /// <summary>
        /// 获取或设置 XAML 中 CommandParameter 指定的参数。
        /// </summary>
        public object? Parameter { get; set; }
    }
}