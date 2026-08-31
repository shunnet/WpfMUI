using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Snet.Windows.Controls.drag
{
    /// <summary>
    /// 拖拽控件过度动画  使用一个特效控件，在拖动过程中显示，拖动完成隐藏清空   2022.09.15
    /// </summary>
    public class DragControlsExcessiveAnimate
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="Windows">窗体</param>
        /// <param name="LlayoutContainer">容器：让控件在这里面拖动</param>
        /// <param name="HeightOffset">高度偏移</param>
        /// <param name="WidthOffset">宽度偏移</param>
        public DragControlsExcessiveAnimate(FrameworkElement Windows, object LlayoutContainer, double HeightOffset = 0, double WidthOffset = 0)
        {
            this.HeightOffset = HeightOffset;
            this.WidthOffset = WidthOffset;
            this.Windows = Windows;
            this.LlayoutContainer = LlayoutContainer;
            Windows.SizeChanged += Windwos_SizeChanged;
        }
        #region 私有字段
        /// <summary>
        /// 高度偏移
        /// 拖动过度时，已经显示了要拖动的控件[半透明状态]，鼠标默认是在中心位置，由于窗体样式原因，导致不在中心点位置，所以加上了偏移
        /// </summary>
        double HeightOffset { get; set; }
        /// <summary>
        /// 宽度偏移
        /// 拖动过度时，已经显示了要拖动的控件[半透明状态]，鼠标默认是在中心位置，由于窗体样式原因，导致不在中心点位置，所以加上了偏移
        /// </summary>
        double WidthOffset { get; set; }
        /// <summary>
        /// 界面上已经生成的控件，也就是从哪个控件上拖动的集合
        /// </summary>
        readonly List<FrameworkElement> ShowControlsList = new List<FrameworkElement>();
        /// <summary>
        /// 窗体
        /// </summary>
        readonly FrameworkElement Windows;
        /// <summary>
        /// 容器：让控件在这里面拖动
        /// </summary>
        readonly object LlayoutContainer;
        /// <summary>
        /// 鼠标是否按下
        /// </summary>
        bool IsMouseDown = false;
        /// <summary>
        /// 实时需要拖动的控件
        /// </summary>
        FrameworkElement ControlsObj;
        /// <summary>
        /// 拖拽大小与移动
        /// </summary>
        readonly DragControlsHelper dragControlsHelper = new DragControlsHelper();
        #endregion

        #region 方法
        /// <summary>
        /// 动态修改偏移量
        /// </summary>
        /// <param name="HeightOffset">高度偏移</param>
        /// <param name="WidthOffset">宽度偏移</param>
        public void DynamicUpdateOffset(double HeightOffset, double WidthOffset)
        {
            this.HeightOffset = HeightOffset;
            this.WidthOffset = WidthOffset;
        }
        /// <summary>
        /// 移动与拖拽大小添加
        /// </summary>
        /// <param name="Controls">控件</param>
        /// <param name="Window">窗体</param>
        /// <param name="Move">移动功能</param>
        /// <param name="DragSize">拖拽大小功能</param>
        public string MoveAndDragSizeInsert(FrameworkElement Controls, FrameworkElement Window, bool Move, bool DragSize)
        {
            //创建拖动与拖拽大小
            return dragControlsHelper.Insert(Controls, Window, Move, DragSize);
        }

        /// <summary>
        /// 移除拖拽大小与移动
        /// </summary>
        public void MoveAndDragSizeRemove(FrameworkElement Controls)
        {
            //创建拖动与拖拽大小
            dragControlsHelper.Remove(Controls);
        }
        /// <summary>
        ///  添加需要拖动的组件
        /// </summary>
        /// <param name="ControlsShow">界面上已经生成的控件</param>
        public void Insert(FrameworkElement ControlsShow)
        {
            if (!ShowControlsList.Contains(ControlsShow))  //不存在则添加
            {
                InsertEven(ControlsShow);
                ShowControlsList.Add(ControlsShow);
            }
        }
        /// <summary>
        /// 移除拖动
        /// </summary>
        /// <param name="ControlsShow">界面上已经生成的控件</param>
        public void Remove(FrameworkElement ControlsShow)
        {
            if (ShowControlsList.Contains(ControlsShow))
            {
                RemoveEven(ControlsShow);
                ShowControlsList.Remove(ControlsShow);  //直接移除
            }
        }
        /// <summary>
        /// 创建事件
        /// </summary>
        /// <param name="ControlsShow">界面上已经生成的控件</param>
        public void InsertEven(FrameworkElement ControlsShow)
        {
            //ControlsShow.PreviewMouseLeftButtonDown += delegate (object sender, MouseButtonEventArgs e) { ControlsShow_PreviewMouseLeftButtonDown(sender, e, ControlsObj); };
            //ControlsShow.PreviewMouseLeftButtonUp += delegate (object sender, MouseButtonEventArgs e) { ControlsShow_PreviewMouseLeftButtonUp(sender, e, ControlsObj); };
            //ControlsShow.PreviewMouseMove += delegate (object sender, MouseEventArgs e) { ControlsShow_PreviewMouseMove(sender, e, ControlsObj); };

            ControlsShow.PreviewMouseLeftButtonDown += ControlsShow_PreviewMouseLeftButtonDown;
            ControlsShow.PreviewMouseLeftButtonUp += ControlsShow_PreviewMouseLeftButtonUp;
            ControlsShow.PreviewMouseMove += ControlsShow_PreviewMouseMove;
            ControlsShow.LostMouseCapture += ControlsShow_LostMouseCapture;
        }
        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="ControlsShow">界面上已经生成的控件</param>
        public void RemoveEven(FrameworkElement ControlsShow)
        {
            ControlsShow.PreviewMouseLeftButtonDown -= ControlsShow_PreviewMouseLeftButtonDown;
            ControlsShow.PreviewMouseLeftButtonUp -= ControlsShow_PreviewMouseLeftButtonUp;
            ControlsShow.PreviewMouseMove -= ControlsShow_PreviewMouseMove;
            ControlsShow.LostMouseCapture -= ControlsShow_LostMouseCapture;
        }

        #endregion

        #region 委托回调事件

        /// <summary>
        /// 定义委托 提醒拖拽事件开始了，请传需要拖动的按钮对象
        /// </summary>
        /// <param name="ShowControl">在哪个控件上触发了拖拽</param>
        /// <returns>返回已经创建了新的控件对象  -   是否需要移动   -  是否需要拖拽大小</returns>
        public delegate (FrameworkElement NewControl, bool IsMove, bool IsDragSize) dragEvenTrigger(FrameworkElement ShowControl);
        /// <summary>
        /// 实现委托
        /// </summary>
        public dragEvenTrigger DragEvenTrigger;

        /// <summary>
        /// 按钮状态
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum ButtonState
        {
            /// <summary>
            /// 按下
            /// </summary>
            Down,
            /// <summary>
            /// 松开
            /// </summary>
            Up,
            /// <summary>
            /// 移动
            /// </summary>
            Move
        }

        /// <summary>
        /// 动作事件 告诉外部，按钮目前的状态
        /// </summary>
        public delegate void actionEvenTrigger(ButtonState state, Button obj);
        /// <summary>
        /// 实现委托
        /// </summary>
        public actionEvenTrigger ActionEvenTrigger;
        #endregion

        #region 执行事件

        //移动位置
        private void ControlsShow_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ControlsObj == null) return;
            if (IsMouseDown)
            {
                if (LlayoutContainer.GetType().Equals(typeof(Canvas)))
                {
                    Point pos = e.GetPosition(Windows);
                    Canvas.SetLeft(ControlsObj, (pos.X - ControlsObj.Width / 2) - WidthOffset);
                    Canvas.SetTop(ControlsObj, (pos.Y - ControlsObj.Height / 2) - HeightOffset);
                }
                else if (LlayoutContainer.GetType().Equals(typeof(Grid)))
                {
                    Point pos = e.GetPosition(Windows);
                    double Left = (pos.X - ControlsObj.Width / 2) - WidthOffset;
                    double Top = (pos.Y - ControlsObj.Height / 2) - HeightOffset;
                    double Right = Windows.ActualWidth - Left - ControlsObj.Width;
                    double Bottom = Windows.ActualHeight - Top - ControlsObj.Height;
                    ControlsObj.Margin = new Thickness(Left, Top, Right, Bottom);
                }
                ActionEvenTrigger(ButtonState.Move, sender as Button);
            }
        }

        /// <summary>
        /// 鼠标左键松开事件处理<br/>
        /// 停止拖动状态，释放鼠标捕获，启动控件渐隐消失效果，并通知外部按钮已松开
        /// </summary>
        private void ControlsShow_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsMouseDown = false;
            if (sender is UIElement uiElement)
            {
                uiElement.ReleaseMouseCapture();
            }
            if (ControlsObj == null) return;
            _ = ControlsVanish(ControlsObj);   //异步渐隐，不阻塞当前线程
            ControlsObj = null;
            ActionEvenTrigger(ButtonState.Up, sender as Button);
        }

        /// <summary>
        /// 鼠标捕获丢失事件处理。<br/>
        /// 捕获被其他窗口抢占（如 Alt+Tab 切换）时，结束拖动并通知外部按钮已松开。
        /// </summary>
        private void ControlsShow_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (!IsMouseDown) return;
            IsMouseDown = false;
            if (ControlsObj != null)
            {
                _ = ControlsVanish(ControlsObj);   //捕获被抢占也执行渐隐，避免残留
                ControlsObj = null;
            }
            ActionEvenTrigger(ButtonState.Up, sender as Button);
        }


        //当在已显示的控件左键点击后
        private void ControlsShow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (LlayoutContainer.GetType().Equals(typeof(Canvas)))
            {
                Canvas? layout = LlayoutContainer as Canvas;
                (FrameworkElement element, bool IsMove, bool IsDragSize) = DragEvenTrigger(sender as FrameworkElement);
                ControlsObj = element;
                if (!layout.Children.Contains(ControlsObj))
                {
                    IsMouseDown = true;
                    // 捕获鼠标：拖出源控件边界后继续跟随，直到左键松开
                    if (sender is UIElement source)
                    {
                        source.CaptureMouse();
                    }
                    Point Position = e.GetPosition(Windows);
                    ControlsObj.Opacity = 0.5;
                    Canvas.SetLeft(ControlsObj, Position.X - ControlsObj.Width / 2 - WidthOffset);
                    Canvas.SetTop(ControlsObj, Position.Y - ControlsObj.Height / 2 - HeightOffset);
                    layout.Children.Add(ControlsObj);
                }
            }
            else if (LlayoutContainer.GetType().Equals(typeof(Grid)))
            {
                Grid? layout = LlayoutContainer as Grid;
                (FrameworkElement element, bool IsMove, bool IsDragSize) = DragEvenTrigger(sender as FrameworkElement);
                ControlsObj = element;
                if (!layout.Children.Contains(ControlsObj))
                {
                    IsMouseDown = true;
                    // 捕获鼠标：拖出源控件边界后继续跟随，直到左键松开
                    if (sender is UIElement source)
                    {
                        source.CaptureMouse();
                    }
                    Point Position = e.GetPosition(Windows);
                    ControlsObj.Opacity = 0.5;

                    double Left = Position.X - ControlsObj.Width / 2 - WidthOffset;
                    double Top = Position.Y - ControlsObj.Height / 2 - HeightOffset;
                    double Right = Windows.ActualWidth - Left - ControlsObj.Width;
                    double Bottom = Windows.ActualHeight - Top - ControlsObj.Height;
                    ControlsObj.Margin = new Thickness(Left, Top, Right, Bottom);
                    layout.Children.Add(ControlsObj);
                }
            }
            ActionEvenTrigger(ButtonState.Down, sender as Button);
        }


        //当窗体大小改变，布局容器也要跟着改变大小
        private void Windwos_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FrameworkElement? window = sender as FrameworkElement;
            if (LlayoutContainer.GetType().Equals(typeof(Canvas)))
            {
                Canvas? layout = LlayoutContainer as Canvas;
                layout.Width = window.ActualWidth;
                layout.Height = window.ActualHeight;
            }
            else if (LlayoutContainer.GetType().Equals(typeof(Grid)))
            {
                Grid layout = LlayoutContainer as Grid;
                layout.Width = window.ActualWidth;
                layout.Height = window.ActualHeight;
            }
        }


        /// <summary>
        /// 控件渐隐消失效果<br/>
        /// 使用 DoubleAnimation 动画替代旧的 100 次 Dispatcher+Task.Delay 循环模拟渐隐，避免频繁调度开销<br/>
        /// 动画完成后从容器中移除控件，外部可 await 等待动画完成
        /// </summary>
        /// <param name="element">需要执行消失动画的控件对象</param>
        async Task ControlsVanish(object element)
        {
            if (element is not FrameworkElement fe)
                return;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                // 没有 UI 环境时直接移除控件
                RemoveElement(fe);
                return;
            }

            try
            {
                // 在 UI 线程上启动渐隐动画，并返回动画完成信号
                Task animationCompleted = await dispatcher.InvokeAsync(() =>
                {
                    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var animation = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(300));
                    animation.Completed += (_, _) => tcs.TrySetResult();
                    fe.BeginAnimation(UIElement.OpacityProperty, animation);
                    return tcs.Task;
                });

                // 等待动画完成
                await animationCompleted;

                // 动画完成后在 UI 线程上从容器中移除控件
                await dispatcher.InvokeAsync(() => RemoveElement(fe));
            }
            catch (Exception ex)
            {
                // 动画启动或等待失败时直接移除控件，避免残留
                RemoveElement(fe);
            }
        }

        /// <summary>
        /// 从容器中移除控件
        /// </summary>
        /// <param name="fe">要移除的控件</param>
        private void RemoveElement(FrameworkElement fe)
        {
            if (LlayoutContainer is Canvas canvas)
            {
                canvas.Children.Remove(fe);
            }
            else if (LlayoutContainer is Grid grid)
            {
                grid.Children.Remove(fe);
            }
        }
        #endregion

    }
}
