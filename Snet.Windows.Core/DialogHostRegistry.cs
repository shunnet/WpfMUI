using MaterialDesignThemes.Wpf;
using System.Windows;

namespace Snet.Windows.Core
{
    /// <summary>
    /// 管理各个 <see cref="WindowBase"/> 的 DialogHost 标识符。<br/>
    /// MaterialDesign 的 <c>DialogHost.Show(content, identifier)</c> 要求标识符在进程内唯一：同时存在两个标识符相同的、已加载的 DialogHost 时，
    /// 任何一次显示都会抛出 "Multiple viable DialogHosts"。因此每个窗口都有自己唯一的标识符（<c>DialogHost#序号</c>）。<br/>
    /// 为了兼容单窗口时代的写法 <c>DialogHost.Show(content, "DialogHost")</c>，传统名称 <c>DialogHost</c>（及 <c>DialogHost_ClickClose</c>）
    /// 始终归"最早创建且已加载并可见"的那个窗口所有，它关闭或隐藏后自动移交给下一个窗口——先关闭登录窗口再打开主窗口的常见写法因此不受影响
    /// （持有者的 DialogHost 里正开着对话框时，名称暂不移交，直到下一次刷新）。<br/>
    /// 注册表只在 Application 所在的 UI 线程上维护（不需要锁）；在其它线程创建的窗口只保留自己的唯一标识符，不参与传统名称的归属。
    /// </summary>
    internal static class DialogHostRegistry
    {
        /// <summary>传统（单窗口时代）的 DialogHost 标识符</summary>
        internal const string LegacyIdentifier = "DialogHost";

        /// <summary>"点击空白处可关闭"的第二个 DialogHost 的标识符后缀</summary>
        internal const string ClickCloseSuffix = "_ClickClose";

        /// <summary>唯一标识符序号（原子递增，进程内不重复）</summary>
        private static int _nextId;

        /// <summary>按创建顺序记录的窗口（弱引用，不阻止窗口被回收）；仅在 UI 线程访问</summary>
        private static readonly List<WeakReference<WindowBase>> Windows = new();

        /// <summary>生成一个进程内唯一的 DialogHost 标识符</summary>
        /// <returns>形如 <c>DialogHost#3</c> 的标识符</returns>
        internal static string NewUniqueIdentifier() => LegacyIdentifier + "#" + Interlocked.Increment(ref _nextId);

        /// <summary>当前线程是否是 Application 所在的 UI 线程</summary>
        private static bool OnApplicationThread()
        {
            var app = Application.Current;
            return app != null && app.Dispatcher.CheckAccess();
        }

        /// <summary>登记一个新创建的窗口（按创建顺序）</summary>
        /// <param name="window">窗口</param>
        internal static void Register(WindowBase window)
        {
            if (!OnApplicationThread()) return;
            Windows.Add(new WeakReference<WindowBase>(window));
        }

        /// <summary>窗口关闭后移除登记，并把传统名称移交给下一个窗口</summary>
        /// <param name="window">已关闭的窗口</param>
        internal static void Unregister(WindowBase window)
        {
            if (!OnApplicationThread()) return;
            Windows.RemoveAll(w => !w.TryGetTarget(out var target) || ReferenceEquals(target, window));
            Refresh();
        }

        /// <summary>
        /// 重新分配标识符：传统名称给"最早创建且已加载并可见"的窗口（没有可见窗口时给最早已加载的窗口），其余窗口使用各自的唯一标识符。
        /// 窗口加载、关闭、可见性变化时调用。<br/>
        /// 例外：当前持有传统名称的窗口里正开着对话框时，名称不换人（见 <see cref="HolderWithOpenDialog"/>）。
        /// </summary>
        internal static void Refresh()
        {
            if (!OnApplicationThread()) return;

            Windows.RemoveAll(w => !w.TryGetTarget(out _));
            WindowBase? primary = HolderWithOpenDialog() ?? PickPrimary();
            foreach (var weak in Windows)
            {
                if (weak.TryGetTarget(out var window))
                {
                    window.ApplyDialogHostIdentifier(ReferenceEquals(window, primary) ? LegacyIdentifier : window.DialogHostUniqueIdentifier);
                }
            }
        }

        /// <summary>
        /// 当前持有传统名称的窗口、并且它的 DialogHost 里正开着对话框时返回该窗口，否则返回 <c>null</c>。<br/>
        /// 名称在对话框打开期间不能换人：正在使用传统名称的代码（<c>DialogHost.Close("DialogHost")</c>、<c>IsDialogOpen</c>、
        /// <c>GetDialogSession</c>）会按标识符重新查找宿主，名称一旦移交给别的窗口，它们就落到别的窗口的宿主上
        /// （抛 "DialogHost is not open"，或者误关别的窗口的对话框）。
        /// </summary>
        private static WindowBase? HolderWithOpenDialog()
        {
            foreach (var weak in Windows)
            {
                if (!weak.TryGetTarget(out var window) || !window.IsLoaded || window.DialogHostIdentifier != LegacyIdentifier) continue;
                return IsDialogOpen(LegacyIdentifier) || IsDialogOpen(LegacyIdentifier + ClickCloseSuffix) ? window : null;
            }
            return null;
        }

        /// <summary>指定标识符的 DialogHost 里是否开着对话框；没有加载的宿主或标识符不唯一时返回 <c>false</c></summary>
        private static bool IsDialogOpen(string identifier)
        {
            try
            {
                return DialogHost.IsDialogOpen(identifier);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>选出持有传统名称的窗口</summary>
        private static WindowBase? PickPrimary()
        {
            WindowBase? firstLoaded = null;
            foreach (var weak in Windows)
            {
                if (!weak.TryGetTarget(out var window) || !window.IsLoaded) continue;
                if (window.IsVisible) return window;
                firstLoaded ??= window;
            }
            return firstLoaded;
        }

        /// <summary>
        /// 解析应使用的 DialogHost 所在的窗口。
        /// </summary>
        /// <param name="element">界面元素；其所在窗口是 WindowBase 时返回该窗口（即使它已经关闭），否则回退到活动窗口、再到持有传统名称的窗口</param>
        /// <returns>窗口；没有任何已加载的 WindowBase 时返回 null</returns>
        internal static WindowBase? ResolveWindow(DependencyObject? element)
        {
            if (element != null && Window.GetWindow(element) is WindowBase owner)
            {
                return owner;
            }

            if (!OnApplicationThread()) return null;

            WindowBase? firstLoaded = null;
            foreach (var weak in Windows)
            {
                if (!weak.TryGetTarget(out var window) || !window.IsLoaded) continue;
                if (window.IsActive) return window;
                firstLoaded ??= window;
            }

            return PickPrimary() ?? firstLoaded;
        }

        /// <summary>
        /// 解析应使用的 DialogHost 标识符。
        /// </summary>
        /// <param name="element">界面元素；其所在窗口是 WindowBase 时返回该窗口的标识符，否则回退到活动窗口、再到持有传统名称的窗口</param>
        /// <returns>标识符；没有任何已加载的 WindowBase 时返回传统名称</returns>
        internal static string Resolve(DependencyObject? element) => ResolveWindow(element)?.DialogHostIdentifier ?? LegacyIdentifier;
    }
}
