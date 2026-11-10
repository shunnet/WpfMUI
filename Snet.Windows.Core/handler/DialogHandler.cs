using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Threading;

namespace Snet.Windows.Core.handler
{
    /// <summary>
    /// 对话框处理器：把对话框显示在 <see cref="WindowBase"/> 的 DialogHost 里，并且让同一个窗口的对话框排队显示。<br/>
    /// 同一个 DialogHost 同一时刻只能显示一个对话框，直接调用 <c>DialogHost.Show</c> 时，第二个对话框会抛出 InvalidOperationException（DialogHost is already open）。
    /// 通过 <see cref="ShowAsync(object, DependencyObject?, CancellationToken)"/> 显示的对话框——包括库自带的 <c>MessageBox</c>——共用同一个先进先出的队列：
    /// 前一个对话框关闭之后下一个才显示，后到的调用不会抛异常。不同窗口的对话框互不影响，可以同时显示。<br/>
    /// 这个类没有静态构造函数：没有任何 <see cref="WindowBase"/> 的应用调用它，不会触发 <see cref="WindowBase"/> 的类型初始化（合并主题、写皮肤配置）。
    /// </summary>
    public static class DialogHandler
    {
        /// <summary>
        /// 在 UI 线程上把 <paramref name="content"/> 显示在目标窗口的 DialogHost 里，同一个窗口的并发调用按调用顺序排队，并返回对话框的结果。<br/>
        /// <b>目标窗口</b>：<paramref name="element"/> 所在的 <see cref="WindowBase"/>；为 null、或不在 <see cref="WindowBase"/> 里时用当前活动窗口，再回退到持有传统名称 <c>DialogHost</c> 的窗口
        /// （与 <see cref="WindowBase.GetDialogHostIdentifier"/> 的规则一致）。没有任何已加载的 <see cref="WindowBase"/> 时回退到应用自己的、标识符为 <c>DialogHost</c> 的 DialogHost，
        /// 此时不排队（没有窗口的生命周期可以观察），行为和直接调用 <c>DialogHost.Show(content, "DialogHost")</c> 一样。<br/>
        /// <b>结果</b>：对话框关闭时 <c>DialogHost.CloseDialogCommand</c>（或 <c>DialogHost.Close</c>）传入的参数。<b>null 表示“没有得到回答”</b>：
        /// 对话框关闭时没有传参数，或者对话框所属窗口在对话框显示期间、排队等待期间被关闭，或者窗口在对话框显示期间重新套用了模板（换肤时窗口换了样式，旧模板里的 DialogHost 随之消失）。
        /// 这几种情况都没有用户的回答，调用方应当一样处理（通常按“取消”）；窗口关闭后宿主随之消失，<c>DialogHost.Show</c> 的任务永远不会完成，不能一直等下去。<br/>
        /// <b>异常</b>：调用经调度器到达 UI 线程时（<c>Loaded</c> 优先级，所以刚刚 <c>Show()</c> 的窗口已经加载完成），<paramref name="element"/> 所在的窗口已经关闭、或者还没有显示过，
        /// 抛出 InvalidOperationException（没有可用的 DialogHost）——包括调用之后、到达 UI 线程之前关闭的窗口；通过这个检查之后窗口才关闭，按上面的规则返回 null。
        /// <paramref name="cancellationToken"/> 在排队等待期间被取消时任务被取消，不占用队列。<br/>
        /// <b>线程</b>：可以从任意线程调用，对话框在 UI 线程上显示；<paramref name="content"/> 必须是 UI 线程上创建的元素——要在后台线程调用、并让内容在 UI 线程上创建，请用 <see cref="ShowAsync(Func{object}, DependencyObject?, CancellationToken)"/>。
        /// <paramref name="element"/> 必须是 UI 线程（<c>Application</c> 所在的线程）上创建的元素。<br/>
        /// 注意：排队没有上限；取消只影响排队等待，对话框一旦显示，取消不会关闭它；用 <c>DialogHost.Show</c> 直接打开的对话框不在队列里，它打开期间调用本方法仍然会抛出 “DialogHost is already open”；
        /// 也不要在对话框的事件处理里等待另一个对话框：后者排在前者后面，而前者在等后者，只有用户先把前者关掉才能继续。
        /// </summary>
        /// <param name="content">对话框内容</param>
        /// <param name="element">发起对话框的界面元素（如视图），用来确定显示在哪个窗口；为 null 时用活动窗口</param>
        /// <param name="cancellationToken">取消排队等待</param>
        /// <returns>对话框的结果；没有得到回答（关闭时没有传参数，或窗口在对话框显示或排队期间被关闭/换了模板）时为 null</returns>
        /// <exception cref="ArgumentNullException"><paramref name="content"/> 为 null</exception>
        /// <exception cref="ArgumentException"><paramref name="content"/> 是委托：委托不是对话框内容。异步 lambda（<c>async () =&gt; ...</c>）会绑定到本重载，要延迟创建内容请传入同步的 <c>Func&lt;object&gt;</c>，异步创建的内容请先 await 出来再传入</exception>
        /// <exception cref="InvalidOperationException">没有 Application 对象；或目标窗口已经关闭/尚未显示；或没有可用的 DialogHost</exception>
        public static Task<object?> ShowAsync(object content, DependencyObject? element = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);
            if (content is Delegate)
            {
                throw new ArgumentException("对话框内容不能是委托：要延迟创建内容请传入 Func<object>；异步创建的内容请先 await 出来再传入。", nameof(content));
            }

