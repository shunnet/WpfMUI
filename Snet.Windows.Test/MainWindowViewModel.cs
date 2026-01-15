using CommunityToolkit.Mvvm.Input;
using Snet.Core.handler;
using Snet.Utility;
using Snet.Windows.Controls.data;
using Snet.Windows.Core.mvvm;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Snet.Windows.Test
{
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
            ComboBoxSelectedItem = ComboBoxItemsSource[0];

            TextBoxText = "测试控件";
        }

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
