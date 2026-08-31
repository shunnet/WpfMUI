using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Snet.Core.communication.net.tcp.service;
using Snet.Core.handler;
using Snet.Utility;
using Snet.Windows.Controls.data;
using Snet.Windows.Controls.property;
using Snet.Windows.Controls.property.wpf;
using Snet.Windows.Core.mvvm;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Media;

namespace Snet.Windows.Test
{
    /// <summary>数据表格测试行模型</summary>
    public class DataRowModel
    {
        public string Name { get; set; } = "";
        public double Value { get; set; }
        public bool Enabled { get; set; } = true;
        public string Type { get; set; } = "";
    }

    /// <summary>树形列表测试节点模型</summary>
    public class TreeNodeModel
    {
        public string Name { get; set; } = "";
        public ObservableCollection<TreeNodeModel> Children { get; set; } = new();
    }

    /// <summary>测试枚举</summary>
    public enum TestEnum
    {
        [System.ComponentModel.Description("选项一")]
        Option1,
        [System.ComponentModel.Description("选项二")]
        Option2,
        [System.ComponentModel.Description("选项三")]
        Option3
    }

    public class MainWindowViewModel : BindNotify
    {
        public MainWindowViewModel()
        {
            LanguageHandler.OnLanguageEvent += LanguageHandler_OnLanguageEvent;
            ComboBoxItemsSource.Add(new ComboBoxModel("测试控件", 1));
            ComboBoxItemsSource.Add(new ComboBoxModel("二号科室", 2));
            ComboBoxItemsSource.Add(new ComboBoxModel("三号科室", 3));
            ComboBoxItemsSource.Add(new ComboBoxModel("四号科室", 4));
            ComboBoxItemsSource.Add(new ComboBoxModel("五号科室", 5));
            //ComboBoxSelectedItem = ComboBoxItemsSource[0];

            TextBoxText = "测试控件";

            bd = basics;

            // 测试数据：表格行
            for (int i = 0; i < 50; i++)
            {
                DataRows.Add(new DataRowModel
                {
                    Name = $"设备{i + 1}",
                    Value = i * 1.5,
                    Enabled = i % 3 != 0,
                    Type = i % 2 == 0 ? "PLC" : "仪表"
                });
            }

            // 测试数据：树形节点
            for (int i = 0; i < 5; i++)
            {
                var node = new TreeNodeModel { Name = $"根节点{i + 1}" };
                for (int j = 0; j < 4; j++)
                {
                    node.Children.Add(new TreeNodeModel { Name = $"子节点{i + 1}-{j + 1}" });
                }
                TreeRoots.Add(node);
            }

            // 编辑器初始文本
            EditorText = """
                using System;

                namespace Demo
                {
                    /// <summary>测试代码</summary>
                    public class Program
                    {
                        public static void Main(string[] args)
                        {
                            Console.WriteLine("Hello Snet WPF!");
                        }
                    }
                }





                [ Info ] 2026/8/26 23:18:24 : [ OpcUaService ] TRUE
                {
                  "Message": "opc.tcp://127.0.0.1:6688/Opc.Ua.Service",
                  "Status": true,
                  "Time": "2026-08-26T23:18:24.4219612+08:00"
                }
                [ Info ] 2026/8/26 23:18:24 : {
                  "RunTime": 2221,
                  "ResultData": null,
                  "Message": "[ OpcUaServiceOperate ]( OnAsync 成功 )",
                  "Status": true,
                  "Time": "2026-08-26T23:18:24.4371351+08:00"
                }
                [ Info ] 2026/8/26 23:18:24 : [ MqttService ] TRUE
                {
                  "Message": "[ 服务启动事件 ]服务已启动",
                  "Status": true,
                  "Time": "2026-08-26T23:18:24.4763866+08:00"
                }
                [ Info ] 2026/8/26 23:18:24 : {
                  "RunTime": 30,
                  "ResultData": null,
                  "Message": "[ MqttServiceOperate ]( OnAsync 成功 )",
                  "Status": true,
                  "Time": "2026-08-26T23:18:24.4776434+08:00"
                }
                [ Info ] 2026/8/26 23:18:24 : 2 台设备已成功加载
                
                """;

            // 页码条初始数据
            PageTotal = 100;
            PageSize = 10;
            PageIndex = 1;

            // LED 指示灯默认点亮（绿色，参考 Daq）
            LedOn = true;
            LedColor = System.Windows.Media.Colors.Green;
        }
        TcpServiceData.Basics basics = new TcpServiceData.Basics();
        private void LanguageHandler_OnLanguageEvent(object? sender, Model.data.EventLanguageResult e)
        {
            Debug.WriteLine(e.ToJson(true));
        }

