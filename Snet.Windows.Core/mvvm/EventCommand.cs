using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace Snet.Windows.Core.mvvm
{
    /// <summary>
    /// 通用事件命令绑定行为（适用于 Microsoft.Xaml.Behaviors.Wpf）。
    /// 可将任意触发器事件绑定到 ViewModel 中的 ICommand 实现。
    /// </summary>
    public class EventCommand : TriggerAction<DependencyObject>
    {

        #region 依赖属性定义

        /// <summary>
        /// 绑定命令参数。如果为 null，则使用事件参数。
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(EventCommand), new PropertyMetadata(null));

        /// <summary>
        /// 要执行的命令（实现了 ICommand 的 ViewModel 方法）。
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(EventCommand), new PropertyMetadata(null));

        #endregion

        #region 属性封装

        /// <summary>
        /// 要执行的命令。
        /// </summary>
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// 命令的参数。如果未设置，则默认使用事件传入参数。
        /// </summary>
        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        #endregion

        /// <summary>
        /// 当触发器触发时执行命令。
        /// </summary>
        /// <param name="parameter">触发事件传入的参数</param>
        protected override void Invoke(object parameter)
        {
            var cmd = Command;

            if (cmd == null)
                return;

            var commandParameter = CommandParameter ?? parameter;

            if (cmd.CanExecute(commandParameter))
            {
                cmd.Execute(commandParameter);
            }
        }
    }
}