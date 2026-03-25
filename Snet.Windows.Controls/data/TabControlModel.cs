using Snet.Windows.Core.mvvm;
using System.Windows.Controls;
namespace Snet.Windows.Controls.data
{
    /// <summary>
    /// TabControl 选项卡数据模型<br/>
    /// 用于表示选项卡的标题、图标和内容页面<br/>
    /// 继承 BindNotify 支持属性变更通知
    /// </summary>
    public class TabControlModel : BindNotify
    {
        /// <summary>
        /// 构造函数：创建包含标题、图标和内容的完整选项卡模型
        /// </summary>
        /// <param name="title">选项卡标题文本</param>
        /// <param name="icon">选项卡图标对象</param>
        /// <param name="content">选项卡内容页面（UserControl）</param>
        public TabControlModel(string title, object icon, UserControl content)
        {
            Title = title;
            Icon = icon;
            Content = content;
        }

        /// <summary>
        /// 构造函数：创建仅包含标题和图标的选项卡模型（延迟设置内容）
        /// </summary>
        /// <param name="title">选项卡标题文本</param>
        /// <param name="icon">选项卡图标对象</param>
        public TabControlModel(string title, object icon)
        {
            Title = title;
            Icon = icon;
        }

        /// <summary>
        /// 选项卡内容页面<br/>
        /// 通常为 UserControl 实例，显示在选项卡的内容区域
        /// </summary>
        public UserControl Content
        {
            get => GetProperty(() => Content);
            set => SetProperty(() => Content, value);
        }

        /// <summary>
        /// 选项卡图标<br/>
        /// 可以是 ImageSource、SymbolIcon 或其他任意可视对象
        /// </summary>
        public object Icon
        {
            get => GetProperty(() => Icon);
            set => SetProperty(() => Icon, value);
        }

        /// <summary>
        /// 选项卡标题文本<br/>
        /// 显示在选项卡头部的文字
        /// </summary>
        public string Title
        {
            get => GetProperty(() => Title);
            set => SetProperty(() => Title, value);
        }
    }

}