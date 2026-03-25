using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        /// 停止拖动状态，启动控件渐隐消失效果，并通知外部按钮已松开
        /// </summary>
        private void ControlsShow_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsMouseDown = false;
            if (ControlsObj == null) return;
            ControlsVanish(ControlsObj);
            ControlsObj = null;
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
                    Point Position = e.GetPosition(Windows);
                    ControlsObj.Opacity = 0.5;
                    Canvas.SetLeft(ControlsObj, Position.X - ControlsObj.Width / 2);
                    Canvas.SetTop(ControlsObj, Position.Y - ControlsObj.Height / 2);
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
                    Point Position = e.GetPosition(Windows);
                    ControlsObj.Opacity = 0.5;

                    double Left = Position.X - ControlsObj.Width / 2;
                    double Top = Position.Y - ControlsObj.Height / 2;
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
        /// 使用异步方式逐步降低控件透明度，消失后从容器中移除<br/>
        /// 替代旧的 new Thread + Task.Delay(1).Wait() 实现，避免线程阻塞和资源浪费
        /// </summary>
        /// <param name="element">需要执行消失动画的控件对象</param>
        async void ControlsVanish(object element)
        {
            if (element is not FrameworkElement fe)
                return;

            // 逐步降低透明度实现渐隐效果
            for (double opacity = 1.0; opacity > 0; opacity -= 0.01)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    fe.Opacity = opacity;
                });
                await Task.Delay(1);
            }

            // 透明度归零后，从容器中移除控件
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (LlayoutContainer is Canvas canvas)
                {
                    canvas.Children.Remove(fe);
                }
                else if (LlayoutContainer is Grid grid)
                {
                    grid.Children.Remove(fe);
                }
            });
        }
        #endregion

    }
}
