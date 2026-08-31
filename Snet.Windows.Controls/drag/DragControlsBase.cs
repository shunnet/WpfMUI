using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace Snet.Windows.Controls.drag
{
    /// <summary>
    /// 控件拖动基类
    /// </summary>
    public class DragControlsBase : Adorner
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="Controls">要拖动的控件</param>
        /// <param name="LlayoutContainer">窗体的布局容器</param>
        /// <param name="Move">移动（含中心移动圈）</param>
        /// <param name="DragSize">拖拽大小（四周 8 个缩放点）</param>
        /// <param name="Rotate">旋转（顶部旋转圈）</param>
        public DragControlsBase(UIElement Controls, FrameworkElement LlayoutContainer, bool Move, bool DragSize, bool Rotate = false) : base(Controls)
        {
            this.Controls = Controls;
            this.LlayoutContainer = LlayoutContainer;
            // 三类装饰点互相独立：移动圈 / 缩放点 / 旋转圈
            if (Move || DragSize || Rotate)
            {
                InitLayout();
            }
            if (DragSize)
            {
                InitDragDelta();      //四周 8 个缩放点（蓝色）
            }
            if (Move)
            {
                InitMove();           //控件本体拖动
                InitCentreThumb();    //中心移动圈（红色）
            }
            if (Rotate)
            {
                InitRotateThumb();    //顶部旋转圈（红色）
            }
            StyleThumbs();
            // 控件已带旋转（如 JSON 加载）时同步装饰器
            if (Controls is FrameworkElement fe && fe.RenderTransform is RotateTransform rotate)
            {
                SetRotation(rotate.Angle);
            }
        }
        /// <summary>
        /// 容器边框颜色
        /// </summary>
        public SolidColorBrush BorderColor = new SolidColorBrush(Colors.Green);
        /// <summary>
        /// 容器边框线径
        /// </summary>
        public Thickness BorderWireDiameter = new Thickness(1);
        /// <summary>
        /// 容器边框透明度
        /// </summary>
        public double BorderOpacity = 0;
        /// <summary>
        /// 拖拽装饰器的内圈颜色（中心点 / 旋转圈 / 连接线）
        /// </summary>
        public SolidColorBrush ThumbInnerColor = new SolidColorBrush(new Color { A = 0xFF, R = 0xF4, G = 0x43, B = 0x36 });
        /// <summary>
        /// 拖拽装饰器的外圈颜色（中心点 / 旋转圈 / 连接线）
        /// </summary>
        public SolidColorBrush ThumbOuterColor = new SolidColorBrush(new Color { A = 0xFF, R = 0xFF, G = 0x8A, B = 0x80 });
        /// <summary>
        /// 四周调整圈内圈颜色（8 个缩放点，默认蓝色）
        /// </summary>
        public SolidColorBrush SurroundInnerColor = new SolidColorBrush(new Color { A = 0xFF, R = 0x21, G = 0x96, B = 0xF3 });
        /// <summary>
        /// 四周调整圈外圈颜色（8 个缩放点，默认浅蓝）
        /// </summary>
        public SolidColorBrush SurroundOuterColor = new SolidColorBrush(new Color { A = 0xFF, R = 0x90, G = 0xCA, B = 0xF9 });
        /// <summary>
        /// 装饰器线径
        /// </summary>
        public double ThumbWireDiameter = 1;
        /// <summary>
        /// 装饰器透明度
        /// </summary>
        public double ThumbOpacity = 0.6;
        /// <summary>
        /// 拖拽最小的宽
        /// </summary>
        public double MinWidths = 100;
        /// <summary>
        /// 拖拽最小的高
        /// </summary>
        public double MinHeights = 100;
        /// <summary>
        /// 拖拽最大的宽
        /// </summary>
        public double MaxWidths = 500;
        /// <summary>
        /// 拖拽最大的高
        /// </summary>
        public double MaxHeights = 500;

        #region 私有字段
        /// <summary>
        /// 4条边
        /// </summary>
        Thumb LeftThumb, TopThumb, RightThumb, BottomThumb;
        /// <summary>
        /// 4个角
        /// </summary>
        Thumb LefTopThumb, RightTopThumb, RightBottomThumb, LeftbottomThumb;
        /// <summary>
        /// 中间（拖动整个控件）
        /// </summary>
        Thumb CentreThumb;
        /// <summary>
        /// 旋转手柄（位于控件上方，拖动绕中心旋转）
        /// </summary>
        Thumb RotateThumb;

        /// <summary>
        /// 旋转手柄中心到控件上边缘的距离
        /// </summary>
        double RotateThumbDistance = 26;

        /// <summary>
        /// 当前旋转角度（度，绕中心顺时针；0 表示不旋转）
        /// </summary>
        public double Angle { get; private set; }
        /// <summary>
        /// 布局容器，如果不使用布局容器，则需要给上述8个控件布局，实现和Grid布局定位是一样的，会比较繁琐且意义不大。
        /// </summary>
        Grid Llayout;

        /// <summary>
        /// 要拖动的控件
        /// </summary>
        readonly UIElement Controls;
        /// <summary>
        /// 窗体的布局容器
        /// </summary>
        readonly FrameworkElement LlayoutContainer;
        /// <summary>
        /// 鼠标是否按下
        /// </summary>
        bool IsMouseDown = false;
        /// <summary>
        /// 鼠标按下的位置
        /// </summary>
        Point MouseDownPosition;
        /// <summary>
        /// 鼠标按下控件的Margin
        /// </summary>
        Thickness MouseDownMargin;
        #endregion

        #region 重写方法
        /// <summary>
        /// 获取指定索引位置的可视子元素（返回布局容器 Grid）。
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            return Llayout!;
        }
        /// <summary>
        /// 获取可视子元素数量（有布局容器时返回 1，否则 0）
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                return Llayout == null ? 0 : 1;
            }
        }
        /// <summary>
        /// 重写布局排列方法。<br/>
        /// 将装饰器布局容器安排在控件周围，偏移半个拖拽点大小以使拖拽点居中在边缘。
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            //直接给容器布局，容器内部的装饰器会自动布局。
            if (Llayout != null)
            {
                double half = LeftThumb is { } left ? left.Width / 2 : 0;
                Llayout.Arrange(new Rect(new Point(-half, -half), new Size(finalSize.Width + half * 2, finalSize.Height + half * 2)));
            }
            return finalSize;
        }
        #endregion

        #region 方法
        /// <summary>
        /// 共享的拖拽点模板（静态只读，构造一次即可复用，避免每个拖拽点独立创建 ControlTemplate）<br/>
        /// 颜色/线径通过 Thumb 的 Background/BorderBrush/BorderThickness 模板绑定传入，保持每个实例可定制
        /// </summary>
        private static readonly ControlTemplate SharedThumbTemplate;

        /// <summary>
        /// 静态构造函数：只创建一次共享的拖拽点模板
        /// </summary>
        static DragControlsBase()
        {
            FrameworkElementFactory element = new FrameworkElementFactory(typeof(Ellipse));  //绘制椭圆形元素
            element.SetValue(Ellipse.FillProperty, new TemplateBindingExtension(Control.BackgroundProperty));  //内圈色
            element.SetValue(Ellipse.StrokeProperty, new TemplateBindingExtension(Control.BorderBrushProperty));  //外圈色
            element.SetValue(Ellipse.StrokeThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));  //线径
            SharedThumbTemplate = new ControlTemplate(typeof(Thumb))
            {
                VisualTree = element
            };
            SharedThumbTemplate.Seal();
        }

        /// <summary>
        /// 初始化装饰器布局容器（网格 + 边框容器），三类装饰点共用同一容器
        /// </summary>
        private void InitLayout()
        {
            Llayout = new Grid();
            //给布局容器加个边框
            Border border = new Border
            {
                Margin = new Thickness(2),
                Opacity = BorderOpacity,
                BorderThickness = BorderWireDiameter,
                BorderBrush = BorderColor
            };
            Llayout.Children.Add(border);
            AddVisualChild(Llayout);
        }

        /// <summary>
        /// 初始化拖拽大小（四周 8 个缩放点，蓝色）
        /// </summary>
        public void InitDragDelta()
        {
            //初始化装饰器
            LeftThumb = new Thumb
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.SizeWE
            };
            TopThumb = new Thumb
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = System.Windows.Input.Cursors.SizeNS
            };
            RightThumb = new Thumb
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.SizeWE
            };
            BottomThumb = new Thumb
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Cursor = System.Windows.Input.Cursors.SizeNS
            };
            LefTopThumb = new Thumb
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = System.Windows.Input.Cursors.SizeNWSE
            };
            RightTopThumb = new Thumb
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = System.Windows.Input.Cursors.SizeNESW
            };
            RightBottomThumb = new Thumb
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Cursor = System.Windows.Input.Cursors.SizeNWSE
            };
            LeftbottomThumb = new Thumb
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Cursor = System.Windows.Input.Cursors.SizeNESW
            };
            //给布局容器添加拖动大小装饰器
            Llayout.Children.Add(LeftThumb);
            Llayout.Children.Add(TopThumb);
            Llayout.Children.Add(RightThumb);
            Llayout.Children.Add(BottomThumb);
            Llayout.Children.Add(LefTopThumb);
            Llayout.Children.Add(RightTopThumb);
            Llayout.Children.Add(RightBottomThumb);
            Llayout.Children.Add(LeftbottomThumb);
        }

        /// <summary>
        /// 初始化中心移动圈（红色，拖动整个控件）
        /// </summary>
        public void InitCentreThumb()
        {
            CentreThumb = new Thumb
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.SizeAll
            };
            Llayout.Children.Add(CentreThumb);
        }

        /// <summary>
        /// 初始化顶部旋转圈（红色，拖动绕中心旋转）
        /// </summary>
        public void InitRotateThumb()
        {
            RotateThumb = new Thumb
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -RotateThumbDistance, 0, 0),
                Cursor = System.Windows.Input.Cursors.SizeAll
            };
            Llayout.Children.Add(RotateThumb);
        }

        /// <summary>
        /// 应用拖拽点样式（尺寸/颜色/动画/拖拽事件），按角色区分
        /// </summary>
        private void StyleThumbs()
        {
            if (Llayout == null) return;
            foreach (var item in Llayout.Children)
            {
                if (item.GetType().Equals(typeof(Thumb)))
                {
                    Thumb thumb = item as Thumb;
                    thumb.Opacity = ThumbOpacity;//透明度
                    thumb.Template = SharedThumbTemplate;  //使用共享模板，避免每个拖拽点独立创建
                    thumb.BorderThickness = new Thickness(ThumbWireDiameter);  //线径（模板绑定）
                    if (ReferenceEquals(thumb, CentreThumb))
                    {
                        thumb.Width = 10;   //中心的点比四周的大一点（红色）
                        thumb.Height = 10;
                        thumb.Background = ThumbInnerColor;
                        thumb.BorderBrush = ThumbOuterColor;
                        thumb.DragDelta += Control_DragDeltaCentre;
                        AnimateInteractiveThumb(thumb, 0.1);   //呼吸动画
                    }
                    else if (ReferenceEquals(thumb, RotateThumb))
                    {
                        thumb.Width = 10;   //旋转手柄（红色）
                        thumb.Height = 10;
                        thumb.Background = ThumbInnerColor;
                        thumb.BorderBrush = ThumbOuterColor;
                        thumb.DragDelta += Control_DragDeltaRotate;
                        AnimateInteractiveThumb(thumb, 0.0);   //呼吸动画
                    }
                    else
                    {
                        thumb.Width = 5;   //设置圆圈的宽（四周调整圈，蓝色）
                        thumb.Height = 5;  //设置圆圈的高
                        thumb.Background = SurroundInnerColor;
                        thumb.BorderBrush = SurroundOuterColor;
                        thumb.DragDelta += Control_DragDelta;
                        AnimatePopIn(thumb);
                    }
                }
            }
        }

        /// <summary>
        /// 调整圈出现动画：透明度 0 → ThumbOpacity 淡入（220ms）
        /// </summary>
        private static void AnimatePopIn(UIElement thumb)
        {
            double targetOpacity = thumb.Opacity;
            var animation = new DoubleAnimation(0, targetOpacity, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            thumb.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        /// <summary>
        /// 交互点（中心/旋转圈）呼吸动画：透明度 0.55 ↔ 1.0 缓慢往复（1.6s）
        /// </summary>
        private static void AnimateInteractiveThumb(UIElement thumb, double beginTimeSeconds)
        {
            thumb.RenderTransformOrigin = new Point(0.5, 0.5);
            thumb.RenderTransform = new ScaleTransform(1, 1);
            // NOTE: 无限(Forever)呼吸动画会持续触发渲染/布局失效，
            // 在部分渲染环境下导致同窗口其他元素（如左侧面板）绘制撕裂/不绘制；
            // 改为有限时长动画（3 次往复后稳定），视觉相近且消除持续重绘
            var breathe = new DoubleAnimation(0.55, 1.0, TimeSpan.FromSeconds(1.6))
            {
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3),
                BeginTime = TimeSpan.FromSeconds(beginTimeSeconds),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            thumb.BeginAnimation(UIElement.OpacityProperty, breathe);
        }

        /// <summary>
        /// 初始化移动
        /// </summary>
        public void InitMove()
        {
            //添加移动事件
            Controls.MouseLeftButtonDown += Control_MouseLeftButtonDown;   //鼠标左键按下
            Controls.MouseLeftButtonUp += Control_MouseLeftButtonUp;   //鼠标左键松开
            Controls.MouseMove += Control_MouseMove;   //鼠标移动
        }
        /// <summary>
        /// 移除移动事件订阅（退订三个鼠标事件，防止事件泄漏）
        /// </summary>
        public void Detach()
        {
            Controls.MouseLeftButtonDown -= Control_MouseLeftButtonDown;
            Controls.MouseLeftButtonUp -= Control_MouseLeftButtonUp;
            Controls.MouseMove -= Control_MouseMove;
        }
        #endregion

        #region 事件
        /// <summary>
        /// 拖拽大小逻辑事件处理。<br/>
        /// 根据拖拽方向（由 Thumb 的对齐方式决定）计算新的宽高和边距，<br/>
        /// 受最小/最大尺寸限制。
        /// </summary>
        private void Control_DragDelta(object sender, DragDeltaEventArgs e)
        {
            FrameworkElement Control = Controls as FrameworkElement;  //要拖动的控件
            FrameworkElement Thumb = sender as FrameworkElement;  //哪个装饰被拖动
            double Left, Top, Right, Bottom, Width, Height;   //左，上，右，下，宽，高
            if (Thumb.HorizontalAlignment == System.Windows.HorizontalAlignment.Left)
            {
                Right = Control.Margin.Right;
                Left = Control.Margin.Left + e.HorizontalChange;
                Width = (double.IsNaN(Control.Width) ? Control.ActualWidth : Control.Width) - e.HorizontalChange;
            }
            else
            {
                Left = Control.Margin.Left;
                Right = Control.Margin.Right - e.HorizontalChange;
                Width = (double.IsNaN(Control.Width) ? Control.ActualWidth : Control.Width) + e.HorizontalChange;
            }
            if (Thumb.VerticalAlignment == VerticalAlignment.Top)
            {
                Bottom = Control.Margin.Bottom;
                Top = Control.Margin.Top + e.VerticalChange;
                Height = (double.IsNaN(Control.Height) ? Control.ActualHeight : Control.Height) - e.VerticalChange;
            }
            else
            {
                Top = Control.Margin.Top;
                Bottom = Control.Margin.Bottom - e.VerticalChange;
                Height = (double.IsNaN(Control.Height) ? Control.ActualHeight : Control.Height) + e.VerticalChange;
            }

            if (Thumb.HorizontalAlignment != System.Windows.HorizontalAlignment.Center)
            {
                if (Width >= 0)
                {
                    if (Width >= MinWidths && Width <= MaxWidths)
                    {
                        Control.Margin = new Thickness(Left, Control.Margin.Top, Right, Control.Margin.Bottom);
                        Control.Width = Width;
                    }
                }
            }
            if (Thumb.VerticalAlignment != VerticalAlignment.Center)
            {
                if (Height >= 0)
                {
                    if (Height >= MinHeights && Height <= MaxHeights)
                    {
                        Control.Margin = new Thickness(Control.Margin.Left, Top, Control.Margin.Right, Bottom);
                        Control.Height = Height;
                    }
                }
            }
        }

        /// <summary>
        /// 中心点拖拽事件处理。<br/>
        /// 拖动整个控件（修改 Margin 平移位置，不改尺寸）。<br/>
        /// 采用容器空间绝对定位：让控件中心跟随鼠标 —— 直接使用鼠标在容器空间的位置，
        /// 不依赖 Thumb 的局部增量（局部坐标系随装饰器旋转，旋转后增量会反向/混淆导致飞走或卡顿）。
        /// </summary>
        private void Control_DragDeltaCentre(object sender, DragDeltaEventArgs e)
        {
            if (Controls is not FrameworkElement control || LlayoutContainer is not FrameworkElement container) return;
            var center = control.TranslatePoint(new Point(control.ActualWidth / 2, control.ActualHeight / 2), container);
            var pos = Mouse.GetPosition(container);
            var delta = pos - center;
            var margin = control.Margin;
            control.Margin = new Thickness(
                margin.Left + delta.X,
                margin.Top + delta.Y,
                margin.Right - delta.X,
                margin.Bottom - delta.Y);
        }

        /// <summary>
        /// 旋转手柄拖拽事件处理。<br/>
        /// 以控件中心为圆心，按鼠标相对中心的角度旋转（0° = 手柄在正上方）。
        /// </summary>
        private void Control_DragDeltaRotate(object sender, DragDeltaEventArgs e)
        {
            if (Controls is not FrameworkElement control || LlayoutContainer is not FrameworkElement container) return;
            var center = control.TranslatePoint(new Point(control.ActualWidth / 2, control.ActualHeight / 2), container);
            var pos = Mouse.GetPosition(container);
            double angle = Math.Atan2(pos.Y - center.Y, pos.X - center.X) * 180 / Math.PI + 90;
            SetRotation(angle);
        }

        /// <summary>
        /// 设置旋转角度（度，绕控件中心、顺时针）。<br/>
        /// 控件与装饰器内容（周围点/手柄/连接线）绕同一中心同步旋转。<br/>
        /// 注意：变换只加到控件与内部布置网格上，不能加到 Adorner 本体 ——
        /// AdornerLayer 会跟随控件的 RenderTransform 重新排列 adorner，叠加会错位。
        /// </summary>
        /// <param name="angle">角度（度）</param>
        public void SetRotation(double angle)
        {
            Angle = angle % 360;
            if (Controls is FrameworkElement control)
            {
                // 只旋转控件本身：AdornerLayer 渲染装饰器时会自动跟随 adorned element 的 RenderTransform，
                // 装饰器内容（周围点/手柄/连接线）无需（也不能）再手动旋转，否则会叠加成 2 倍角度
                control.RenderTransformOrigin = new Point(0.5, 0.5);
                control.RenderTransform = new RotateTransform(Angle);
            }
        }

        /// <summary>
        /// 鼠标左键按下事件处理。<br/>
        /// 记录按下位置和当前边距，捕获鼠标以实现拖动。
        /// </summary>
        private void Control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var c = sender as FrameworkElement;
            IsMouseDown = true;
            MouseDownPosition = e.GetPosition(LlayoutContainer);
            MouseDownMargin = c.Margin;
            c.CaptureMouse();
        }
        /// <summary>
        /// 鼠标左键松开事件处理。<br/>
        /// 停止拖动并释放鼠标捕获。
        /// </summary>
        private void Control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var c = sender as FrameworkElement;
            IsMouseDown = false;
            c.ReleaseMouseCapture();
        }
        /// <summary>
        /// 鼠标移动事件处理。<br/>
        /// 根据鼠标位移计算新的 Margin 值，实现控件的自由拖动。
        /// </summary>
        private void Control_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsMouseDown)
            {
                var c = sender as FrameworkElement;
                var pos = e.GetPosition(LlayoutContainer);
                var dp = pos - MouseDownPosition;
                double Left, Top, Right, Bottom;  //设置控件坐标
                Left = MouseDownMargin.Left + dp.X;
                Top = MouseDownMargin.Top + dp.Y;
                Right = MouseDownMargin.Right - dp.X;
                Bottom = MouseDownMargin.Bottom - dp.Y;
                c.Margin = new Thickness(Left, Top, Right, Bottom);

                //GeneralTransform generalTransform = c.TransformToAncestor(LlayoutContainer);
                //Point point = generalTransform.Transform(new Point(0, 0));
                ////控件的  左上右下
                //double ControlLeft = c.Margin.Left;   //左
                //double ControlTop = c.Margin.Top;     //上
                //double ControlRight = point.X + c.Width;   //右
                //double ControlBottom = point.Y + c.Height;  //下
            }
        }
        #endregion
    }
}
