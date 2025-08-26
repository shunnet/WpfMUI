using Snet.Windows.Core.mvvm;
using System.Windows.Controls;
namespace Snet.Windows.Controls.data
{
    public class TabControlModel : BindNotify
    {
        public TabControlModel(string title, object icon, UserControl content)
        {
            Title = title;
            Icon = icon;
            Content = content;
        }

        public TabControlModel(string title, object icon)
        {
            Title = title;
            Icon = icon;
        }

        /// <summary>
        /// 内容
        /// </summary>
        public UserControl Content
        {
            get => GetProperty(() => Content);
            set => SetProperty(() => Content, value);
        }

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
    }

}