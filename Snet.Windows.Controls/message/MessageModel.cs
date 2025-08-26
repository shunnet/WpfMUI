using Snet.Windows.Core.mvvm;
using System.Windows;
using System.Windows.Media;

namespace Snet.Windows.Controls.message
{
    /// <summary>
    /// 消息模型
    /// </summary>
    public class MessageModel : BindNotify
    {
        /// <summary>
        /// 无参构造函数
        /// </summary>
        public MessageModel()
        {
            this.Icon = Application.Current?.MainWindow?.Icon;
        }

        /// <summary>
        /// 内容
        /// </summary>
        public string Content
        {
            get => GetProperty(() => Content);
            set => SetProperty(() => Content, value);
        }

        /// <summary>
        /// 标题图标图像源
        /// </summary>
        public ImageSource Icon
        {
            get => GetProperty(() => Icon);
            set => SetProperty(() => Icon, value);
        }

        /// <summary>
        /// 消息图标图像源
        /// </summary>
        public ImageSource ContentIcon
        {
            get => GetProperty(() => ContentIcon);
            set => SetProperty(() => ContentIcon, value);
        }
        /// <summary>
        /// 标题
        /// </summary>
        public string Title
        {
            get => GetProperty(() => Title);
            set => SetProperty(() => Title, value);
        }


    }
}
