using MaterialDesignThemes.Wpf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snet.Model.@enum;
using Snet.Windows.Core;
using Snet.Windows.Core.data;
using Snet.Windows.Core.@enum;
using Snet.Windows.Core.handler;
using Snet.Windows.Core.localize.wpf.Engine;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Snet.Windows.Tests;

/// <summary>
/// Snet.Windows.Core 审查（2026-11）发现的缺陷的回归测试。<br/>
/// 每个测试都注明"修复前"的失败现象；依赖 Application 的测试通过 <see cref="WpfTestHost"/> 在统一的宿主线程上运行。
/// </summary>
[TestClass]
public sealed class CoreRegressionTests
{
    public sealed class CachedWindow : Window { }

    public sealed class CachedViewModel { }

    public sealed class CancelWindow : Window { }

    public sealed class CancelViewModel { }

    [TestCleanup]
    public void Cleanup() => WpfTestHost.ResetApplicationState();

    /// <summary>
    /// 把主题资源合并进应用资源。WindowBase 的静态构造只在进程内第一次使用时合并主题，
    /// 而每个测试结束后宿主会清空应用资源，所以依赖样式/主题画刷的测试要显式合并一次。
    /// </summary>
    private static void ApplyTheme() => SkinHandler.SetSkin(SkinType.Dark, false);

    private static WindowBase NewWindow() => new() { Width = 300, Height = 200, ShowInTaskbar = false };

