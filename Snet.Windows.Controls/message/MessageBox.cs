using MaterialDesignThemes.Wpf;
using Snet.Utility;
using Snet.Windows.Core;
using Snet.Windows.Core.handler;
using System.Collections.Concurrent;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        /// 显示提示框（完整参数版，显示在当前活动窗口）<br/>
        /// 在 UI 线程上异步显示 DialogHost 对话框，并返回用户操作结果。<br/>
        /// 同时打开多个 <see cref="WindowBase"/> 时，每个窗口的 DialogHost 标识符各不相同；
        /// 本重载不指定窗口：显示在当前活动窗口里，没有活动窗口时显示在持有传统名称 <c>DialogHost</c> 的窗口里。需要指定窗口请用带 owner 的重载。<br/>
        /// 同一个窗口里的并发调用按调用顺序排队，详见带 owner 的重载。
        /// </summary>
        /// <param name="content">消息内容文本</param>
        /// <param name="title">对话框标题</param>
        /// <param name="btn">按钮类型（OK/OKCancel/Yes/YesNo）</param>
        /// <param name="img">图标类型</param>
        /// <returns>用户确认返回 true，取消或关闭返回 false</returns>
        public static Task<bool> Show(string content, string title, MessageBoxButton btn, MessageBoxImage img) => Show(content, title, btn, img, null);

        /// <summary>
        /// 显示提示框（可指定所属窗口）<br/>
        /// 在 UI 线程上异步显示 DialogHost 对话框，并返回用户操作结果；可以从任意线程调用，但 <paramref name="owner"/> 必须是 UI 线程（<c>Application</c> 所在的线程）上创建的窗口。<br/>
        /// <b>同一个窗口（同一个 DialogHost）同一时刻只能显示一个对话框</b>：对同一个 <see cref="WindowBase"/> 的并发调用按调用顺序排队，
        /// 前一个对话框关闭之后下一个才显示（此前后到的调用会抛出 InvalidOperationException：DialogHost is already open）；
        /// 不同窗口的对话框可以同时显示。排队没有上限、也不能取消：请不要在循环里无节制地调用（例如错误处理循环里每次都弹一个），必要时自己做节流或去重。<br/>
        /// 对话框所属窗口在对话框显示期间、或在排队等待期间被关闭，或者窗口在对话框显示期间重新套用了模板（例如换肤使窗口换了样式）时，调用按“关闭”处理，返回 <c>false</c>
        /// （此前对话框随窗口或旧模板一起消失，返回的任务永远不会完成）。调用到达 UI 线程时所属窗口已经关闭、或者还没有显示过的，抛出 InvalidOperationException（没有可用的 DialogHost）。<br/>
        /// 排队由 <see cref="DialogHandler.ShowAsync(Func{object}, DependencyObject?, CancellationToken)"/> 完成：消息框和通过它显示的其它对话框共用同一个队列。
        /// 限制：没有 <see cref="WindowBase"/>（应用自己的、标识符为 <c>DialogHost</c> 的 DialogHost）时没有窗口的生命周期可以观察，不排队，并发调用仍然会抛出 “DialogHost is already open”；
        /// 用 <c>DialogHost.Show</c> 直接打开的对话框也不在队列里，它打开期间调用本方法同样会抛出该异常——请改用 <see cref="DialogHandler.ShowAsync(object, DependencyObject?, CancellationToken)"/>。
        /// 也不要在消息框的事件处理里等待另一个消息框：后者排在前者后面，而前者在等后者，只有用户先把前者关掉才能继续。
        /// </summary>
        /// <param name="content">消息内容文本</param>
        /// <param name="title">对话框标题</param>
        /// <param name="btn">按钮类型（OK/OKCancel/Yes/YesNo）</param>
        /// <param name="img">图标类型</param>
        /// <param name="owner">对话框所属窗口；为 null、或不是 <see cref="WindowBase"/>（没有 DialogHost）时回退到当前活动窗口</param>
        /// <returns>用户确认返回 true，取消或关闭（包括所属窗口被关闭）返回 false</returns>
        public static async Task<bool> Show(string content, string title, MessageBoxButton btn, MessageBoxImage img, Window? owner)
        {
            // 目标窗口的确定、排队、窗口关闭的处理都在 DialogHandler.ShowAsync 里。对话框内容在轮到自己时才在 UI 线程上创建：
            // MessageModel 的构造会读取 Application.Current.MainWindow，只能在 UI 线程上访问——所以本方法可以从任意线程调用
            object? result = await DialogHandler.ShowAsync(() => CreateDialogContent(content, title, btn, img), owner);

            // 获取对话框结果并返回
            return result?.GetSource<bool>() ?? false;
        }

        /// <summary>
        /// 创建消息框的内容控件（必须在 UI 线程上调用）
        /// </summary>
        /// <param name="content">消息内容文本</param>
        /// <param name="title">对话框标题</param>
        /// <param name="btn">按钮类型</param>
        /// <param name="img">图标类型</param>
        /// <returns>设置好数据上下文的内容控件</returns>
        private static FrameworkElement CreateDialogContent(string content, string title, MessageBoxButton btn, MessageBoxImage img)
        {
            var messageModel = new MessageModel
            {
                ContentIcon = SetIcon(img),
                Content = content,
                Title = title
            };

            // 根据按钮类型创建对话框内容。每次调用都创建新的控件实例：
            // 不同窗口的消息框可能同时显示，同一个控件实例不能同时出现在两个 DialogHost 里（也不能被后到的调用覆盖 DataContext）
            FrameworkElement dialogContent = btn switch
            {
                MessageBoxButton.OKCancel => new OKCancel(),
                MessageBoxButton.Yes => new Yes(),
                MessageBoxButton.YesNo => new YesNo(),
                _ => new OK()
            };
            dialogContent.DataContext = messageModel;
            return dialogContent;
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