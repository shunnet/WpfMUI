using System.IO;
using System.Windows;
using System.Windows.Threading;
using Snet.Windows.Core.handler;

namespace Snet.Windows.Tests;

/// <summary>
/// 为依赖 <see cref="Application"/> / Dispatcher 的测试提供进程内唯一的 WPF 宿主线程。<br/>
/// WPF 的 Application 在同一个 AppDomain 内只能创建一次，且归创建它的线程所有，
/// 所以所有依赖 <c>Application.Current</c> 的测试都必须通过 <see cref="Run{T}"/> / <see cref="RunVoid"/> 在这个线程上执行。<br/>
/// 每个测试结束后调用 <see cref="ResetApplicationState"/>，清空应用资源与依赖注入容器，避免主题字典残留影响其它测试
/// （其它测试在各自的 STA 线程里创建窗口，不能访问归宿主线程所有的应用资源）。
/// </summary>
internal static class WpfTestHost
{
    private static readonly Lazy<Dispatcher> HostDispatcher = new(Start, LazyThreadSafetyMode.ExecutionAndPublication);

    private static Dispatcher Start()
    {
        // WindowBase 在窗口加载时会修改当前用户的注册表（仅首次运行、标记文件不存在时）。
        // 测试进程必须预先写入标记文件，让它跳过这一步，绝不能改动开发机的系统设置。
        string config = WindowHandler.BasePath;
        Directory.CreateDirectory(config);
        File.WriteAllText(Path.Combine(config, "registrie.ini"), "test marker: keeps WindowBase.UpdateRegistry from touching the registry");

        Dispatcher? dispatcher = null;
        using var ready = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            dispatcher = Dispatcher.CurrentDispatcher;
            ready.Set();
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "WpfTestHost"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();
        return dispatcher!;
    }

    /// <summary>在宿主线程上执行并返回结果；委托内的异常会原样抛给调用方。</summary>
    public static T Run<T>(Func<T> action) => HostDispatcher.Value.Invoke(action);

    /// <summary>在宿主线程上执行；委托内的异常会原样抛给调用方。</summary>
    public static void RunVoid(Action action) => HostDispatcher.Value.Invoke(action);

    /// <summary>
    /// 在宿主线程上运行消息循环一小段时间（只能在宿主线程调用），让排队的 Loaded 优先级工作与窗口加载完成。
    /// </summary>
    /// <param name="milliseconds">运行时长</param>
    public static void Pump(int milliseconds = 200)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    /// <summary>
    /// 在宿主线程上运行消息循环，直到条件成立或超时（只能在宿主线程调用）。<br/>
    /// 比固定时长的 <see cref="Pump"/> 可靠：异步延续可能要先经过线程池再回到调度器，机器繁忙时固定的等待可能不够。
    /// </summary>
    /// <param name="condition">要等待成立的条件</param>
    /// <param name="timeoutMilliseconds">最长等待时间</param>
    /// <returns>条件在超时之前成立返回 <c>true</c></returns>
    public static bool PumpUntil(Func<bool> condition, int timeoutMilliseconds = 5000)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.ElapsedMilliseconds >= timeoutMilliseconds) return false;
            Pump(20);
        }

        return true;
    }

    /// <summary>清空应用资源与依赖注入容器，使下一个测试从干净状态开始。</summary>
    public static void ResetApplicationState()
    {
        if (!HostDispatcher.IsValueCreated) return;
        RunVoid(() =>
        {
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.Clear();
            InjectionWpf.ClearService();
        });
    }
}
