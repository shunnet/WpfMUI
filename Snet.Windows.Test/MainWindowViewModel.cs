using CommunityToolkit.Mvvm.Input;
using Snet.Core.handler;
using Snet.Utility;
using Snet.Windows.Core.mvvm;
using System.Diagnostics;

namespace Snet.Windows.Test
{
    public class MainWindowViewModel : BindNotify
    {
        public MainWindowViewModel()
        {
            LanguageHandler.OnLanguageEvent += LanguageHandler_OnLanguageEvent;
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
            string message = App.LanguageOperate.GetLanguageValue("Welcome");
            await Snet.Windows.Controls.message.MessageBox.Show(message, title, Snet.Windows.Controls.@enum.MessageBoxButton.OK, Snet.Windows.Controls.@enum.MessageBoxImage.Information);



        }
    }
}
