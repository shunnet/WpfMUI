  /*
     �����϶��������˿ؼ����ƶ������Ŵ�С
     ע�⣺ ����������ֻ�ܶ���һ���������������������������������Margin���������ù̶����
     by:Shunnet.top 2022/6/8
    -----------------------------������ʹ�÷���---------------------------
     */

    #region ��˴���
    /*
    	/// <summary>
        /// ����һ  
        /// ����������ֻ�ܶ���һ���������������������������������Margin���������ù̶����
        /// </summary>
        DragControlsAnimate dragControlsAnimate;
        public MainWindow()
        {
            InitializeComponent();
            dragControlsAnimate = new DragControlsAnimate(this, Pane);   //��ö���һ�������������������Name
            dragControlsAnimate.Insert(ConShow1);
            dragControlsAnimate.Insert(ConShow2);
            dragControlsAnimate.MessageEvenTrigger += MessageEvenTrigger;
            dragControlsAnimate.DragEvenTrigger += DragEvenTrigger;
        }
        /// <summary>
        /// ��Ϣ
        /// </summary>
        /// <param name="Message">��Ϣ</param>
        /// <param name="element">�ĸ��ؼ���ʾ����Ϣ</param>
        public void MessageEvenTrigger(string Message, FrameworkElement element)
        {
            Console.WriteLine($"�ؼ�Name:{element.Name}->�׳���Ϣ��{Message}");
        }
        /// <summary>
        /// ������ק�¼���ʼ�ˣ��봫��Ҫ�϶��İ�ť����
        /// </summary>
        /// <param name="element">���ĸ��ؼ��ϴ�������ק</param>
        /// <returns>�����Ѿ��������µĿؼ�����  -   �Ƿ���Ҫ�ƶ�   -  �Ƿ���Ҫ��ק��С</returns>
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
        /// ����ͼ��
        /// </summary>
        /// <param name="dashboardDataMode">ͼ������</param>
        private Label InitControls(int A)
        {
            return new Label() { Background = new SolidColorBrush(A == 0 ? Colors.AliceBlue : Colors.AntiqueWhite), Width = 100, Height = 100,Content= "�Զ���ؼ�" }; 
        }
    */
    #endregion

    #region ǰ�˴���
    /*
         <Window x:Class="WpfApp5.MainWindow"
            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
            xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
            Title="Canvas��Grid ���϶�����+����+�ƶ� Shunnet.top" Height="500" Width="800" >
        <!--<Canvas Name="Pane" Background="DarkGray">
            <Label Content="����ʹ��Canvas��������,����������ֻ�ܶ���һ���������������������������������Margin���������ù̶����" Foreground="Red" FontWeight="Bold"/>
            <Button Content="�����϶�" Width="90" Height="50" Name="ConShow1" VerticalAlignment="Top" HorizontalAlignment="Left"  Margin="0,30,0,0"/>
            <Button Content="�����϶�" Width="90" Height="50" Name="ConShow2" VerticalAlignment="Top" HorizontalAlignment="Left"  Margin="100,30,0,0"/>
        </Canvas>-->
        <Grid Name="Pane" Background="DarkGray">
            <Label Content="����ʹ��GRID��������,����������ֻ�ܶ���һ���������������������������������Margin���������ù̶����" Foreground="Red" FontWeight="Bold"/>
            <Button Content="�����϶�" Width="90" Height="50" Name="ConShow1" VerticalAlignment="Top" HorizontalAlignment="Left"  Margin="0,30,0,0"/>
            <Button Content="�����϶�" Width="90" Height="50" Name="ConShow2" VerticalAlignment="Top" HorizontalAlignment="Left"  Margin="100,30,0,0"/>
        </Grid>
    </Window>

     */
    #endregion