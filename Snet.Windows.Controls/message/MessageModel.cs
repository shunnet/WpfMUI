using Snet.Windows.Core.mvvm;
using System.Windows;
using System.Windows.Media;

namespace Snet.Windows.Controls.message
{
    /// <summary>
    /// 消息框数据模型<br/>
    /// 用于绑定自定义消息对话框的标题、内容、图标等信息<br/>
    /// 继承 BindNotify 支持属性变更通知
    /// </summary>
    public class MessageModel : BindNotify
    {
        /// <summary>
        /// 构造函数：自动获取当前应用程序主窗口的图标作为标题图标
        /// </summary>
        public MessageModel()
        {
            this.Icon = Application.Current?.MainWindow?.Icon;
        }

        /// <summary>
        /// 消息内容文本<br/>
        /// 显示在对话框主体区域的消息正文
        /// </summary>
        public string Content
        {
            get => GetProperty(() => Content);
            set => SetProperty(() => Content, value);
        }

        /// <summary>
        /// 标题栏图标图像源<br/>
        /// 默认使用主窗口图标，可自定义设置
        /// </summary>
        public ImageSource Icon
        {
            get => GetProperty(() => Icon);
            set => SetProperty(() => Icon, value);
        }

        /// <summary>
        /// 消息类型图标图像源<br/>
        /// 根据 MessageBoxImage 类型设置，如感叹号、错误图标等
        /// </summary>
        public ImageSource ContentIcon
        {
            get => GetProperty(() => ContentIcon);
            set => SetProperty(() => ContentIcon, value);
        }

        /// <summary>
        /// 对话框标题文本<br/>
        /// 显示在对话框顶部的标题
        /// </summary>
        public string Title
        {
            get => GetProperty(() => Title);
            set => SetProperty(() => Title, value);
        }
    }
}
