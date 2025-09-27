using MaterialDesignThemes.Wpf;
using Snet.Utility;
using System.Collections.Concurrent;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MessageBoxButton = Snet.Windows.Controls.@enum.MessageBoxButton;
using MessageBoxImage = Snet.Windows.Controls.@enum.MessageBoxImage;

namespace Snet.Windows.Controls.message
{
    /// <summary>
    /// 消息框
    /// </summary>
    public class MessageBox
    {
        /// <summary>
        /// 设置消息图标
        /// </summary>
        private static ImageSource SetIcon(MessageBoxImage iconType)
        {
#pragma warning disable CS8603 // 可能返回 null 引用。
            return iconType switch
            {
                MessageBoxImage.Exclamation => CreateIconBitmap(SystemIcons.Exclamation),
                MessageBoxImage.Application => CreateIconBitmap(SystemIcons.Application),
                MessageBoxImage.Asterisk => CreateIconBitmap(SystemIcons.Asterisk),
                MessageBoxImage.Error => CreateIconBitmap(SystemIcons.Error),
                MessageBoxImage.Hand => CreateIconBitmap(SystemIcons.Hand),
                MessageBoxImage.Information => CreateIconBitmap(SystemIcons.Information),
                MessageBoxImage.Question => CreateIconBitmap(SystemIcons.Question),
                MessageBoxImage.Shield => CreateIconBitmap(SystemIcons.Shield),
                MessageBoxImage.Warning => CreateIconBitmap(SystemIcons.Warning),
                MessageBoxImage.WinLogo => CreateIconBitmap(SystemIcons.WinLogo),
                _ => null  // 默认无图标
            };
#pragma warning restore CS8603 // 可能返回 null 引用。
        }

        /// <summary>
        /// 数据
        /// </summary>
        private static ConcurrentDictionary<Icon, BitmapSource> IconArry = new ConcurrentDictionary<Icon, BitmapSource>();

        /// <summary>
        /// 从系统图标创建位图源
        /// </summary>
        private static BitmapSource CreateIconBitmap(Icon ic)
        {
            BitmapSource bitmapSource = null;
            if (!IconArry.TryGetValue(ic, out BitmapSource? source))
            {
                using (Icon originalIcon = ic)
                {
                    using (Icon icon = (Icon)originalIcon.Clone())
                    {
                        bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        IconArry.TryAdd(ic, bitmapSource);
                    }
                }
            }
            else
            {
                bitmapSource = source;
            }
            return bitmapSource;
        }

        /// <summary>
        /// 标志位
        /// </summary>
        private static string SignPosition = "DialogHost";
        static OK oK = new OK();
        static OKCancel oKCancel = new OKCancel();
        static Yes yes = new Yes();
        static YesNo yesNo = new YesNo();
        /// <summary>
        /// 显示提示框
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="title">标题</param>
        /// <param name="btn">按钮</param>
        /// <param name="img">图标</param>
        /// <returns>状态</returns>
        public static async Task<bool> Show(string content, string title, MessageBoxButton btn, MessageBoxImage img)
        {
            // 设置模型内容
            var messageModel = new MessageModel
            {
                ContentIcon = SetIcon(img),
                Content = content,
                Title = title
            };

            // 设置对话框数据上下文
            object dialogContent = null;

            // 根据按钮类型设置对话框内容
            switch (btn)
            {
                case MessageBoxButton.OK:
                    oK.DataContext = messageModel;
                    dialogContent = oK;
                    break;
                case MessageBoxButton.OKCancel:
                    oKCancel.DataContext = messageModel;
                    dialogContent = oKCancel;
                    break;
                case MessageBoxButton.Yes:
                    yes.DataContext = messageModel;
                    dialogContent = yes;
                    break;
                case MessageBoxButton.YesNo:
                    yesNo.DataContext = messageModel;
                    dialogContent = yesNo;
                    break;
            }

            // 显示对话框
            var result = await Application.Current?.Dispatcher.InvokeAsync(() =>
                         DialogHost.Show(dialogContent, SignPosition),
                         DispatcherPriority.Loaded).Task.Unwrap();

            // 获取对话框结果并返回
            return result?.GetSource<bool>() ?? false;
        }

        /// <summary>
        /// 显示提示框
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="title">标题</param>
        /// <param name="btn">按钮</param>
        /// <returns>状态</returns>
        public static async Task<bool> Show(string content, string title, MessageBoxButton btn) => await Show(content, title, btn, MessageBoxImage.Information);

        /// <summary>
        /// 显示提示框
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="title">标题</param>
        /// <param name="img">图标</param>
        /// <returns>状态</returns>
        public static async Task<bool> Show(string content, string title, MessageBoxImage img) => await Show(content, title, MessageBoxButton.OK, img);

        /// <summary>
        /// 显示提示框
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="title">标题</param>
        /// <returns>状态</returns>
        public static async Task<bool> Show(string content, string title) => await Show(content, title, MessageBoxButton.OK, MessageBoxImage.Information);

        /// <summary>
        /// 显示提示框
        /// </summary>
        /// <param name="content">内容</param>
        /// <returns>状态</returns>
        public static async Task<bool> Show(string content) => await Show(content, string.Empty, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}