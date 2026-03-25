using MaterialDesignThemes.Wpf;
using Snet.Utility;
using System.Collections.Concurrent;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MessageBoxButton = Snet.Windows.Controls.@enum.MessageBoxButton;
using MessageBoxImage = Snet.Windows.Controls.@enum.MessageBoxImage;

namespace Snet.Windows.Controls.message
{
    /// <summary>
    /// 自定义消息框组件<br/>
    /// 基于 MaterialDesign DialogHost 实现，支持多种按钮和图标组合<br/>
    /// 提供异步显示和结果返回功能
    /// </summary>
    public class MessageBox
    {
        /// <summary>
        /// 根据消息图标类型返回对应的系统图标图像源<br/>
        /// 将枚举值映射为 Windows 系统图标并转换为 WPF 可用的 ImageSource
        /// </summary>
        /// <param name="iconType">消息图标类型枚举</param>
        /// <returns>对应的系统图标图像源，无匹配时返回 null</returns>
        private static ImageSource? SetIcon(MessageBoxImage iconType)
        {
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
                _ => null
            };
        }

        /// <summary>
        /// 图标缓存字典<br/>
        /// 缓存已转换的系统图标，避免重复创建 BitmapSource 对象
        /// </summary>
        private static readonly ConcurrentDictionary<Icon, BitmapSource> IconArry = new();

        /// <summary>
        /// 将系统图标转换为 WPF 的 BitmapSource<br/>
        /// 使用缓存避免重复转换，注意不释放静态 SystemIcons 实例
        /// </summary>
        /// <param name="ic">系统图标实例（SystemIcons 静态属性，不可释放）</param>
        /// <returns>对应的 BitmapSource 图像</returns>
        private static BitmapSource CreateIconBitmap(Icon ic)
        {
            return IconArry.GetOrAdd(ic, static icon =>
            {
                var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                bitmapSource.Freeze();
                return bitmapSource;
            });
        }

        /// <summary>
        /// DialogHost 标识符，对应 XAML 中的 DialogHost.Identifier
        /// </summary>
        private const string SignPosition = "DialogHost";

        /// <summary>确认按钮对话框实例</summary>
        private static readonly OK oK = new();
        /// <summary>确认/取消按钮对话框实例</summary>
        private static readonly OKCancel oKCancel = new();
        /// <summary>是按钮对话框实例</summary>
        private static readonly Yes yes = new();
        /// <summary>是/否按钮对话框实例</summary>
        private static readonly YesNo yesNo = new();

        /// <summary>
        /// 显示提示框（完整参数版）<br/>
        /// 在 UI 线程上异步显示 DialogHost 对话框，并返回用户操作结果
        /// </summary>
        /// <param name="content">消息内容文本</param>
        /// <param name="title">对话框标题</param>
        /// <param name="btn">按钮类型（OK/OKCancel/Yes/YesNo）</param>
        /// <param name="img">图标类型</param>
        /// <returns>用户确认返回 true，取消或关闭返回 false</returns>
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
        /// 显示提示框（不带图标，默认 Information）<br/>
        /// 简化调用，自动使用 Information 图标
        /// </summary>
        /// <param name="content">消息内容文本</param>
        /// <param name="title">对话框标题</param>
        /// <param name="btn">按钮类型</param>
        /// <returns>用户确认返回 true，取消返回 false</returns>
        public static async Task<bool> Show(string content, string title, MessageBoxButton btn) => await Show(content, title, btn, MessageBoxImage.Information);

        /// <summary>
        /// 显示提示框（不带按钮选择，默认 OK）<br/>
        /// 简化调用，自动使用 OK 按钮
        /// </summary>
        /// <param name="content">消息内容文本</param>
        /// <param name="title">对话框标题</param>
        /// <param name="img">图标类型</param>
        /// <returns>用户确认返回 true，取消返回 false</returns>
        public static async Task<bool> Show(string content, string title, MessageBoxImage img) => await Show(content, title, MessageBoxButton.OK, img);

        /// <summary>
        /// 显示提示框（默认 OK 按钮 + Information 图标）<br/>
        /// 最常用的简化调用方式
        /// </summary>
        /// <param name="content">消息内容文本</param>
        /// <param name="title">对话框标题</param>
        /// <returns>用户确认返回 true，取消返回 false</returns>
        public static async Task<bool> Show(string content, string title) => await Show(content, title, MessageBoxButton.OK, MessageBoxImage.Information);

        /// <summary>
        /// 显示提示框（仅内容，无标题）<br/>
        /// 最简单的调用方式，只显示消息内容
        /// </summary>
        /// <param name="content">消息内容文本</param>
        /// <returns>用户确认返回 true，取消返回 false</returns>
        public static async Task<bool> Show(string content) => await Show(content, string.Empty, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}