        public IAsyncRelayCommand Hello => p_Hello ??= new AsyncRelayCommand(HelloAsync);
        IAsyncRelayCommand p_Hello;
        public async Task HelloAsync()
        {
            string title = App.LanguageOperate.GetLanguageValue("Hello");
            string message = App.LanguageOperate.GetLanguageValue("Welcome") + $"\r\n{ComboBoxSelectedItem?.Key}\r\n{TextBoxText}";
            await Snet.Windows.Controls.message.MessageBox.Show(message, title, Snet.Windows.Controls.@enum.MessageBoxButton.OK, Snet.Windows.Controls.@enum.MessageBoxImage.Information);
        }

        public IAsyncRelayCommand Hello1 => p_Hello1 ??= new AsyncRelayCommand(HelloAsync1);
        IAsyncRelayCommand p_Hello1;

        public IAsyncRelayCommand Hello2 => p_Hello2 ??= new AsyncRelayCommand(HelloAsync2);
        IAsyncRelayCommand p_Hello2;

        public IAsyncRelayCommand Hello3 => p_Hello3 ??= new AsyncRelayCommand(HelloAsync3);
        IAsyncRelayCommand p_Hello3;

        public IAsyncRelayCommand Hello4 => p_Hello4 ??= new AsyncRelayCommand(HelloAsync4);
        IAsyncRelayCommand p_Hello4;

        /// <summary>
        /// 属性框
        /// </summary>
        public object bd
        {
            get => GetProperty(() => bd);
            set => SetProperty(() => bd, value);
        }

        public async Task HelloAsync1()
        {
            string title = App.LanguageOperate.GetLanguageValue("Hello");
            string message = App.LanguageOperate.GetLanguageValue("Welcome") + $"\r\n{ComboBoxSelectedItem?.Key}\r\n{TextBoxText}";
            await Snet.Windows.Controls.message.MessageBox.Show(message, title, Snet.Windows.Controls.@enum.MessageBoxButton.OKCancel, Snet.Windows.Controls.@enum.MessageBoxImage.Information);
        }

        public async Task HelloAsync2()
        {
            string title = App.LanguageOperate.GetLanguageValue("Hello");
            string message = App.LanguageOperate.GetLanguageValue("Welcome") + $"\r\n{ComboBoxSelectedItem?.Key}\r\n{TextBoxText}";
            await Snet.Windows.Controls.message.MessageBox.Show(message, title, Snet.Windows.Controls.@enum.MessageBoxButton.Yes, Snet.Windows.Controls.@enum.MessageBoxImage.Information);
        }

        public async Task HelloAsync3()
        {
            string title = App.LanguageOperate.GetLanguageValue("Hello");
            string message = App.LanguageOperate.GetLanguageValue("Welcome") + $"\r\n{ComboBoxSelectedItem?.Key}\r\n{TextBoxText}";
            await Snet.Windows.Controls.message.MessageBox.Show(message, title, Snet.Windows.Controls.@enum.MessageBoxButton.YesNo, Snet.Windows.Controls.@enum.MessageBoxImage.Information);
        }

