  /*
     动画拖动，包含了控件的移动与缩放大小
     注意： 单个窗体中只能定义一个布局容器，这个布局容器，不能设置Margin，不能设置固定宽高
     by:Shunnet.top 2022/6/8
    -----------------------------下面是使用方法---------------------------
     */

    #region 后端代码
    /*
    	/// <summary>
        /// 三合一  
        /// 单个窗体中只能定义一个布局容器，这个布局容器，不能设置Margin，不能设置固定宽高
        /// </summary>
        DragControlsAnimate dragControlsAnimate;
        public MainWindow()
        {
            InitializeComponent();
            dragControlsAnimate = new DragControlsAnimate(this, Pane);   //你得定义一个容器传容器对象或者Name
            dragControlsAnimate.Insert(ConShow1);
            dragControlsAnimate.Insert(ConShow2);
            dragControlsAnimate.MessageEvenTrigger += MessageEvenTrigger;
            dragControlsAnimate.DragEvenTrigger += DragEvenTrigger;
        }
        /// <summary>
        /// 消息
        /// </summary>
        /// <param name="Message">消息</param>
        /// <param name="element">哪个控件显示的消息</param>
        public void MessageEvenTrigger(string Message, FrameworkElement element)
        {
            Console.WriteLine($"控件Name:{element.Name}->抛出消息：{Message}");
        }
        /// <summary>
        /// 提醒拖拽事件开始了，请传需要拖动的按钮对象
        /// </summary>
        /// <param name="element">在哪个控件上触发了拖拽</param>
        /// <returns>返回已经创建了新的控件对象  -   是否需要移动   -  是否需要拖拽大小</returns>
        public (FrameworkElement NewControl, bool IsMove, bool IsDragSize) DragEvenTrigger(FrameworkElement ShowControl)
        {
            FrameworkElement NewControl = new FrameworkElement();
            bool IsMove = false;
            bool IsDragSize = false;
            switch (ShowControl.Name)
            {
                case "ConShow1":
                    NewControl = InitControls(0);
                    IsMove = false;
                    IsDragSize =false;
                    break;
                case "ConShow2":
                    NewControl = InitControls(1);
                    IsMove = true;
                    IsDragSize = true;
                    break;
            }
            return (NewControl, IsMove, IsDragSize);
        }
        /// <summary>
        /// 创建图标
        /// </summary>
        /// <param name="dashboardDataMode">图标类型</param>
        private Label InitControls(int A)
        {
            return new Label() { Background = new SolidColorBrush(A == 0 ? Colors.AliceBlue : Colors.AntiqueWhite), Width = 100, Height = 100,Content= "自定义控件" }; 
        }
    */
    #endregion

    #region 前端代码
    /*
         <Window x:Class="WpfApp5.MainWindow"
            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
            xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
            Title="Canvas与Grid 中拖动动画+缩放+移动 Shunnet.top" Height="500" Width="800" >
        <!--<Canvas Name="Pane" Background="DarkGray">
            <Label Content="这是使用Canvas容器布局,单个窗体中只能定义一个布局容器，这个布局容器，不能设置Margin，不能设置固定宽高" Foreground="Red" FontWeight="Bold"/>
            <Button Content="不能拖动" Width="90" Height="50" Name="ConShow1" VerticalAlignment="Top" HorizontalAlignment="Left"  Margin="0,30,0,0"/>
            <Button Content="可以拖动" Width="90" Height="50" Name="ConShow2" VerticalAlignment="Top" HorizontalAlignment="Left"  Margin="100,30,0,0"/>
        </Canvas>-->
        <Grid Name="Pane" Background="DarkGray">
            <Label Content="这是使用GRID容器布局,单个窗体中只能定义一个布局容器，这个布局容器，不能设置Margin，不能设置固定宽高" Foreground="Red" FontWeight="Bold"/>
            <Button Content="不能拖动" Width="90" Height="50" Name="ConShow1" VerticalAlignment="Top" HorizontalAlignment="Left"  Margin="0,30,0,0"/>
            <Button Content="可以拖动" Width="90" Height="50" Name="ConShow2" VerticalAlignment="Top" HorizontalAlignment="Left"  Margin="100,30,0,0"/>
        </Grid>
    </Window>

     */
    #endregion