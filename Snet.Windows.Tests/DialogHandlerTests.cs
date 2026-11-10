using MaterialDesignThemes.Wpf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snet.Model.@enum;
using Snet.Windows.Core;
using Snet.Windows.Core.@enum;
using Snet.Windows.Core.handler;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ShowButton = Snet.Windows.Controls.@enum.MessageBoxButton;
using ShowImage = Snet.Windows.Controls.@enum.MessageBoxImage;

namespace Snet.Windows.Tests;

/// <summary>
/// 公共 API <see cref="DialogHandler"/>：让调用方自己的对话框（原来直接调用 <c>DialogHost.Show</c>）也进入 <c>MessageBox</c> 使用的同一个队列。
/// </summary>
[TestClass]
public sealed class DialogHandlerTests
{
    [TestCleanup]
    public void Cleanup() => WpfTestHost.ResetApplicationState();

    private static void ApplyTheme() => SkinHandler.SetSkin(SkinType.Dark, false);

    private static WindowBase NewWindow() => new() { Width = 300, Height = 200, ShowInTaskbar = false };

    private static TextBlock View(string text) => new() { Text = text };

    private static Task<bool> ShowMessage(string text, Window? owner) =>
        Snet.Windows.Controls.message.MessageBox.Show(text, "title", ShowButton.OK, ShowImage.Information, owner);

    /// <summary>指定标识符的 DialogHost 里当前显示的内容的文字：消息框取正文，普通视图取 TextBlock 的文字。</summary>
    private static string? ShownLabel(string identifier)
    {
        object? content = DialogHost.GetDialogSession(identifier)?.Content;
        if (content is FrameworkElement { DataContext: Snet.Windows.Controls.message.MessageModel model })
        {
            return model.Content;
        }

        return (content as TextBlock)?.Text;
    }

    private static string Describe(Task task) =>
        task.IsFaulted ? "FAULTED " + task.Exception!.GetBaseException().GetType().Name + ": " + task.Exception.GetBaseException().Message : task.Status.ToString();

    private static void WaitUntilShown(string identifier, string label) =>
        Assert.IsTrue(WpfTestHost.PumpUntil(() => ShownLabel(identifier) == label), "等不到 “" + label + "” 显示出来，当前显示的是：" + (ShownLabel(identifier) ?? "（没有）"));

    private static void WaitUntilCompleted(Task task, string name) =>
        Assert.IsTrue(WpfTestHost.PumpUntil(() => task.IsCompleted), name + " 没有结束：" + Describe(task));