        PropertyControl control = new PropertyControl();
        public async Task HelloAsync4()
        {
            control.ButtonVisibility = System.Windows.Visibility.Visible;
            control.SetBasics(basics);
            if ((await DialogHost.Show(control, "DialogHost")).ToBool())
            {
                var data = control.GetBasics().GetSource<TcpServiceData.Basics>();

                string title = App.LanguageOperate.GetLanguageValue("Hello");
                string message = data.ToJson(true);
                await Snet.Windows.Controls.message.MessageBox.Show(message, title, Snet.Windows.Controls.@enum.MessageBoxButton.YesNo, Snet.Windows.Controls.@enum.MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 打开属性编辑对话框
        /// </summary>
        public IAsyncRelayCommand OpenPropertyDialog => p_OpenPropertyDialog ??= new AsyncRelayCommand(OpenPropertyDialogAsync);
        IAsyncRelayCommand p_OpenPropertyDialog;
        public async Task OpenPropertyDialogAsync()
        {
            var dialog = new PropertyDialog { Owner = System.Windows.Application.Current.MainWindow };
            dialog.DataContext = new DataRowModel { Name = "测试设备", Value = 66.6, Enabled = true, Type = "PLC" };
            dialog.ShowDialog();
            await Task.CompletedTask;
        }

        /// <summary>
        /// 打开向导对话框
        /// </summary>
        public IAsyncRelayCommand OpenWizardDialog => p_OpenWizardDialog ??= new AsyncRelayCommand(OpenWizardDialogAsync);
        IAsyncRelayCommand p_OpenWizardDialog;
        public async Task OpenWizardDialogAsync()
        {
            var dialog = new WizardDialog { Owner = System.Windows.Application.Current.MainWindow };
            dialog.ShowDialog();
            await Task.CompletedTask;
        }

        #region LED 指示灯

        /// <summary>LED 灯开关</summary>
        public bool LedOn
        {
            get => GetProperty(() => LedOn);
            set => SetProperty(() => LedOn, value);
        }

        /// <summary>LED 闪烁</summary>
        public bool LedFlashing
        {
            get => GetProperty(() => LedFlashing);
            set => SetProperty(() => LedFlashing, value);
        }

        /// <summary>LED 颜色</summary>
        public Color LedColor
        {
            get => GetProperty(() => LedColor);
            set => SetProperty(() => LedColor, value, changedCallback: () => OnPropertyChanged(nameof(LedBrush)));
        }

        /// <summary>
        /// LED 颜色的 Brush 形式（用于 Rectangle.Fill 等 Brush 类型绑定）。<br/>
        /// WPF 默认绑定转换器不支持 Color→Brush 的直接转换，
        /// 单独暴露 Brush 属性避免 XAML 绑定错误与运行时静默失败。
        /// </summary>
        public Brush LedBrush
        {
            get
            {
                // 冻结画刷提升性能；SolidColorBrush 可按需修改
                var brush = new SolidColorBrush(LedColor);
                brush.Freeze();
                return brush;
            }
        }

        #endregion

        #region 页码条

        /// <summary>总条数</summary>
        public int PageTotal
        {
            get => GetProperty(() => PageTotal);
            set => SetProperty(() => PageTotal, value);
        }

        /// <summary>每页条数</summary>
        public int PageSize
        {
            get => GetProperty(() => PageSize);
            set => SetProperty(() => PageSize, value);
        }

        /// <summary>当前页</summary>
        public int PageIndex
        {
            get => GetProperty(() => PageIndex);
            set => SetProperty(() => PageIndex, value);
        }

        /// <summary>页码变更命令（参考 Daq：PageIndexChangedCommand）</summary>
        public IAsyncRelayCommand PageIndexChangedCommand => p_PageIndexChangedCommand ??= new AsyncRelayCommand<object>(PageIndexChangedAsync);
        IAsyncRelayCommand p_PageIndexChangedCommand;
        private Task PageIndexChangedAsync(object? param) => Task.CompletedTask;

        #endregion

        #region 表格

        /// <summary>表格数据源</summary>
        public ObservableCollection<DataRowModel> DataRows { get; set; } = new();

        #endregion

        #region 树形列表

        /// <summary>树形数据源</summary>
        public ObservableCollection<TreeNodeModel> TreeRoots { get; set; } = new();

        #endregion

        #region 编辑器

        /// <summary>编辑器文本</summary>
        public string EditorText
        {
            get => GetProperty(() => EditorText);
            set => SetProperty(() => EditorText, value);
        }

        #endregion

        #region 高级编辑控件

        /// <summary>SpinControl 值</summary>
        public double SpinValue
        {
            get => GetProperty(() => SpinValue);
            set => SetProperty(() => SpinValue, value);
        }

        /// <summary>CheckMark 选中</summary>
        public bool CheckValue
        {
            get => GetProperty(() => CheckValue);
            set => SetProperty(() => CheckValue, value);
        }

        /// <summary>滑块值</summary>
        public double SliderValue
        {
            get => GetProperty(() => SliderValue);
            set => SetProperty(() => SliderValue, value);
        }

        /// <summary>单选列表值</summary>
        public TestEnum EnumValue
        {
            get => GetProperty(() => EnumValue);
            set => SetProperty(() => EnumValue, value);
        }

        /// <summary>文件选择</summary>
        public string FilePath
        {
            get => GetProperty(() => FilePath);
            set => SetProperty(() => FilePath, value);
        }

        /// <summary>目录选择</summary>
        public string DirectoryPath
        {
            get => GetProperty(() => DirectoryPath);
            set => SetProperty(() => DirectoryPath, value);
        }

        /// <summary>颜色选择</summary>
        public Color ColorValue
        {
            get => GetProperty(() => ColorValue);
            set => SetProperty(() => ColorValue, value);
        }

        /// <summary>PopupBox 选择值</summary>
        public string PopupValue
        {
            get => GetProperty(() => PopupValue);
            set => SetProperty(() => PopupValue, value);
        }

        #endregion

        /// <summary>
        /// 下拉框数据源
        /// </summary>
        public ObservableCollection<ComboBoxModel> ComboBoxItemsSource
        {
            get => _ComboBoxItemsSource;
            set => SetProperty(ref _ComboBoxItemsSource, value);
        }
        private ObservableCollection<ComboBoxModel> _ComboBoxItemsSource = new ObservableCollection<ComboBoxModel>();

        public ComboBoxModel ComboBoxSelectedItem
        {
            get => GetProperty(() => ComboBoxSelectedItem);
            set => SetProperty(() => ComboBoxSelectedItem, value);
        }

        public string TextBoxText
        {
            get => GetProperty(() => TextBoxText);
            set => SetProperty(() => TextBoxText, value);
        }
    }

}