    [TestMethod]
    public void WindowBase_TwoWindows_EachHasItsOwnDialogHost_AndTheLegacyNameFollowsTheOldest() => WpfTestHost.RunVoid(() =>
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

            Assert.AreEqual("DialogHost", first.DialogHostIdentifier, "传统名称归最早创建的可见窗口");
            Assert.AreEqual("DialogHost_ClickClose", first.DialogHostClickCloseIdentifier);
            Assert.AreNotEqual("DialogHost", second.DialogHostIdentifier);
            Assert.AreNotEqual(first.DialogHostIdentifier, second.DialogHostIdentifier);

            // 修复前：两个窗口的 DialogHost 标识符都是写死的 "DialogHost"，按任何名称显示都抛 InvalidOperationException（Multiple viable DialogHosts）
            var legacy = DialogHost.Show("legacy", "DialogHost");
            var own = DialogHost.Show("own", second.DialogHostIdentifier);
            WpfTestHost.Pump(300);
            Assert.IsFalse(legacy.IsFaulted, legacy.Exception?.GetBaseException().Message);
            Assert.IsFalse(own.IsFaulted, own.Exception?.GetBaseException().Message);
            Assert.IsTrue(DialogHost.IsDialogOpen("DialogHost"));
            Assert.IsTrue(DialogHost.IsDialogOpen(second.DialogHostIdentifier));
            DialogHost.Close("DialogHost");
            DialogHost.Close(second.DialogHostIdentifier);
            WpfTestHost.Pump(200);

            // 传统名称随最早的窗口关闭而移交（先关登录窗口再开主窗口的常见写法仍然可用）
            first.Close();
            firstClosed = true;
            WpfTestHost.Pump(200);
            Assert.AreEqual("DialogHost", second.DialogHostIdentifier);
            Assert.AreEqual("DialogHost_ClickClose", second.DialogHostClickCloseIdentifier);
        }
        finally
        {
            if (!firstClosed) first.Close();
            second.Close();
        }
    });

    [TestMethod]
    public void WindowBase_LegacyDialogHostName_StaysWithTheWindowWhoseDialogIsOpen() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase older = NewWindow();   // 先创建，后显示
        WindowBase newer = NewWindow();
        try
        {
            newer.Show();
            WpfTestHost.Pump(300);
            Assert.AreEqual("DialogHost", newer.DialogHostIdentifier, "older 还没有加载，传统名称暂时归 newer");

            var legacy = DialogHost.Show("legacy", "DialogHost");
            WpfTestHost.Pump(200);
            Assert.IsTrue(DialogHost.IsDialogOpen("DialogHost"));

            // older 现在才加载：按创建顺序它应该接管传统名称，但 newer 里正开着一个用传统名称打开的对话框
            older.Show();
            WpfTestHost.Pump(300);

            // 修复前：名称被移交给 older，DialogHost.Close("DialogHost") 落到 older 的（没有打开对话框的）宿主上并抛出 InvalidOperationException
            Assert.AreEqual("DialogHost", newer.DialogHostIdentifier, "对话框打开期间传统名称不能换人");
            Assert.AreNotEqual("DialogHost", older.DialogHostIdentifier);
            DialogHost.Close("DialogHost");
            WpfTestHost.Pump(200);
            Assert.IsTrue(legacy.IsCompleted);
        }
        finally
        {
            older.Close();
            newer.Close();
        }
    });

    [TestMethod]
    public void MessageBox_Show_WithOwner_ShowsInThatWindow_AndTwoWindowsCanShowTogether() => WpfTestHost.RunVoid(() =>
    {
        ApplyTheme();
        WindowBase first = NewWindow();
        WindowBase second = NewWindow();
        try
        {
            first.Show();
            second.Show();
            WpfTestHost.Pump(300);

            // 修复前：库自带 MessageBox 固定使用标识符 "DialogHost"，两个窗口同时存在时直接抛 Multiple viable DialogHosts
            var one = Snet.Windows.Controls.message.MessageBox.Show("one", "t1", Snet.Windows.Controls.@enum.MessageBoxButton.OK, Snet.Windows.Controls.@enum.MessageBoxImage.Information, first);
            var two = Snet.Windows.Controls.message.MessageBox.Show("two", "t2", Snet.Windows.Controls.@enum.MessageBoxButton.OKCancel, Snet.Windows.Controls.@enum.MessageBoxImage.Question, second);
            WpfTestHost.Pump(500);
            Assert.IsFalse(one.IsFaulted, one.Exception?.GetBaseException().Message);
            Assert.IsFalse(two.IsFaulted, two.Exception?.GetBaseException().Message);
            Assert.IsTrue(DialogHost.IsDialogOpen(first.DialogHostIdentifier), "第一个消息框应显示在 owner 窗口里");
            Assert.IsTrue(DialogHost.IsDialogOpen(second.DialogHostIdentifier), "两个窗口的消息框可以同时显示（每次调用使用各自的对话框控件）");
            DialogHost.Close(first.DialogHostIdentifier);
            DialogHost.Close(second.DialogHostIdentifier);
            WpfTestHost.Pump(300);

            // 不指定 owner 时也不再因为多窗口而失败
            var any = Snet.Windows.Controls.message.MessageBox.Show("three", "t3", Snet.Windows.Controls.@enum.MessageBoxButton.OK, Snet.Windows.Controls.@enum.MessageBoxImage.Information);
            WpfTestHost.Pump(500);
            Assert.IsFalse(any.IsFaulted, any.Exception?.GetBaseException().Message);
            Assert.IsTrue(DialogHost.IsDialogOpen(first.DialogHostIdentifier) || DialogHost.IsDialogOpen(second.DialogHostIdentifier));
        }
        finally
        {
            first.Close();
            second.Close();
        }
    });

    [TestMethod]
    public void WindowBase_TemplateWithoutOptionalParts_DoesNotThrow() => WpfTestHost.RunVoid(() =>
    {
        // 用户自定义模板可以省略最小化/最大化按钮；修复前 OnApplyTemplate 抛 NullReferenceException（UpdateButtonVisibility）
        var window = new WindowBase();
        window.Template = new ControlTemplate(typeof(WindowBase)) { VisualTree = new FrameworkElementFactory(typeof(ContentPresenter)) };

        window.ApplyTemplate();
    });

    [TestMethod]
    public void WindowBase_CreatedInCode_CanBeShownAndClosed() => WpfTestHost.RunVoid(() =>
    {
        // 修复前：样式里的 AllowsTransparency 在 Show() 创建窗口句柄之后才被应用，抛 InvalidOperationException
        ApplyTheme();
        var window = new WindowBase { Width = 240, Height = 160, ShowInTaskbar = false };
        try
        {
            window.Show();
            WpfTestHost.Pump(150);
        }
        finally
        {
            window.Close();
        }
    });

    [TestMethod]
    public void WindowBase_Close_ReleasesNativeBackgroundBrush() => WpfTestHost.RunVoid(() =>
    {
        FieldInfo brushField = typeof(WindowBase).GetField("_hBrush", BindingFlags.Instance | BindingFlags.NonPublic)!;
        ApplyTheme();
        var window = new WindowBase { Width = 240, Height = 160, ShowInTaskbar = false };
        try
        {
            window.Show();
            WpfTestHost.Pump(200);
            Assert.AreNotEqual(
                IntPtr.Zero,
                (IntPtr)brushField.GetValue(window)!,
                $"Loaded 之后应已创建背景画刷（IsLoaded={window.IsLoaded}, 合并字典={Application.Current.Resources.MergedDictionaries.Count}, "
                + $"WindowBackgroundBrush={window.TryFindResource("WindowBackgroundBrush")?.GetType().Name ?? "未找到"}）");
        }
        finally
        {
            window.Close();
            WpfTestHost.Pump(100);
        }

        // 修复前：每个窗口创建的 GDI 画刷在窗口关闭后永远不会被删除
        Assert.AreEqual(IntPtr.Zero, (IntPtr)brushField.GetValue(window)!);
    });

    [TestMethod]
    public void WindowBase_DpiChanged_UpdatesCachedScale() => WpfTestHost.RunVoid(() =>
    {
        var window = new WindowBase();
        MethodInfo onDpiChanged = typeof(WindowBase).GetMethod("OnDpiChanged", BindingFlags.Instance | BindingFlags.NonPublic)!;

        onDpiChanged.Invoke(window, [new DpiScale(1, 1), new DpiScale(1.5, 2)]);

        // 修复前：DPI 比例只在 SourceInitialized 读取一次，跨显示器拖动后 WM_GETMINMAXINFO 与吸附判断继续使用旧比例
        Assert.AreEqual(1.5, (double)typeof(WindowBase).GetField("_dpiX", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!);
        Assert.AreEqual(2.0, (double)typeof(WindowBase).GetField("_dpiY", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!);
    });

    [TestMethod]
    public void SkinHandler_SetSkin_PersistsBeforeNotifyingSubscribers() => WpfTestHost.RunVoid(() =>
    {
        SkinHandler.SetSkin(SkinType.Dark, false);
        SkinHandler.Save(SkinType.Dark);
        SkinType? seenBySubscriber = null;
        EventHandler<EventSkinResult> handler = (_, _) => seenBySubscriber = SkinHandler.GetSkin();
        SkinHandler.OnSkinEvent += handler;
        try
        {
            SkinHandler.SetSkin(SkinType.Light);
        }
        finally
        {
            SkinHandler.OnSkinEvent -= handler;
            SkinHandler.SetSkin(SkinType.Dark, false);
        }

        // 修复前：通知先于保存，订阅者在回调里通过 GetSkin() 读到的仍是旧皮肤（Dark）
        Assert.AreEqual(SkinType.Light, seenBySubscriber);
    });

    [TestMethod]
    public void SkinHandler_SetSkin_KeepsThemeDictionaryPosition() => WpfTestHost.RunVoid(() =>
    {
        SkinHandler.SetSkin(SkinType.Dark, false);
        var merged = Application.Current.Resources.MergedDictionaries;
        var hostDictionary = new ResourceDictionary();
        merged.Add(hostDictionary);

        SkinHandler.SetSkin(SkinType.Light, false);

        // 修复前：新主题被追加到末尾，反过来覆盖宿主在它之后合并的资源字典
        Assert.EndsWith("LightTheme.xaml", merged[0].Source!.OriginalString);
        Assert.AreSame(hostDictionary, merged[1]);

        SkinHandler.SetSkin(SkinType.Dark, false);
    });

    [TestMethod]
    public async Task LanguageHandler_SetLanguageAsync_PreCanceledToken_DoesNotChangeCulture()
    {
        string before = WpfTestHost.Run(() =>
        {
            LanguageHandler.SetLanguage(LanguageType.zh);
            return LocalizeDictionary.Instance.Culture.Name;
        });
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => WpfTestHost.Run(() => LanguageHandler.SetLanguageAsync(LanguageType.en, cts.Token)));

        // 修复前：文化先被改成 en，随后才在写文件时观察到取消，留下"文化已变、配置与事件未更新"的半应用状态
        Assert.AreEqual(before, WpfTestHost.Run(() => LocalizeDictionary.Instance.Culture.Name));
    }

    [TestMethod]
    public void IconsHandler_BadExternalFile_KeepsBuiltInIconsAndIsNotCached() => WpfTestHost.RunVoid(() =>
    {
        SkinHandler.SetSkin(SkinType.Dark, false);
        Assert.IsInstanceOfType<DrawingImage>(Application.Current.TryFindResource("Count"));

        Exception? thrown = null;
        try
        {
            IconsHandler.Loading("pack://application:,,,/Nope.Missing;component/x.xaml");
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        Assert.IsNotNull(thrown, "加载失败仍应告知调用方");
        // 修复前：旧字典已被移除而新字典没能构造出来，全部内置图标丢失
        Assert.IsInstanceOfType<DrawingImage>(Application.Current.TryFindResource("Count"));

        // 修复前：坏路径留在缓存里，之后每次换肤都再次失败并丢掉内置图标
        SkinHandler.SetSkin(SkinType.Light, false);
        WpfTestHost.Pump(300);
        Assert.IsInstanceOfType<DrawingImage>(Application.Current.TryFindResource("Count"));
    });

    [TestMethod]
    public void IconsHandler_GetIconOnBackgroundThread_DoesNotPoisonUiThreadCache() => WpfTestHost.RunVoid(() =>
    {
        SkinHandler.SetSkin(SkinType.Dark, false);
        DrawingImage? background = null;
        Task.Run(() => background = IconsHandler.GetIcon("Count")).Wait();

        DrawingImage onUiThread = IconsHandler.GetIcon("Count")!;

        Assert.AreNotSame(background, onUiThread);
        // 修复前：缓存里是后台线程创建的实例，UI 线程取到后访问属性抛 InvalidOperationException（跨线程）
        Assert.IsNotNull(onUiThread.Drawing);
    });

    [TestMethod]
    public void InjectionWpf_CachedWindow_IsEvictedWhenClosed() => WpfTestHost.RunVoid(() =>
    {
        var first = InjectionWpf.Window<CachedWindow, CachedViewModel>(true);
        first.ShowInTaskbar = false;
        first.Show();
        WpfTestHost.Pump(100);
        first.Close();
        WpfTestHost.Pump(100);

        var second = InjectionWpf.Window<CachedWindow, CachedViewModel>(true);

        // 修复前：返回已关闭的旧实例，Show() 抛 InvalidOperationException（Cannot set Visibility ... after a Window has closed）
        Assert.AreNotSame(first, second);
        second.ShowInTaskbar = false;
        second.Show();
        second.Close();
    });

    [TestMethod]
    public async Task InjectionWpf_WindowAsync_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // 修复前：取消被包装成普通 Exception（InnerException 才是 TaskCanceledException）
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => WpfTestHost.Run(() => InjectionWpf.WindowAsync<CancelWindow, CancelViewModel>(false, cts.Token)));
    }
}