    [TestMethod]
    public void ShowAsync_ConcurrentCalls_AreQueuedAndShownInCallOrder_AndReturnTheirOwnResults() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        try
        {
            window.Show();
            WpfTestHost.Pump(300);
            string id = window.DialogHostIdentifier;

            var a = DialogHandler.ShowAsync(View("a"), window);
            var b = DialogHandler.ShowAsync(View("b"), window);
            var c = DialogHandler.ShowAsync(View("c"), window);
            WaitUntilShown(id, "a");
            WpfTestHost.Pump(300);

            // 直接调用 DialogHost.Show 时后到的两个调用会立刻抛 “DialogHost is already open”
            Assert.IsFalse(b.IsFaulted, "b: " + Describe(b));
            Assert.IsFalse(c.IsFaulted, "c: " + Describe(c));
            Assert.IsFalse(a.IsCompleted);
            Assert.IsFalse(b.IsCompleted);
            Assert.IsFalse(c.IsCompleted);

            DialogHost.Close(id, "result-a");
            WaitUntilCompleted(a, "a");
            Assert.AreEqual("result-a", a.Result);
            WaitUntilShown(id, "b");

            DialogHost.Close(id, "result-b");
            WaitUntilCompleted(b, "b");
            Assert.AreEqual("result-b", b.Result);
            WaitUntilShown(id, "c");

            // 没有参数关闭：结果是 null
            DialogHost.Close(id);
            WaitUntilCompleted(c, "c");
            Assert.IsNull(c.Result);
            Assert.IsFalse(DialogHost.IsDialogOpen(id), "队列排空之后宿主应该关闭");
        }
        finally
        {
            window.Close();
        }
    });

    [TestMethod]
    public void ShowAsync_SharesTheQueueWithMessageBox_InStrictCallOrder() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        try
        {
            window.Show();
            WpfTestHost.Pump(300);
            string id = window.DialogHostIdentifier;

            // 在同一次调度里交错调用两个 API：它们都只经过一次调度器跳转，进入队列的先后就是调用的先后
            var m1 = ShowMessage("m1", window);
            var view = DialogHandler.ShowAsync(View("view"), window);
            var m2 = ShowMessage("m2", window);
            WaitUntilShown(id, "m1");
            WpfTestHost.Pump(300);
            Assert.IsFalse(view.IsFaulted, "view: " + Describe(view));
            Assert.IsFalse(m2.IsFaulted, "m2: " + Describe(m2));

            DialogHost.Close(id, true);
            WaitUntilShown(id, "view");
            DialogHost.Close(id, "v");
            WaitUntilShown(id, "m2");
            DialogHost.Close(id, true);
            WaitUntilCompleted(m2, "m2");

            Assert.IsTrue(m1.Result);
            Assert.AreEqual("v", view.Result);
            Assert.IsTrue(m2.Result);
        }
        finally
        {
            window.Close();
        }
    });

    [TestMethod]
    public void ShowAsync_ContentFactory_IsCalledOnTheUiThreadWhenItIsItsTurn() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        try
        {
            window.Show();
            WpfTestHost.Pump(300);
            string id = window.DialogHostIdentifier;
            int uiThread = Environment.CurrentManagedThreadId;
            var factoryThreads = new List<int>();

            var a = DialogHandler.ShowAsync(View("a"), window);
            var b = DialogHandler.ShowAsync(() =>
            {
                factoryThreads.Add(Environment.CurrentManagedThreadId);
                return View("b");
            }, window);
            WaitUntilShown(id, "a");
            WpfTestHost.Pump(300);
            Assert.IsEmpty(factoryThreads, "排队期间不创建内容");

            DialogHost.Close(id);
            WaitUntilShown(id, "b");
            CollectionAssert.AreEqual(new[] { uiThread }, factoryThreads);

            // 从线程池线程调用：内容仍然在 UI 线程上由工厂创建（直接传一个在线程池线程上创建的元素是不行的）
            var c = Task.Run(() => DialogHandler.ShowAsync(() =>
            {
                factoryThreads.Add(Environment.CurrentManagedThreadId);
                return View("c");
            }, window));
            DialogHost.Close(id);
            WaitUntilShown(id, "c");
            CollectionAssert.AreEqual(new[] { uiThread, uiThread }, factoryThreads);
            DialogHost.Close(id, "rc");
            WaitUntilCompleted(c, "c");
            Assert.AreEqual("rc", c.Result);
            Assert.IsNull(a.Result);
            Assert.IsNull(b.Result);
        }
        finally
        {
            window.Close();
        }
    });

    [TestMethod]
    public void ShowAsync_FactoryThrows_FaultsOnlyThatCall_AndDoesNotBlockTheQueue() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        try
        {
            window.Show();
            WpfTestHost.Pump(300);
            string id = window.DialogHostIdentifier;

            var bad = DialogHandler.ShowAsync(() => throw new FormatException("boom"), window);
            var good = DialogHandler.ShowAsync(View("good"), window);
            WaitUntilCompleted(bad, "bad");
            Assert.IsTrue(bad.IsFaulted);
            Assert.IsInstanceOfType<FormatException>(bad.Exception!.GetBaseException());

            WaitUntilShown(id, "good");
            DialogHost.Close(id, "ok");
            WaitUntilCompleted(good, "good");
            Assert.AreEqual("ok", good.Result);
        }
        finally
        {
            window.Close();
        }
    });

    [TestMethod]
    public void ShowAsync_OwnerClosedWhileShownAndWhileQueued_BothEndWithNull() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase first = NewWindow();
        WindowBase second = NewWindow();
        bool firstClosed = false;
        try
        {
            first.Show();
            second.Show();
            WpfTestHost.Pump(300);

            var shown = DialogHandler.ShowAsync(View("shown"), first);
            var queued = DialogHandler.ShowAsync(View("queued"), first);
            WaitUntilShown(first.DialogHostIdentifier, "shown");

            first.Close();
            firstClosed = true;

            // 窗口关闭后宿主随之消失，DialogHost.Show 的任务永远不会完成；不能让调用一直挂着，也不能挡住别的窗口
            WaitUntilCompleted(shown, "shown");
            WaitUntilCompleted(queued, "queued");
            Assert.IsTrue(shown.IsCompletedSuccessfully, "shown: " + Describe(shown));
            Assert.IsTrue(queued.IsCompletedSuccessfully, "queued: " + Describe(queued));
            Assert.IsNull(shown.Result);
            Assert.IsNull(queued.Result);

            var other = DialogHandler.ShowAsync(View("other"), second);
            WaitUntilShown(second.DialogHostIdentifier, "other");
            DialogHost.Close(second.DialogHostIdentifier, "done");
            WaitUntilCompleted(other, "other");
            Assert.AreEqual("done", other.Result);
        }
        finally
        {
            if (!firstClosed) first.Close();
            second.Close();
        }
    });

    [TestMethod]
    public void ShowAsync_OwnerAlreadyClosed_ThrowsInvalidOperation() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        bool closed = false;
        try
        {
            window.Show();
            WpfTestHost.Pump(300);
            window.Close();
            closed = true;
            WpfTestHost.Pump(300);

            var late = DialogHandler.ShowAsync(View("late"), window);
            WaitUntilCompleted(late, "late");
            Assert.IsTrue(late.IsFaulted, "late: " + Describe(late));
            Assert.IsInstanceOfType<InvalidOperationException>(late.Exception!.GetBaseException());
        }
        finally
        {
            if (!closed) window.Close();
        }
    });

    [TestMethod]
    public void ShowAsync_CalledRightAfterTheWindowWasShown_WaitsForTheWindowToLoad() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        try
        {
            // “先 Show() 再立刻弹对话框”的常见写法：此时窗口还没有触发 Loaded，宿主还没有加载
            window.Show();
            var early = DialogHandler.ShowAsync(View("early"), window);

            Assert.IsTrue(WpfTestHost.PumpUntil(() => early.IsFaulted || ShownLabel(window.DialogHostIdentifier) == "early"), "early: " + Describe(early));
            Assert.IsFalse(early.IsFaulted, "early: " + Describe(early));
            DialogHost.Close(window.DialogHostIdentifier, "x");
            WaitUntilCompleted(early, "early");
            Assert.AreEqual("x", early.Result);
        }
        finally
        {
            window.Close();
        }
    });

    [TestMethod]
    public void ShowAsync_ElementInAnotherWindow_UsesThatWindowsDialogHost() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase first = NewWindow();
        var button = new Button { Content = "inside second" };
        WindowBase second = NewWindow();
        second.Content = button;
        try
        {
            first.Show();
            second.Show();
            WpfTestHost.Pump(300);
            Assert.AreEqual("DialogHost", first.DialogHostIdentifier, "传统名称归最早的窗口");

            // element 在第二个窗口里：显示在第二个窗口的 DialogHost 里，而不是持有传统名称的第一个窗口
            var task = DialogHandler.ShowAsync(View("for-second"), button);
            WaitUntilShown(second.DialogHostIdentifier, "for-second");
            Assert.IsFalse(DialogHost.IsDialogOpen(first.DialogHostIdentifier));
            DialogHost.Close(second.DialogHostIdentifier, "r");
            WaitUntilCompleted(task, "task");
            Assert.AreEqual("r", task.Result);
        }
        finally
        {
            first.Close();
            second.Close();
        }
    });

    [TestMethod]
    public void ShowAsync_WithoutAnyWindowBase_FallsBackToTheLegacyHost() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();

        // 没有任何 WindowBase：显示在应用自己的、标识符为 “DialogHost” 的 DialogHost 里（和直接 DialogHost.Show 一样，不排队）
        var host = new DialogHost { Identifier = "DialogHost", Content = new TextBlock { Text = "host" } };
        var plain = new Window { Content = host, Width = 300, Height = 200, ShowInTaskbar = false };
        try
        {
            plain.Show();
            WpfTestHost.Pump(400);

            var task = DialogHandler.ShowAsync(View("legacy"));
            WaitUntilShown("DialogHost", "legacy");
            DialogHost.Close("DialogHost", "r");
            WaitUntilCompleted(task, "task");
            Assert.AreEqual("r", task.Result);
        }
        finally
        {
            plain.Close();
        }
    });

    [TestMethod]
    public void ShowAsync_CancelledWhileQueued_IsCancelledAndDoesNotBlockTheOthers() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        try
        {
            window.Show();
            WpfTestHost.Pump(300);
            string id = window.DialogHostIdentifier;
            using var cts = new CancellationTokenSource();

            var a = DialogHandler.ShowAsync(View("a"), window);
            var b = DialogHandler.ShowAsync(View("b"), window, cts.Token);
            var c = DialogHandler.ShowAsync(View("c"), window);
            WaitUntilShown(id, "a");
            WpfTestHost.Pump(200);

            cts.Cancel();
            WaitUntilCompleted(b, "b");
            Assert.IsTrue(b.IsCanceled, "b: " + Describe(b));
            Assert.IsFalse(a.IsCompleted, "取消排队中的 b 不影响正在显示的 a");

            // b 被取消之后不占用队列：a 关闭后直接轮到 c
            DialogHost.Close(id, "ra");
            WaitUntilShown(id, "c");
            DialogHost.Close(id, "rc");
            WaitUntilCompleted(c, "c");
            Assert.AreEqual("ra", a.Result);
            Assert.AreEqual("rc", c.Result);

            // 调用时令牌就已经取消：直接取消，什么都不显示
            var already = DialogHandler.ShowAsync(View("never"), window, new CancellationToken(true));
            WaitUntilCompleted(already, "already");
            Assert.IsTrue(already.IsCanceled, "already: " + Describe(already));
            Assert.IsFalse(DialogHost.IsDialogOpen(id));
        }
        finally
        {
            window.Close();
        }
    });

    [TestMethod]
    public void ShowAsync_CancellationAfterTheDialogIsShown_DoesNotCloseIt() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        try
        {
            window.Show();
            WpfTestHost.Pump(300);
            string id = window.DialogHostIdentifier;
            using var cts = new CancellationTokenSource();

            var a = DialogHandler.ShowAsync(View("a"), window, cts.Token);
            WaitUntilShown(id, "a");
            cts.Cancel();
            WpfTestHost.Pump(300);

            // 取消只影响排队等待：对话框一旦显示就由用户决定什么时候关闭
            Assert.IsTrue(DialogHost.IsDialogOpen(id));
            Assert.IsFalse(a.IsCompleted, "a: " + Describe(a));
            DialogHost.Close(id, "x");
            WaitUntilCompleted(a, "a");
            Assert.AreEqual("x", a.Result);
        }
        finally
        {
            window.Close();
        }
    });

    [TestMethod]
    public void ShowAsync_WindowRetemplatedWhileShown_EndsTheCallAndDoesNotBlockTheQueueForever() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        try
        {
            window.Show();
            WpfTestHost.Pump(300);
            string id = window.DialogHostIdentifier;

            var a = DialogHandler.ShowAsync(View("a"), window);
            var b = DialogHandler.ShowAsync(View("b"), window);
            WaitUntilShown(id, "a");

            // 窗口没有关闭、但重新套用了模板（例如换肤使窗口换了样式）：显示着对话框的旧 DialogHost 随旧模板一起卸载，
            // DialogHost.Show 的任务永远不会完成。门不能因此被永久占住，否则这个窗口之后的所有对话框都会悄悄排队到永远。
            // 模板和 DialogHost 的卸载/加载都留给消息循环去做（不调用 UpdateLayout），和换肤时的真实顺序一致
            ControlTemplate original = window.Template;
            window.Template = null;
            window.Template = original;

            WaitUntilCompleted(a, "a");
            Assert.IsTrue(a.IsCompletedSuccessfully, "a: " + Describe(a));
            Assert.IsNull(a.Result, "宿主丢了：没有结果");

            // 排在后面的调用在新的 DialogHost 里正常显示
            WaitUntilShown(id, "b");
            DialogHost.Close(id, "rb");
            WaitUntilCompleted(b, "b");
            Assert.IsTrue(b.IsCompletedSuccessfully, "b: " + Describe(b));
            Assert.AreEqual("rb", b.Result);

            // 换过模板的窗口之后照常工作
            var c = DialogHandler.ShowAsync(View("c"), window);
            WaitUntilShown(id, "c");
            DialogHost.Close(id, "rc");
            WaitUntilCompleted(c, "c");
            Assert.AreEqual("rc", c.Result);
        }
        finally
        {
            window.Close();
        }
    });

    [TestMethod]
    public void ShowAsync_ClosedHandlerThrows_StillEndsTheShownAndTheQueuedCalls() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        window.Show();
        WpfTestHost.Pump(300);
        string id = window.DialogHostIdentifier;
        window.Closed += (_, _) => throw new InvalidOperationException("Closed 事件的处理程序抛出的异常");

        // 处理程序的异常不会从 Close() 里抛出来，而是经消息循环交给调度器的 UnhandledException（应用里就是 DispatcherUnhandledException）；
        // 这里把它接住，否则测试进程会崩溃
        Exception? unhandled = null;
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DispatcherUnhandledExceptionEventHandler onUnhandled = (_, e) =>
        {
            unhandled = e.Exception;
            e.Handled = true;
        };
        dispatcher.UnhandledException += onUnhandled;
        try
        {
            var shown = DialogHandler.ShowAsync(View("shown"), window);
            var queued = DialogHandler.ShowAsync(View("queued"), window);
            WaitUntilShown(id, "shown");

            // 窗口已经关闭了，显示着的和排着队的调用必须结束，不能因为 Closed 处理程序抛了异常、清理被跳过而永远挂着
            window.Close();
            WaitUntilCompleted(shown, "shown");
            WaitUntilCompleted(queued, "queued");
            Assert.IsInstanceOfType<InvalidOperationException>(unhandled, "Closed 处理程序的异常没有交给调度器");
            Assert.IsTrue(shown.IsCompletedSuccessfully, "shown: " + Describe(shown));
            Assert.IsTrue(queued.IsCompletedSuccessfully, "queued: " + Describe(queued));
            Assert.IsNull(shown.Result);
            Assert.IsNull(queued.Result);
        }
        finally
        {
            dispatcher.UnhandledException -= onUnhandled;
        }
    });

    [TestMethod]
    public void ShowAsync_AsyncLambda_BindsToTheObjectOverloadAndIsRejected()
    {
        // async () => ... 转换不成 Func<object>，只能绑定到 object 重载：委托不是对话框内容，要立刻报错，而不是把委托的类型名显示出来
        var error = Assert.ThrowsExactly<ArgumentException>(() => { _ = DialogHandler.ShowAsync(async () => { await Task.Yield(); return new TextBlock(); }); });
        Assert.AreEqual("content", error.ParamName);
    }

    [TestMethod]
    public void ShowAsync_NullArguments_AreRejectedSynchronously()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => { _ = DialogHandler.ShowAsync((object)null!); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { _ = DialogHandler.ShowAsync((Func<object>)null!); });
    }
}
