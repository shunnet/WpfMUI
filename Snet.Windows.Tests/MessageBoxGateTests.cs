using MaterialDesignThemes.Wpf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snet.Model.@enum;
using Snet.Windows.Core;
using Snet.Windows.Core.@enum;
using Snet.Windows.Core.handler;
using System.Windows;
using System.Windows.Controls;
using ShowButton = Snet.Windows.Controls.@enum.MessageBoxButton;
using ShowImage = Snet.Windows.Controls.@enum.MessageBoxImage;

namespace Snet.Windows.Tests;

/// <summary>
/// <c>MessageBox.Show</c> 的“异步显示门”：同一个窗口（同一个 DialogHost）同一时刻只能显示一个对话框，
/// 并发的调用必须按调用顺序排队，而不是抛出 “DialogHost is already open”；窗口在对话框显示/排队期间关闭时，调用必须结束（返回 false），不能永远挂起。
/// </summary>
[TestClass]
public sealed class MessageBoxGateTests
{
    [TestCleanup]
    public void Cleanup() => WpfTestHost.ResetApplicationState();

    private static void ApplyTheme() => SkinHandler.SetSkin(SkinType.Dark, false);

    private static WindowBase NewWindow() => new() { Width = 300, Height = 200, ShowInTaskbar = false };

    private static Task<bool> Show(string text, ShowButton button, Window? owner) =>
        Snet.Windows.Controls.message.MessageBox.Show(text, "title", button, ShowImage.Information, owner);

    /// <summary>指定标识符的 DialogHost 里当前显示的消息框的正文。</summary>
    private static string? ShownMessage(string identifier)
    {
        var session = DialogHost.GetDialogSession(identifier);
        return ((session?.Content as FrameworkElement)?.DataContext as Snet.Windows.Controls.message.MessageModel)?.Content;
    }

    private static string Describe(Task task) =>
        task.IsFaulted ? "FAULTED " + task.Exception!.GetBaseException().GetType().Name + ": " + task.Exception.GetBaseException().Message : task.Status.ToString();

    /// <summary>等到指定标识符的 DialogHost 里显示出指定正文的消息框。</summary>
    private static void WaitUntilShown(string identifier, string message) =>
        Assert.IsTrue(WpfTestHost.PumpUntil(() => ShownMessage(identifier) == message), "等不到消息框 “" + message + "” 显示出来，当前显示的是：" + (ShownMessage(identifier) ?? "（没有）"));

    /// <summary>等到任务结束（完成或出错）。</summary>
    private static void WaitUntilCompleted(Task task, string name) =>
        Assert.IsTrue(WpfTestHost.PumpUntil(() => task.IsCompleted), name + " 没有结束：" + Describe(task));

