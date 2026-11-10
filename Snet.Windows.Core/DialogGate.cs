namespace Snet.Windows.Core
{
    /// <summary>
    /// 异步显示门：同一个 DialogHost 同一时刻只能显示一个对话框（再显示会抛出 "DialogHost is already open"），
    /// 这个门把对同一个宿主的显示请求排成先进先出的队列，前一个对话框关闭之后下一个才开始显示。每个 <see cref="WindowBase"/> 一个（<see cref="WindowBase.ShowGate"/>）。<br/>
    /// 用异步互斥（<see cref="SemaphoreSlim"/>）而不是 <c>lock</c>：临界区要 <c>await</c> 用户的操作，可能持有很久，<c>lock</c> 不能跨越 <c>await</c>；
    /// 调用链本来就是异步的，等待者也不会阻塞 UI 线程；<see cref="SemaphoreSlim.WaitAsync(CancellationToken)"/> 的异步等待者按先进先出被放行，等待可以被取消。<br/>
    /// 宿主消失时：窗口关闭调用 <see cref="Close"/>，窗口重新套用了模板（换肤时窗口换了样式、或者调用方改了 Template/Style，旧模板里的 DialogHost 随之卸载）调用 <see cref="HostReplaced"/>。
    /// 宿主消失之后，正在显示的 <c>DialogHost.Show</c> 返回的任务永远不会完成，持有门的调用据此（<see cref="HostLostTask"/>）放弃等待，并通过 <see cref="Exit"/> 依次放走排在后面的调用；
    /// 窗口关闭时，它们进门后用 <see cref="IsClosed"/> 发现窗口已经关闭，直接结束，不会去显示；窗口只是换了模板时，它们在新的宿主里显示。<br/>
    /// 门只在 UI 线程上使用（<see cref="IsClosed"/> 和宿主消失信号的读写不需要同步）。
    /// </summary>
    internal sealed class DialogGate
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private TaskCompletionSource _hostLost = NewSignal();

        /// <summary>宿主所属的窗口是否已经关闭（只在 UI 线程上读写）</summary>
        public bool IsClosed { get; private set; }

        /// <summary>
        /// 宿主下一次消失时完成的任务：每次显示对话框之前取一次（窗口换过模板之后取到的是新任务；窗口关闭之后取到的永远是已完成的任务）。只在 UI 线程上读
        /// </summary>
        public Task HostLostTask => _hostLost.Task;

        /// <summary>
        /// 等待轮到自己显示（先进先出）。等待成功之后必须恰好调用一次 <see cref="Exit"/>；
        /// 等待被取消时抛出 <see cref="OperationCanceledException"/>，此时没有占用门，不能调用 <see cref="Exit"/>
        /// </summary>
        /// <param name="cancellationToken">取消等待（只影响排队，不影响已经显示的对话框）</param>
        /// <returns>轮到自己时完成的任务</returns>
        public Task EnterAsync(CancellationToken cancellationToken = default) => _semaphore.WaitAsync(cancellationToken);

        /// <summary>显示结束或放弃：放行排在后面的下一个调用</summary>
        public void Exit() => _semaphore.Release();

        /// <summary>
        /// 宿主被替换：窗口重新套用了模板，旧的 DialogHost 随旧模板一起卸载，正在显示的对话框随之消失。窗口还在，之后的对话框显示在新的宿主里
        /// </summary>
        public void HostReplaced()
        {
            if (IsClosed)
            {
                return;
            }

            TaskCompletionSource lost = _hostLost;
            _hostLost = NewSignal();
            lost.TrySetResult();
        }

        /// <summary>宿主所属的窗口已经关闭</summary>
        public void Close()
        {
            IsClosed = true;
            _hostLost.TrySetResult();
        }

        private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