            return ShowAsync(() => content, element, cancellationToken);
        }

        /// <summary>
        /// 同 <see cref="ShowAsync(object, DependencyObject?, CancellationToken)"/>，但对话框内容由工厂在<b>轮到自己显示时</b>在 UI 线程上创建：
        /// 排队期间不创建内容，也可以从后台线程调用。工厂抛出的异常会作为任务的异常抛出，并且不会占用队列。
        /// </summary>
        /// <param name="contentFactory">创建对话框内容的工厂（在 UI 线程上调用）</param>
        /// <param name="element">发起对话框的界面元素（如视图），用来确定显示在哪个窗口；为 null 时用活动窗口</param>
        /// <param name="cancellationToken">取消排队等待</param>
        /// <returns>对话框的结果；没有得到回答（关闭时没有传参数，或窗口在对话框显示或排队期间被关闭/换了模板）时为 null</returns>
        /// <exception cref="ArgumentNullException"><paramref name="contentFactory"/> 为 null</exception>
        /// <exception cref="InvalidOperationException">没有 Application 对象；或目标窗口已经关闭/尚未显示；或没有可用的 DialogHost</exception>
        public static Task<object?> ShowAsync(Func<object> contentFactory, DependencyObject? element = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(contentFactory);
            Dispatcher dispatcher = Application.Current?.Dispatcher
                ?? throw new InvalidOperationException("应用程序对象（Application.Current）不存在，无法显示对话框。");

            // 先跳到 UI 线程、以 Loaded 优先级排队：刚刚调用过 Show() 的窗口还没有触发 Loaded 事件，宿主还没有加载，
            // 排在它的 Loaded 之后才能显示；所有调用都经过同一次跳转，进入队列的先后就是调用的先后
            return dispatcher.InvokeAsync(
                () => ShowOnUiThreadAsync(contentFactory, element, cancellationToken),
                DispatcherPriority.Loaded,
                cancellationToken).Task.Unwrap();
        }

        /// <summary>
        /// 在 UI 线程上确定目标窗口、排队并显示
        /// </summary>
        private static async Task<object?> ShowOnUiThreadAsync(Func<object> contentFactory, DependencyObject? element, CancellationToken cancellationToken)
        {
            WindowBase? window = DialogHostRegistry.ResolveWindow(element);
            if (window == null)
            {
                // 没有 WindowBase（应用自己的、标识符为 DialogHost 的 DialogHost）：没有窗口的生命周期可以观察。宿主随窗口一起消失时，
                // 持有门的调用永远等不到对话框结束，会把后面的调用永远挡在门外——所以这种情况不排队，行为和直接 DialogHost.Show 一样
                cancellationToken.ThrowIfCancellationRequested();
                return await DialogHost.Show(contentFactory(), DialogHostRegistry.LegacyIdentifier);
            }

            // 窗口已经关闭、或者还没有加载：没有可用的 DialogHost，必须在这里就失败。
            // 已关闭窗口的 DialogHostIdentifier 可能还是传统名称，而传统名称这时已经移交给了别的窗口，直接显示会显示到别的窗口的 DialogHost 里。
            // ShowGate.IsClosed 覆盖 Closed 已经触发、但 IsLoaded 还没来得及复位的短暂间隙
            DialogGate gate = window.ShowGate;
            if (!window.IsLoaded || gate.IsClosed)
            {
                throw new InvalidOperationException("对话框所属窗口已经关闭，或者还没有加载：没有可用的 DialogHost。");
            }

            await gate.EnterAsync(cancellationToken);
            try
            {
                // 排队期间窗口被关闭了：没有宿主可以显示，按“关闭”处理
                if (gate.IsClosed)
                {
                    return null;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // 轮到自己时再创建内容、再取窗口当前的标识符（传统名称可能在排队期间移交给了别的窗口）
                Task hostLost = gate.HostLostTask;
                Task<object?> show = DialogHost.Show(contentFactory(), window.DialogHostIdentifier);

                // 宿主在对话框显示期间消失（窗口被关闭，或者窗口重新套用了模板、旧模板里的 DialogHost 被卸载）：上面的任务永远不会完成，不能一直等下去
                await Task.WhenAny(show, hostLost);
                if (!show.IsCompleted)
                {
                    ObserveFault(show);
                    return null;
                }

                return await show;
            }
            finally
            {
                gate.Exit();
            }
        }

        /// <summary>
        /// 放弃等待一个可能永远不会完成的任务之后，仍然观察它的异常，避免未观察到的任务异常
        /// </summary>
        /// <param name="task">被放弃的任务</param>
        private static void ObserveFault(Task task) =>
            task.ContinueWith(
                static t => { _ = t.Exception; },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
    }
}
