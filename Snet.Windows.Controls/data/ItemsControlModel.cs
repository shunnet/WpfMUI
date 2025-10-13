using Snet.Model.data;
using Snet.Windows.Core.mvvm;

namespace Snet.Windows.Controls.data
{
    public class ItemsControlModel : BindNotify
    {
        public ItemsControlModel(string key, object icon, LanguageModel model, bool isChecked = false, bool isEnabled = true)
        {
            Key = key;
            Title = Snet.Core.handler.LanguageHandler.GetLanguageValue(key, model);
            Icon = icon;
            IsEnabled = isEnabled;
            IsChecked = isChecked;
        }
        /// <summary>
        /// 通过Key获取中英文
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 选中
        /// </summary>
        public bool IsChecked
        {
            get => GetProperty(() => IsChecked);
            set => SetProperty(() => IsChecked, value);
        }

        /// <summary>
        /// 启用
        /// </summary>
        public bool IsEnabled
        {
            get => isChecked;
            set => SetProperty(ref isChecked, value);
        }
        private bool isChecked = true;

        /// <summary>
        /// 图标
        /// </summary>
        public object Icon
        {
            get => GetProperty(() => Icon);
            set => SetProperty(() => Icon, value);
        }

        /// <summary>
        /// 显示文字
        /// </summary>
        public string Title
        {
            get => GetProperty(() => Title);
            set => SetProperty(() => Title, value);
        }

        /// <summary>
        /// 预留属性
        /// </summary>
        public object? Content
        {
            get => GetProperty(() => Content);
            set => SetProperty(() => Content, value);
        }
    }
}
