using Snet.Model.data;
using Snet.Windows.Core.mvvm;

namespace Snet.Windows.Controls.data
{
    /// <summary>
    /// ItemsControl 子项数据模型<br/>
    /// 用于表示带有选中状态、启用状态、图标和标题的列表项<br/>
    /// 支持多语言标题自动解析
    /// </summary>
    public class ItemsControlModel : BindNotify
    {
        /// <summary>
        /// 构造函数：根据语言模型自动解析标题<br/>
        /// 通过 Key 查找对应语言的显示文本，设置图标及初始状态
        /// </summary>
        /// <param name="key">多语言键值，用于获取对应语言的显示文本</param>
        /// <param name="icon">图标对象，可以是 ImageSource 或其他可视元素</param>
        /// <param name="model">语言模型，用于多语言文本解析</param>
        /// <param name="isChecked">初始选中状态，默认未选中</param>
        /// <param name="isEnabled">初始启用状态，默认启用</param>
        public ItemsControlModel(string key, object icon, LanguageModel model, bool isChecked = false, bool isEnabled = true)
        {
            Key = key;
            Title = Snet.Core.handler.LanguageHandler.GetLanguageValue(key, model);
            Icon = icon;
            IsEnabled = isEnabled;
            IsChecked = isChecked;
        }

        /// <summary>
        /// 多语言键值<br/>
        /// 通过此 Key 获取对应语言的中英文显示文本
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 是否被选中<br/>
        /// 可用于 CheckBox、RadioButton 等控件的绑定
        /// </summary>
        public bool IsChecked
        {
            get => GetProperty(() => IsChecked);
            set => SetProperty(() => IsChecked, value);
        }

        /// <summary>
        /// 是否启用<br/>
        /// 控制该项是否可交互，false 时控件将呈现禁用状态
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
        private bool _isEnabled = true;

        /// <summary>
        /// 图标对象<br/>
        /// 可以是 ImageSource、SymbolIcon 或其他任意可视对象
        /// </summary>
        public object Icon
        {
            get => GetProperty(() => Icon);
            set => SetProperty(() => Icon, value);
        }

        /// <summary>
        /// 显示文字<br/>
        /// 根据语言模型自动解析后的标题文本
        /// </summary>
        public string Title
        {
            get => GetProperty(() => Title);
            set => SetProperty(() => Title, value);
        }

        /// <summary>
        /// 预留扩展属性<br/>
        /// 可用于携带自定义业务数据
        /// </summary>
        public object? Content
        {
            get => GetProperty(() => Content);
            set => SetProperty(() => Content, value);
        }
    }
}
