using Snet.Core.extend;
using Snet.Log;
using Snet.Model.data;
using System.Collections.Concurrent;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace Snet.Windows.Controls.handler
{
    /// <summary>
    /// 用于在WPF界面中高效显示实时日志消息的组件
    /// 通过后台线程收集日志，定期批量刷新到UI线程，避免频繁的界面更新导致的性能问题
    /// 支持多线程并发写入，自动内存管理，安全资源释放
    /// </summary>
    public sealed class UiMessageHandler : CoreUnify<UiMessageHandler, string>, IDisposable, IAsyncDisposable
    {
        // 线程安全的队列，用于存储待显示的日志消息
        // 多个线程可以同时向队列中添加消息，不会出现数据竞争问题
        private readonly ConcurrentQueue<string> _logQueue = new();

        // 用于控制后台日志刷新任务的取消信号
        // 当需要停止服务时，通过这个对象通知后台任务优雅退出
        private CancellationTokenSource? _logCts;

        // 后台任务的引用，用于监控任务状态和等待任务完成
        private Task? _logTask;

        // 保护启动/停止状态转换；0=活动，1=正在释放或已释放。
        private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
        private int _disposeState;

        // 字符串构建器缓存，预分配4KB容量以减少内存分配次数
        // 复用这个实例可以显著降低垃圾回收的压力
        private readonly StringBuilder _sbCache = new(4096);

        /// <summary>
        /// 用于累积 Info 内容的 StringBuilder，避免重复的字符串拼接分配
        /// </summary>
        private readonly StringBuilder _infoBuilder = new(4096);

        /// <summary>
        /// 获取或设置当前显示的日志内容
        /// 这个属性通常会被绑定到WPF界面的TextBox或TextBlock控件
        /// </summary>
        public string Info { get; private set; } = string.Empty;

        /// <summary>
        /// 创建新的日志处理器实例
        /// </summary>
        public UiMessageHandler() : base() { }

        /// <summary>
        /// 使用初始数据创建日志处理器实例
        /// </summary>
        /// <param name="basics">初始化时显示的基础文本内容</param>
        public UiMessageHandler(string basics) : base(basics) { }

        /// <summary>
        /// 检查日志刷新任务是否正在运行
        /// 返回true表示后台正在定期将日志刷新到界面
        /// </summary>
        public bool IsRunning => _logTask != null &&
                                 !_logTask.IsCompleted &&
                                 !_logTask.IsFaulted &&
                                 !_logTask.IsCanceled;

        private bool IsDisposed => Volatile.Read(ref _disposeState) != 0;

        /// <summary>
        /// 获取当前队列中等待处理的日志数量
        /// 这个值反映了尚未显示到界面的日志条目数
        /// </summary>
        public int PendingCount => _logQueue.Count;

        /// <summary>
        /// 启动后台日志刷新服务
        /// 服务启动后会自动定期将队列中的日志批量刷新到用户界面
        /// </summary>
        /// <param name="intervalMs">刷新间隔时间，单位毫秒。较小的值会使界面更新更及时，但会增加系统负载</param>
        /// <param name="maxLength">日志内容的最大长度。超过此长度时会自动清空旧内容，防止内存无限增长</param>
        /// <param name="maxBatchCount">每次最大出队日志条数，防止极端情况下 UI 卡顿</param>
        /// <param name="cancellationToken">用于取消等待启动锁的令牌</param>
        /// <exception cref="ObjectDisposedException">如果对象已被释放，调用此方法会抛出异常</exception>
        public async Task StartAsync(int intervalMs = 200, int maxLength = 10000, int maxBatchCount = 1000, CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(intervalMs, 0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxLength, 0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxBatchCount, 0);

            await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);
                if (IsRunning)
                {
                    return;
                }

                _logCts?.Dispose();
                _logCts = new CancellationTokenSource();
                _logTask = RunLoopAsync(intervalMs, maxLength, maxBatchCount, _logCts.Token);
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }

        private async Task RunLoopAsync(int intervalMs, int maxLength, int maxBatchCount, CancellationToken token)
        {
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMs));
                while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                {
                    FlushToUI(maxLength, maxBatchCount);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[UiMessageHandler] 后台循环发生异常：{ex}", foldername: "UiMessageHandler");
            }
        }

        /// <summary>
        /// 安全停止后台日志刷新服务
        /// 会等待当前正在处理的日志完成刷新，然后停止服务
        /// </summary>
        public async Task StopAsync()
        {
            await StopCoreAsync().ConfigureAwait(false);
        }

        private async Task StopCoreAsync()
        {
            CancellationTokenSource? cancellationSource;
            Task? logTask;

            await _lifecycleGate.WaitAsync().ConfigureAwait(false);
            try
            {
                cancellationSource = _logCts;
                logTask = _logTask;
                _logCts = null;
                _logTask = null;
            }
            finally
            {
                _lifecycleGate.Release();
            }

            if (cancellationSource is null)
            {
                return;
            }

            try
            {
                await cancellationSource.CancelAsync().ConfigureAwait(false);
                if (logTask is not null)
                {
                    await logTask.ConfigureAwait(false);
                }
                if (!_logQueue.IsEmpty && !IsDisposed)
                {
                    FlushToUI();
                }
            }
            finally
            {
                cancellationSource.Dispose();
            }
        }

        /// <summary>
        /// 向日志系统添加一条新消息
        /// 此方法是线程安全的，可以在任何线程中调用
        /// </summary>
        /// <param name="msg">要显示的日志消息内容</param>
        /// <param name="dateTime">日志时间戳，如果为null则使用当前时间</param>
        /// <param name="withTime">是否在消息前添加时间前缀</param>
        /// <returns>表示异步操作完成的任务</returns>
        public Task ShowAsync(string msg, DateTime? dateTime = null, bool withTime = true)
        {
            // 安全检查：如果服务已停止或正在停止，不再接受新日志
            var cancellationSource = _logCts;
            if (IsDisposed || cancellationSource == null || cancellationSource.IsCancellationRequested)
                return Task.CompletedTask;

            // 忽略空消息
            if (string.IsNullOrWhiteSpace(msg))
                return Task.CompletedTask;

            // 处理时间戳
            dateTime ??= DateTime.Now;

            // 格式化日志行：可选择是否包含时间戳
            string log = withTime
                ? $"{dateTime:yyyy-MM-dd HH:mm:ss.ffffff} ：{msg}\r\n"
                : $"{msg}\r\n";

            // 将日志加入队列，等待后台任务处理
            _logQueue.Enqueue(log);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 内部方法：将队列中的日志批量刷新到用户界面
        /// 这个方法运行在后台线程中，负责收集日志并调度到UI线程显示
        /// </summary>
        /// <param name="maxLength">日志内容的最大长度限制</param>
        /// <param name="maxBatchCount">每次最大出队日志条数，防止极端情况下 UI 卡顿</param>
        private void FlushToUI(int maxLength = 10000, int maxBatchCount = 1000)
        {
            // 如果队列为空，无需处理
            if (_logQueue.IsEmpty)
                return;

            // 清空字符串构建器，准备构建新的日志内容
            _sbCache.Clear();
            int processed = 0;

            // 批量从队列中取出日志，限制单次处理数量避免长时间阻塞
            while (_logQueue.TryDequeue(out var line))
            {
                _sbCache.Append(line);
                if (++processed > maxBatchCount)  // 单次最多处理日志
                    break;
            }

            // 如果没有处理任何日志，直接返回
            if (_sbCache.Length == 0)
                return;

            // 获取合并后的日志文本
            string merged = _sbCache.ToString();

            // 获取WPF的UI线程调度器
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is not null)
            {
                // 在UI线程上异步执行界面更新，使用Background优先级减少对用户操作的影响
                dispatcher.BeginInvoke(() =>
                {
                    // 再次检查对象是否已被释放
                    if (IsDisposed) return;
                    // 实际更新界面内容
                    AppendInfo(merged, maxLength);
                }, DispatcherPriority.Render);
            }
            else
            {
                // 如果没有UI环境（如控制台应用），直接更新内容
                AppendInfo(merged, maxLength);
            }
        }

        /// <summary>
        /// 实际更新界面显示内容的方法
        /// 这个方法必须在UI线程中调用
        /// </summary>
        /// <param name="text">要追加的新日志内容</param>
        /// <param name="maxLength">内容的最大长度限制</param>
        private void AppendInfo(string text, int maxLength)
        {
            // 安全检查
            if (IsDisposed) return;

            // 内存管理：如果内容超过最大长度，清空旧内容
            if (_infoBuilder.Length + text.Length > maxLength)
                _infoBuilder.Clear();

            // 追加新内容（使用 StringBuilder 避免重复的字符串分配）
            _infoBuilder.Append(text);
            string newInfo = _infoBuilder.ToString();

            // 内容确实变化时才赋值并触发事件，避免重复 ToString 分配与无效刷新
            if (!string.Equals(newInfo, Info, StringComparison.Ordinal))
            {
                Info = newInfo;
                // 触发事件，通知订阅者内容已更新
                OnInfoEventHandler(this, EventInfoResult.CreateSuccessResult(Info));
            }
        }

        /// <summary>
        /// 清空所有日志内容
        /// 包括队列中的待处理日志和当前显示的内容
        /// </summary>
        public async Task ClearAsync()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            await ClearCoreAsync().ConfigureAwait(false);
        }

        private async Task ClearCoreAsync()
        {
            // 清空队列
            while (_logQueue.TryDequeue(out _)) { }

            var dispatcher = Application.Current?.Dispatcher;
            bool canMarshal = dispatcher is not null
                && !dispatcher.CheckAccess()
                && !dispatcher.HasShutdownStarted
                && !dispatcher.HasShutdownFinished;

            if (canMarshal)
            {
                // 修改 Info 和触发事件前先封送到 UI 线程，避免后台线程直接修改绑定属性
                await dispatcher.InvokeAsync(() =>
                {
                    _sbCache.Clear();
                    _infoBuilder.Clear();

                    // 内容确实变化时才赋值并触发事件
                    if (!string.IsNullOrEmpty(Info))
                    {
                        Info = string.Empty;
                        OnInfoEventHandler(this, EventInfoResult.CreateSuccessResult(""));
                    }
                });
            }
            else
            {
                // 已经在 UI 线程、没有 UI 环境或调度器已关闭，直接更新内容
                _sbCache.Clear();
                _infoBuilder.Clear();
                if (!string.IsNullOrEmpty(Info))
                {
                    Info = string.Empty;
                }

                // 通知订阅者内容已清空
                await OnInfoEventHandlerAsync(this, EventInfoResult.CreateSuccessResult(""));
            }
        }

        private void ClearSynchronously()
        {
            while (_logQueue.TryDequeue(out _)) { }
            _sbCache.Clear();
            _infoBuilder.Clear();
            if (!string.IsNullOrEmpty(Info))
            {
                Info = string.Empty;
                OnInfoEventHandler(this, EventInfoResult.CreateSuccessResult(string.Empty));
            }
        }

        /// <summary>
        /// 异步释放对象占用的所有资源
        /// 这是推荐的释放方式，特别是当对象正在运行后台任务时
        /// </summary>
        public override async ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _disposeState, 1, 0) != 0) return;

            try
            {
                await StopCoreAsync().ConfigureAwait(false);
                await ClearCoreAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 记录释放过程中发生的异常，但不抛出
                LogHelper.Error($"[UiMessageHandler.DisposeAsync] 释放资源时发生异常：{ex}", foldername: "UiMessageHandler");
            }
            finally
            {
                // 基类释放必须执行：负责从静态 LanguageHandler 事件退订，避免事件泄漏
                try
                {
                    await base.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"[UiMessageHandler.DisposeAsync] 基类释放异常：{ex}", foldername: "UiMessageHandler");
                }
                _lifecycleGate.Dispose();
            }
        }

        /// <summary>
        /// 同步释放对象占用的所有资源
        /// 这是为了兼容IDisposable接口，推荐在非异步上下文中使用
        /// </summary>
        public override void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposeState, 1, 0) != 0) return;

            try
            {
                // 非阻塞停止：只解绑并取消后台任务，不等待其退出，
                // 避免在 UI 线程 GetAwaiter().GetResult()/Dispatcher.Invoke 阻塞导致死锁
                // （后台循环自身会响应取消并优雅退出，异常均在 RunLoop 内部捕获）。
                CancellationTokenSource? cts = _logCts;
                Task? logTask = _logTask;
                _logCts = null;
                _logTask = null;
                if (cts is not null)
                {
                    try
                    {
                        cts.Cancel();
                    }
                    catch (ObjectDisposedException) { }
                    finally
                    {
                        cts.Dispose();
                    }
                }
                _ = logTask; // 仅解绑引用；任务结束由运行循环自行完成

                // 清空显示内容：可直接同步执行的路径才同步，否则投递到 UI 线程异步执行
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher is null || dispatcher.CheckAccess())
                {
                    ClearSynchronously();
                }
                else
                {
                    try
                    {
                        dispatcher.BeginInvoke(new Action(ClearSynchronously));
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"[UiMessageHandler.Dispose] 投递清理任务失败：{ex}", foldername: "UiMessageHandler");
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录释放过程中发生的异常
                LogHelper.Error($"[UiMessageHandler.Dispose] 释放资源时发生异常：{ex}", foldername: "UiMessageHandler");
            }
            finally
            {
                // 基类释放必须执行：负责从静态 LanguageHandler 事件退订，避免事件泄漏
                try
                {
                    base.Dispose();
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"[UiMessageHandler.Dispose] 基类释放异常：{ex}", foldername: "UiMessageHandler");
                }
                _lifecycleGate.Dispose();
            }
        }
    }
}
