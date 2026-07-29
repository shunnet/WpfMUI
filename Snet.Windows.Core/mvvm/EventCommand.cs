using Microsoft.Xaml.Behaviors;
using Snet.Windows.Core.data;
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
        /// 命令参数。
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                nameof(CommandParameter),
                typeof(object),
                typeof(EventCommand),
                new PropertyMetadata(null));


        /// <summary>
        /// 要执行的 ICommand。
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(EventCommand),
                new PropertyMetadata(null));


        /// <summary>
        /// 是否启用 EventCommandArgs 包装模式。
        /// </summary>
        public static readonly DependencyProperty UseEventCommandArgsProperty =
            DependencyProperty.Register(
                nameof(UseEventCommandArgs),
                typeof(bool),
                typeof(EventCommand),
                new PropertyMetadata(false));

        #endregion

        #region 属性封装

        /// <summary>
        /// 获取或设置要执行的命令。
        /// </summary>
        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }


        /// <summary>
        /// 获取或设置命令参数。
        /// </summary>
        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }


        /// <summary>
        /// 获取或设置是否使用 EventCommandArgs 包装事件参数。
        /// </summary>
        public bool UseEventCommandArgs
        {
            get => (bool)GetValue(UseEventCommandArgsProperty);
            set => SetValue(UseEventCommandArgsProperty, value);
        }

        #endregion

        /// <summary>
        /// EventTrigger 触发时执行。
        /// </summary>
        /// <param name="parameter">触发事件传入的参数</param>
        protected override void Invoke(object? parameter)
        {
            if (Command == null)
                return;


            object? commandParameter;


            if (UseEventCommandArgs)
            {
                commandParameter = new EventCommandArgs
                {
                    // WPF 路由事件优先使用 Source
                    // 普通事件使用绑定控件本身
                    Source =
                        parameter is RoutedEventArgs routedArgs
                            ? routedArgs.Source
                            : AssociatedObject,


                    // WPF 路由事件的最初触发对象
                    OriginalSource =
                        parameter is RoutedEventArgs routedArgs2
                            ? routedArgs2.OriginalSource
                            : AssociatedObject,


                    // 保存原始事件参数
                    EventArgs = parameter,


                    // 保存 XAML 指定参数
                    Parameter = CommandParameter
                };
            }
            else
            {
                // 保持旧版本行为
                //
                // 例如：
                // CommandParameter="{Binding Item}"
                //
                // VM 收到 Item
                //
                // 未设置 CommandParameter：
                // VM 收到 EventArgs
                commandParameter = CommandParameter ?? parameter;
            }


            if (Command.CanExecute(commandParameter))
            {
                Command.Execute(commandParameter);
            }
        }



    }
}