    [TestMethod]
    public void MessageBox_ConcurrentCallsOnTheSameWindow_AreQueuedAndShownInCallOrder() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        try
        {
            window.Show();
            WpfTestHost.Pump(300);
            string id = window.DialogHostIdentifier;

            var a = Show("a", ShowButton.OK, window);
            var b = Show("b", ShowButton.OKCancel, window);
            var c = Show("c", ShowButton.YesNo, window);
            WaitUntilShown(id, "a");
            WpfTestHost.Pump(300);

            // 修复前：后到的两个调用立刻 FAULTED（InvalidOperationException: DialogHost is already open）
            Assert.IsFalse(b.IsFaulted, "b: " + Describe(b));
            Assert.IsFalse(c.IsFaulted, "c: " + Describe(c));
            Assert.IsTrue(DialogHost.IsDialogOpen(id));
            Assert.AreEqual("a", ShownMessage(id));
            Assert.IsFalse(a.IsCompleted);
            Assert.IsFalse(b.IsCompleted, "b 要等 a 关闭之后才显示");
            Assert.IsFalse(c.IsCompleted);

            // 各自关闭时给出不同的结果：如果显示顺序不是调用顺序，结果会落到错误的调用上
            DialogHost.Close(id, true);
            WaitUntilCompleted(a, "a");
            Assert.IsTrue(a.Result);
            WaitUntilShown(id, "b");

            DialogHost.Close(id, false);
            WaitUntilCompleted(b, "b");
            Assert.IsFalse(b.Result);
            WaitUntilShown(id, "c");

            DialogHost.Close(id, true);
            WaitUntilCompleted(c, "c");
            Assert.IsTrue(c.Result);
            Assert.IsFalse(DialogHost.IsDialogOpen(id), "队列排空之后宿主应该关闭");
        }
        finally
        {
            window.Close();
        }
    });

    [TestMethod]
    public void MessageBox_OwnerClosedWhileItsDialogIsOpen_EndsTheCallInsteadOfWaitingForever() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        bool closed = false;
        try
        {
            window.Show();
            WpfTestHost.Pump(300);

            var shown = Show("shown", ShowButton.OK, window);
            WaitUntilShown(window.DialogHostIdentifier, "shown");
            Assert.IsFalse(shown.IsCompleted);

            window.Close();
            closed = true;

            // 修复前：窗口关闭后宿主随之消失，DialogHost.Show 的任务永远不会完成，调用方（以及它之后的所有逻辑）永远等待
            WaitUntilCompleted(shown, "shown");
            Assert.IsTrue(shown.IsCompletedSuccessfully, "shown: " + Describe(shown));
            Assert.IsFalse(shown.Result, "对话框所属窗口被关闭，按“关闭”处理");
        }
        finally
        {
            if (!closed) window.Close();
        }
    });

    [TestMethod]
    public void MessageBox_OwnerClosed_EndsTheShownCallAndEveryQueuedCall() => WpfTestHost.RunVoid(() =>
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

            var shown = Show("shown", ShowButton.OK, first);
            var queued1 = Show("queued1", ShowButton.OK, first);
            var queued2 = Show("queued2", ShowButton.YesNo, first);
            var queued3 = Show("queued3", ShowButton.OKCancel, first);
            WaitUntilShown(first.DialogHostIdentifier, "shown");
            WpfTestHost.Pump(300);
            Assert.IsFalse(shown.IsCompleted);
            Assert.IsFalse(queued1.IsCompleted, "queued1: " + Describe(queued1));
            Assert.IsFalse(queued2.IsCompleted);
            Assert.IsFalse(queued3.IsCompleted);

            first.Close();
            firstClosed = true;

            // 修复前：排队的调用在上面就已经抛了 “already open”；显示中的调用永远不会结束
            foreach (var (task, name) in new[] { (shown, "shown"), (queued1, "queued1"), (queued2, "queued2"), (queued3, "queued3") })
            {
                WaitUntilCompleted(task, name);
                Assert.IsTrue(task.IsCompletedSuccessfully, name + ": " + Describe(task));
                Assert.IsFalse(task.Result, name + " 应按“关闭”处理");
            }

            // 其它窗口不受影响
            var other = Show("other", ShowButton.OK, second);
            WaitUntilShown(second.DialogHostIdentifier, "other");
            DialogHost.Close(second.DialogHostIdentifier, true);
            WaitUntilCompleted(other, "other");
            Assert.IsTrue(other.Result);
        }
        finally
        {
            if (!firstClosed) first.Close();
            second.Close();
        }
    });

    [TestMethod]
    public void MessageBox_QueuedCallWhoseWindowClosesBeforeItsTurn_EndsAsFalse() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        bool closed = false;
        try
        {
            window.Show();
            WpfTestHost.Pump(300);
            string id = window.DialogHostIdentifier;

            var a = Show("a", ShowButton.OK, window);
            var b = Show("b", ShowButton.OK, window);
            WaitUntilShown(id, "a");

            // a 正常关闭，随即 b 开始显示；这时窗口被关闭：b 必须结束
            DialogHost.Close(id, true);
            WaitUntilCompleted(a, "a");
            Assert.IsTrue(a.IsCompletedSuccessfully);
            WaitUntilShown(id, "b");

            window.Close();
            closed = true;
            WaitUntilCompleted(b, "b");
            Assert.IsTrue(b.IsCompletedSuccessfully, "b: " + Describe(b));
            Assert.IsFalse(b.Result);
        }
        finally
        {
            if (!closed) window.Close();
        }
    });

    [TestMethod]
    public void MessageBox_CallWithoutOwner_QueuesBehindACallWithTheSameWindowAsOwner() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        try
        {
            window.Show();
            WpfTestHost.Pump(300);
            string id = window.DialogHostIdentifier;

            // 没有 owner 的调用解析到同一个窗口（只有这一个 WindowBase），所以与显式指定 owner 的调用共用同一个门
            var explicitOwner = Show("explicit", ShowButton.OK, window);
            var noOwner = Show("no-owner", ShowButton.OK, null);
            WaitUntilShown(id, "explicit");
            WpfTestHost.Pump(300);

            // 修复前：后到的调用立刻 FAULTED（DialogHost is already open）
            Assert.IsFalse(noOwner.IsFaulted, "noOwner: " + Describe(noOwner));
            Assert.IsFalse(noOwner.IsCompleted);

            DialogHost.Close(id, true);
            WaitUntilShown(id, "no-owner");
            DialogHost.Close(id, true);
            WaitUntilCompleted(noOwner, "noOwner");
            Assert.IsTrue(explicitOwner.Result);
            Assert.IsTrue(noOwner.Result);
        }
        finally
        {
            window.Close();
        }
    });

    [TestMethod]
    public void MessageBox_PlainWindowAsOwner_UsesTheWindowBaseHostAndQueuesWithIt() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        var plain = new Window { Content = new TextBlock { Text = "plain" }, Width = 200, Height = 100, ShowInTaskbar = false };
        try
        {
            window.Show();
            plain.Show();
            WpfTestHost.Pump(300);
            string id = window.DialogHostIdentifier;

            // owner 不是 WindowBase（没有 DialogHost）：回退到 WindowBase 的 DialogHost，并与指定那个 WindowBase 的调用共用同一个门
            var viaWindowBase = Show("via-windowbase", ShowButton.OK, window);
            var viaPlain = Show("via-plain", ShowButton.OK, plain);
            WaitUntilShown(id, "via-windowbase");
            WpfTestHost.Pump(300);
            Assert.IsFalse(viaPlain.IsFaulted, "viaPlain: " + Describe(viaPlain));
            Assert.IsFalse(viaPlain.IsCompleted);

            DialogHost.Close(id, true);
            WaitUntilShown(id, "via-plain");
            DialogHost.Close(id, false);
            WaitUntilCompleted(viaPlain, "viaPlain");
            Assert.IsTrue(viaWindowBase.Result);
            Assert.IsFalse(viaPlain.Result);
        }
        finally
        {
            plain.Close();
            window.Close();
        }
    });

    [TestMethod]
    public void MessageBox_OwnerAlreadyClosedBeforeTheCall_ThrowsInvalidOperation() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        bool closed = false;
        try
        {
            window.Show();
            WpfTestHost.Pump(300);

            // 先正常用一次（创建这个窗口的门），关闭窗口之后再调用：没有宿主可以显示，抛 InvalidOperationException。
            // 不是静默返回 false（只有“调用进行中”窗口被关闭才按关闭处理），也不会挂起
            var first = Show("first", ShowButton.OK, window);
            WaitUntilShown(window.DialogHostIdentifier, "first");
            DialogHost.Close(window.DialogHostIdentifier, true);
            WaitUntilCompleted(first, "first");

            window.Close();
            closed = true;
            WpfTestHost.Pump(300);

            var late = Show("late", ShowButton.OK, window);
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
    public void MessageBox_ClosedOwnerThatHeldTheLegacyName_FailsInsteadOfUsingAnotherWindowsHost() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase first = NewWindow();
        WindowBase second = NewWindow();
        WindowBase third = NewWindow();
        bool firstClosed = false, secondClosed = false;
        try
        {
            first.Show();
            second.Show();
            third.Show();
            WpfTestHost.Pump(300);
            Assert.AreEqual("DialogHost", first.DialogHostIdentifier, "传统名称归最早的窗口");

            // first 先正常用一次（它有了自己的门），然后关闭；传统名称移交给 second
            var warm = Show("warm", ShowButton.OK, first);
            WaitUntilShown(first.DialogHostIdentifier, "warm");
            DialogHost.Close(first.DialogHostIdentifier, true);
            WaitUntilCompleted(warm, "warm");
            first.Close();
            firstClosed = true;
            WpfTestHost.Pump(300);
            Assert.AreEqual("DialogHost", second.DialogHostIdentifier);

            // second 从来没用过消息框（没有门）就关闭；传统名称再移交给 third
            second.Close();
            secondClosed = true;
            WpfTestHost.Pump(300);
            Assert.AreEqual("DialogHost", third.DialogHostIdentifier);

            // 以已关闭的窗口为 owner 的调用必须失败，不能显示到别的窗口的 DialogHost 里：无论它的门是否存在（first 有门、second 没有）。
            // 这里是固定契约的用例：窗口是在可见状态下关闭的，它们的标识符在 Closed 之前已经被复位，审查前的实现在这个场景里同样通过；
            // 会留下过期传统名称的场景见 …ClosedHiddenOwnerThatKeptTheLegacyName_…
            var lateFirst = Show("late-first", ShowButton.OK, first);
            var lateSecond = Show("late-second", ShowButton.OK, second);
            WaitUntilCompleted(lateFirst, "lateFirst");
            WaitUntilCompleted(lateSecond, "lateSecond");
            Assert.IsTrue(lateFirst.IsFaulted, "lateFirst: " + Describe(lateFirst));
            Assert.IsTrue(lateSecond.IsFaulted, "lateSecond: " + Describe(lateSecond));
            Assert.IsInstanceOfType<InvalidOperationException>(lateFirst.Exception!.GetBaseException());
            Assert.IsInstanceOfType<InvalidOperationException>(lateSecond.Exception!.GetBaseException());
            Assert.IsFalse(DialogHost.IsDialogOpen(third.DialogHostIdentifier), "third 的 DialogHost 里不应该出现别的窗口的消息框");

            // third 自己照常使用
            var own = Show("own", ShowButton.OK, third);
            WaitUntilShown(third.DialogHostIdentifier, "own");
            DialogHost.Close(third.DialogHostIdentifier, true);
            WaitUntilCompleted(own, "own");
            Assert.IsTrue(own.Result);
        }
        finally
        {
            if (!firstClosed) first.Close();
            if (!secondClosed) second.Close();
            third.Close();
        }
    });

    [TestMethod]
    public void MessageBox_ClosedHiddenOwnerThatKeptTheLegacyName_FailsInsteadOfUsingAnotherWindowsHost() => WpfTestHost.RunVoid(() =>
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
            Assert.AreEqual("DialogHost", first.DialogHostIdentifier, "传统名称归最早的窗口");

            // 可见的窗口关闭时，IsVisibleChanged 会在 Closed 之前把它的标识符复位成它自己的唯一标识符，所以通常不会留下过期的传统名称。
            // 留下过期名称的是“关闭时已经不可见”的持有者：first 里开着消息框时隐藏它，传统名称不在对话框打开期间换人，仍然留在它那里
            var shown = Show("shown", ShowButton.OK, first);
            WaitUntilShown("DialogHost", "shown");
            first.Hide();
            WpfTestHost.Pump(300);
            Assert.AreEqual("DialogHost", first.DialogHostIdentifier, "对话框打开期间传统名称不换人");
            DialogHost.Close("DialogHost", true);
            WaitUntilCompleted(shown, "shown");

            // 关闭这个已经隐藏的窗口：没有 IsVisibleChanged，它的标识符停留在传统名称上，而传统名称已经移交给 second
            first.Close();
            firstClosed = true;
            WpfTestHost.Pump(300);
            Assert.AreEqual("DialogHost", second.DialogHostIdentifier);
            Assert.AreEqual("DialogHost", first.DialogHostIdentifier, "前提：已关闭窗口的标识符停留在传统名称上");

            // 以已关闭窗口为 owner 的调用必须失败，不能显示到现在持有这个名称的 second 的 DialogHost 里
            // （审查前的实现：门已经关闭，调用立刻返回 false，对话框却显示在 second 的 DialogHost 里，不在 second 的门里）
            var late = Show("late", ShowButton.OK, first);
            WaitUntilCompleted(late, "late");
            Assert.IsTrue(late.IsFaulted, "late: " + Describe(late));
            Assert.IsInstanceOfType<InvalidOperationException>(late.Exception!.GetBaseException());
            Assert.IsFalse(DialogHost.IsDialogOpen(second.DialogHostIdentifier), "second 的 DialogHost 里不应该出现别的窗口的消息框");
        }
        finally
        {
            if (!firstClosed) first.Close();
            second.Close();
        }
    });

    [TestMethod]
    public void MessageBox_CallsMadeFromTheContinuationOfThePreviousCall_ShowOneAfterAnother() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        try
        {
            window.Show();
            WpfTestHost.Pump(300);
            string id = window.DialogHostIdentifier;

            // “确认之后再提示结果”的常见写法：在上一个消息框的 await 之后立刻显示下一个，门的交接不能让它抛异常
            async Task<bool[]> FlowAsync()
            {
                var results = new List<bool>();
                for (int i = 0; i < 3; i++)
                {
                    var task = Show("m" + i, ShowButton.YesNo, window);
                    while (ShownMessage(id) != "m" + i)
                    {
                        await Task.Delay(20);
                    }

                    DialogHost.Close(id, i % 2 == 0);
                    results.Add(await task);
                }

                return results.ToArray();
            }

            var flow = FlowAsync();
            WaitUntilCompleted(flow, "flow");
            Assert.IsTrue(flow.IsCompletedSuccessfully, "flow: " + Describe(flow));
            CollectionAssert.AreEqual(new[] { true, false, true }, flow.Result);
        }
        finally
        {
            window.Close();
        }
    });

    [TestMethod]
    public void MessageBox_CalledFromBackgroundThreads_QueuesLikeCallsFromTheUiThread() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase window = NewWindow();
        try
        {
            window.Show();
            WpfTestHost.Pump(300);
            string id = window.DialogHostIdentifier;

            // 全局异常处理、后台任务汇报错误等场景：消息框是从线程池线程上调用的（所属窗口由 UI 线程取得并传入）
            var a = Task.Run(() => Show("bg-a", ShowButton.OK, window));
            var b = Task.Run(() => Show("bg-b", ShowButton.OK, window));
            Assert.IsTrue(WpfTestHost.PumpUntil(() => ShownMessage(id) != null || a.IsFaulted || b.IsFaulted), "没有消息框显示出来");
            WpfTestHost.Pump(300);

            // 修复前：从线程池线程调用时直接抛 InvalidOperationException（The calling thread cannot access this object ...）
            Assert.IsFalse(a.IsFaulted, "a: " + Describe(a));
            Assert.IsFalse(b.IsFaulted, "b: " + Describe(b));

            // 两个线程谁先到不确定：只验证同一时刻只显示一个，关闭它之后另一个接着显示
            string firstShown = ShownMessage(id)!;
            string secondShown = firstShown == "bg-a" ? "bg-b" : "bg-a";
            Assert.IsTrue(firstShown is "bg-a" or "bg-b");
            DialogHost.Close(id, true);
            WaitUntilShown(id, secondShown);
            DialogHost.Close(id, false);
            WaitUntilCompleted(a, "a");
            WaitUntilCompleted(b, "b");
            Assert.AreEqual(firstShown == "bg-a", a.Result);
            Assert.AreEqual(firstShown == "bg-b", b.Result);
        }
        finally
        {
            window.Close();
        }
    });

    private static Window NewPlainWindowWithLegacyHost()
    {
        var host = new DialogHost { Identifier = "DialogHost", Content = new TextBlock { Text = "host" } };
        return new Window { Content = host, Width = 300, Height = 200, ShowInTaskbar = false };
    }

    [TestMethod]
    public void MessageBox_WithoutAnyWindowBase_ShowsInTheLegacyHost() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();

        // 没有任何 WindowBase：消息框显示在应用自己的、标识符为 “DialogHost” 的 DialogHost 里（单窗口时代的写法）
        Window plain = NewPlainWindowWithLegacyHost();
        try
        {
            plain.Show();
            WpfTestHost.Pump(400);

            var shown = Show("legacy", ShowButton.OK, null);
            WaitUntilShown("DialogHost", "legacy");
            DialogHost.Close("DialogHost", true);
            WaitUntilCompleted(shown, "shown");
            Assert.IsTrue(shown.Result);
        }
        finally
        {
            plain.Close();
        }
    });

    [TestMethod]
    public void MessageBox_WithoutAnyWindowBase_HostWindowClosedMidDialog_LaterCallsOnANewHostStillWork() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();

        // 没有 WindowBase 时没有窗口的生命周期可以观察：宿主随窗口一起消失之后，显示中的那个调用不会结束（和以前一样），
        // 但它不能把后面的调用永远挡住——登录窗口关闭、主窗口里有同样标识符的 DialogHost 之后，消息框仍然能显示
        Window firstHost = NewPlainWindowWithLegacyHost();
        Window secondHost = NewPlainWindowWithLegacyHost();
        bool firstClosed = false;
        try
        {
            firstHost.Show();
            WpfTestHost.Pump(400);
            _ = Show("orphan", ShowButton.OK, null);
            WaitUntilShown("DialogHost", "orphan");

            firstHost.Close();
            firstClosed = true;
            secondHost.Show();
            WpfTestHost.Pump(400);

            var later = Show("later", ShowButton.OK, null);
            Assert.IsTrue(WpfTestHost.PumpUntil(() => later.IsFaulted || ShownMessage("DialogHost") == "later"), "后面的调用被挡住了：" + Describe(later));
            Assert.IsFalse(later.IsFaulted, "later: " + Describe(later));
            DialogHost.Close("DialogHost", true);
            WaitUntilCompleted(later, "later");
            Assert.IsTrue(later.Result);
        }
        finally
        {
            if (!firstClosed) firstHost.Close();
            secondHost.Close();
        }
    });
}
