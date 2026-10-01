using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snet.Windows.Controls.edit.Document;
using Snet.Windows.Controls.edit.Folding;
using Snet.Windows.Controls.edit.Search;
using Snet.Windows.Controls.handler;
using Snet.Windows.Controls.property.wpf;
using Snet.Windows.Core.handler;
using Snet.Windows.Core.localize.wpf.TypeConverters;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Snet.Windows.Controls.drag;
using Snet.Windows.Controls.ledgauge;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using DataGrid = Snet.Windows.Controls.property.wpf.DataGrid;
using ColumnDefinition = Snet.Windows.Controls.property.wpf.ColumnDefinition;

[assembly: DoNotParallelize]

namespace Snet.Windows.Tests;

[TestClass]
public sealed class RegressionTests
{
    [STATestMethod]
    public void DragContextMenu_PreservesEditingMenusAndBothRedHandlesOpenOperationMenu()
    {
        var editor = new TextBox { Text = "可编辑文本", ContextMenu = new ContextMenu() };
        var copy = new UserControl { Content = editor, Width = 200, Height = 100, ContextMenu = new ContextMenu() };
        var canvas = new Canvas();
        canvas.Children.Add(copy);
        var window = new Window { Content = canvas, Width = 400, Height = 300, ShowInTaskbar = false };
        var drag = new DragControlsAnimate(window, canvas);
        var adorner = new DragControlsBase(copy, canvas, true, true, true);
        var nativeMenu = copy.ContextMenu;
        var editorMenu = editor.ContextMenu;
        var menuProperty = (DependencyProperty)typeof(DragControlsBase)
            .GetField("OperationContextMenuProperty", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        try
        {
            window.Show();
            drag.AttachCopyMenu(copy);
            var originalMenu = copy.GetValue(menuProperty) as ContextMenu;
            Assert.IsNotNull(originalMenu);
            Assert.AreEqual(6, originalMenu.Items.Count);
            drag.AttachCopyMenu(copy);
            Assert.AreSame(originalMenu, copy.GetValue(menuProperty));
            Assert.AreSame(nativeMenu, copy.ContextMenu);
            Assert.AreSame(editorMenu, editor.ContextMenu);
            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
            {
                RoutedEvent = Mouse.PreviewMouseUpEvent
            };
            editor.RaiseEvent(args);
            Assert.IsFalse(originalMenu.IsOpen);
            Assert.AreSame(editorMenu, editor.ContextMenu);

            // 模拟语言切换时替换菜单，两处装饰点必须打开新菜单而非旧引用。
            var replacement = new ContextMenu();
            replacement.Items.Add(new MenuItem { Header = "控件操作" });
            copy.SetValue(menuProperty, replacement);
            foreach (string fieldName in new[] { "CentreThumb", "RotateThumb" })
            {
                var thumb = (Thumb)typeof(DragControlsBase)
                    .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(adorner)!;
                var rightClick = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
                {
                    RoutedEvent = Mouse.PreviewMouseUpEvent
                };
                thumb.RaiseEvent(rightClick);
                Assert.IsTrue(rightClick.Handled, fieldName);
                Assert.IsTrue(replacement.IsOpen, fieldName);
                Assert.AreSame(copy, replacement.PlacementTarget);
                replacement.IsOpen = false;
            }
            Assert.AreSame(nativeMenu, copy.ContextMenu);

            // 原生 TextBox 本体也不能被控件操作菜单替换。
            var textBox = new TextBox();
            drag.AttachCopyMenu(textBox);
            Assert.IsNull(textBox.ContextMenu);
            Assert.IsInstanceOfType<ContextMenu>(textBox.GetValue(menuProperty));
            drag.RemoveCopy(textBox);
        }
        finally
        {
            if (copy.ContextMenu != null) copy.ContextMenu.IsOpen = false;
            adorner.Detach();
            drag.RemoveCopy(copy);
            drag.Detach();
            window.Close();
        }
    }

    [STATestMethod]
    public void DragClone_KeepsUserControlVisualTreesIndependent()
    {
        var source = new LedGaugeControl { Width = 60, Height = 60, IsOn = true };
        object originalContent = source.Content;
        var clone = (LedGaugeControl)DragControlsAnimate.DefaultClone(source);

        Assert.AreSame(originalContent, source.Content);
        Assert.AreNotSame(originalContent, clone.Content);
        Assert.IsTrue(clone.IsOn);
        Assert.AreEqual(60d, clone.Width);
        Assert.AreSame(source, LogicalTreeHelper.GetParent((DependencyObject)originalContent));
    }

    [STATestMethod]
    public void DragCapture_InternalTransitionDoesNotCancelClone()
    {
        var source = new Button();
        var canvas = new Canvas();
        var drag = new DragControlsAnimate(canvas, canvas);
        var clone = new Button();
        Type type = typeof(DragControlsAnimate);
        type.GetField("ControlsObj", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(drag, clone);
        type.GetField("IsMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(drag, true);
        type.GetField("_isStartingDrag", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(drag, true);
        var args = new MouseEventArgs(Mouse.PrimaryDevice, 0)
        {
            RoutedEvent = Mouse.LostMouseCaptureEvent,
            Source = source
        };
        type.GetMethod("ControlsShow_LostMouseCapture", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(drag, [source, args]);

        Assert.AreSame(clone, type.GetField("ControlsObj", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(drag));
        drag.Detach();
    }

    [STATestMethod]
    public void DragStart_TakesCaptureFromChildWithoutLosingClone()
    {
        var child = new TextBox();
        var source = new UserControl { Content = child, Width = 130, Height = 30 };
        var canvas = new Canvas { Width = 300, Height = 200 };
        var panel = new StackPanel();
        panel.Children.Add(source);
        panel.Children.Add(canvas);
        var window = new Window { Content = panel, Width = 400, Height = 300, ShowInTaskbar = false };
        var drag = new DragControlsAnimate(window, canvas, [source]);
        drag.MessageEvenTrigger = (_, _) => { };
        try
        {
            window.Show();
            Assert.IsTrue(child.CaptureMouse());
            typeof(DragControlsAnimate).GetMethod("StartDrag", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(drag, [source, new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.PreviewMouseMoveEvent }]);

            Assert.AreEqual(1, canvas.Children.Count);
            Assert.AreSame(child, source.Content);
            Assert.AreSame(source, Mouse.Captured);
        }
        finally
        {
            Mouse.Capture(null);
            drag.Detach();
            window.Close();
        }
    }

    [STATestMethod]
    public void DragCanvas_PositionSurvivesLayoutSerialization()
    {
        var canvas = new Canvas();
        var control = new Border { Width = 200, Height = 160 };
        canvas.Children.Add(control);
        Canvas.SetLeft(control, 10);
        Canvas.SetTop(control, 20);
        typeof(DragControlsBase).GetMethod("SetLayoutPosition", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [control, new Point(175, 95)]);
        var item = new DragControlsLayoutItem
        {
            X = Canvas.GetLeft(control), Y = Canvas.GetTop(control), Width = control.Width, Height = control.Height
        };
        string json = System.Text.Json.JsonSerializer.Serialize(item);
        var restored = System.Text.Json.JsonSerializer.Deserialize<DragControlsLayoutItem>(json)!;

        Assert.AreEqual(175d, restored.X);
        Assert.AreEqual(95d, restored.Y);
        Assert.AreEqual(new Thickness(0), control.Margin);
    }

    [STATestMethod]
    public void DragResize_RotatedOppositeAnchorStaysFixed()
    {
        foreach (double angle in new[] { 0d, 45d, 90d, 135d, 270d })
        foreach (int sx in new[] { -1, 0, 1 })
        foreach (int sy in new[] { -1, 0, 1 })
        {
            if (sx == 0 && sy == 0) continue;
            var canvas = new Canvas();
            var control = new Border { Width = 200, Height = 160 };
            canvas.Children.Add(control);
            Canvas.SetLeft(control, 120);
            Canvas.SetTop(control, 80);
            var adorner = new DragControlsBase(control, canvas, false, true, false);
            adorner.SetRotation(angle);
            var start = new Rect(120, 80, 200, 160);
            typeof(DragControlsBase).GetField("resizeStartBounds", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(adorner, start);
            var thumb = new Thumb
            {
                HorizontalAlignment = sx < 0 ? HorizontalAlignment.Left : sx > 0 ? HorizontalAlignment.Right : HorizontalAlignment.Center,
                VerticalAlignment = sy < 0 ? VerticalAlignment.Top : sy > 0 ? VerticalAlignment.Bottom : VerticalAlignment.Center
            };
            var rotate = new RotateTransform(angle);
            Point anchorBefore = new Point(start.X + start.Width / 2, start.Y + start.Height / 2)
                + (Vector)rotate.Transform(new Point(-sx * start.Width / 2, -sy * start.Height / 2));
            var resize = typeof(DragControlsBase).GetMethod("ApplyResize", BindingFlags.Instance | BindingFlags.NonPublic)!;
            resize.Invoke(adorner, [thumb, new Vector(sx * 10, sy * 10)]);
            resize.Invoke(adorner, [thumb, new Vector(sx * 30, sy * 20)]);
            Point anchorAfter = new Point(Canvas.GetLeft(control) + control.Width / 2, Canvas.GetTop(control) + control.Height / 2)
                + (Vector)rotate.Transform(new Point(-sx * control.Width / 2, -sy * control.Height / 2));

            Assert.AreEqual(anchorBefore.X, anchorAfter.X, 0.000001, $"angle={angle}, direction={sx},{sy}");
            Assert.AreEqual(anchorBefore.Y, anchorAfter.Y, 0.000001);
            Assert.AreEqual(sx == 0 ? 200d : 230d, control.Width);
            Assert.AreEqual(sy == 0 ? 160d : 180d, control.Height);
            Assert.AreEqual(new Thickness(0), control.Margin);
        }
    }

    [TestMethod]
    public void NaturalStringComparer_OrdersNumbersBeyondInt32WithoutThrowing()
    {
        var comparer = new NaturalStringComparer();

        Assert.IsLessThan(0, comparer.Compare("item2147483647", "item2147483648"));
        Assert.IsLessThan(0, comparer.Compare("item99999999999999999999", "item100000000000000000000"));
        Assert.AreNotEqual(0, comparer.Compare("item1", "item01"));
    }

    [TestMethod]
    public void RegexSearch_AbortsCatastrophicBacktracking()
    {
        ISearchStrategy strategy = SearchStrategyFactory.Create("(a+)+$", false, false, SearchMode.RegEx);
        var document = new TextDocument(new string('a', 50_000) + "!");

        Assert.ThrowsExactly<RegexMatchTimeoutException>(() => strategy.FindAll(document, 0, document.TextLength).ToList());
    }

    [TestMethod]
    public async Task UiMessageHandler_DisposeAsync_CancelsOwnedBackgroundTask()
    {
        var handler = new UiMessageHandler();
        await handler.StartAsync(intervalMs: 10);

        var taskField = typeof(UiMessageHandler).GetField("_logTask", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(taskField);
        var backgroundTask = taskField.GetValue(handler) as Task;
        Assert.IsNotNull(backgroundTask);

        await handler.DisposeAsync();

        Assert.IsTrue(backgroundTask.IsCompleted);
        Assert.IsFalse(handler.IsRunning);
    }

    [TestMethod]
    public async Task GetLanguageAsync_PropagatesPreCanceledToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => LanguageHandler.GetLanguageAsync(cancellationSource.Token));
    }

    [TestMethod]
    public void LineManager_RebuildMarksEveryReplacedLineDeleted()
    {
        var document = new TextDocument("first\nsecond\nthird");
        DocumentLine second = document.GetLineByNumber(2);
        DocumentLine third = document.GetLineByNumber(3);
        FieldInfo? lineManagerField = typeof(TextDocument).GetField("lineManager", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(lineManagerField);
        object? lineManager = lineManagerField.GetValue(document);
        Assert.IsNotNull(lineManager);

        lineManager.GetType().GetMethod("Rebuild")!.Invoke(lineManager, null);

        Assert.IsTrue(second.IsDeleted);
        Assert.IsTrue(third.IsDeleted);
    }

    [TestMethod]
    public void FoldingManager_UpdateFoldingsUsesCoercedOffsets()
    {
        var document = new TextDocument("abcdef");
        var manager = new FoldingManager(document);

        manager.UpdateFoldings([new NewFolding(3, 100)], -1);

        FoldingSection folding = manager.AllFoldings.Single();
        Assert.AreEqual(3, folding.StartOffset);
        Assert.AreEqual(document.TextLength, folding.EndOffset);
    }

    [STATestMethod]
    public void HeaderedEntrySlider_SettersKeepDependencyPropertiesIndependent()
    {
        var slider = new HeaderedEntrySlider
        {
            EntryStringFormat = "F2",
            EntryContentAlignment = System.Windows.HorizontalAlignment.Right
        };

        Assert.AreEqual("F2", slider.EntryStringFormat);
        Assert.AreEqual(System.Windows.HorizontalAlignment.Right, slider.EntryContentAlignment);
    }

    [STATestMethod]
    public void WrapItemsOperator_UsesCeilingAndHandlesEmptyDefinitions()
    {
        var grid = new DataGrid { ItemsSource = new System.Collections.ArrayList { 1, 2, 3, 4, 5, 6, 7 } };
        var sut = new WrapItemsOperator(grid);
        Assert.AreEqual(0, sut.GetRowCount());
        Assert.AreEqual(0, sut.GetColumnCount());

        grid.PropertyDefinitions.Add(new ColumnDefinition());
        grid.PropertyDefinitions.Add(new ColumnDefinition());
        grid.PropertyDefinitions.Add(new ColumnDefinition());

        Assert.AreEqual(3, sut.GetRowCount());
        Assert.AreEqual(3, sut.GetColumnCount());
    }

    [TestMethod]
    public void TrayRectInterop_UsesFourNativeLongs()
    {
        Type? rectType = typeof(UiMessageHandler).Assembly.GetType("Snet.Windows.Controls.tray.Interop.User32+RECT");
        Assert.IsNotNull(rectType);
        Assert.AreEqual(16, Marshal.SizeOf(rectType));
    }

    [TestMethod]
    public void TrayTooltipInterop_UsesUnicodeEntryPointAndLayout()
    {
        Type? shellType = typeof(UiMessageHandler).Assembly.GetType("Snet.Windows.Controls.tray.Interop.Shell32");
        Assert.IsNotNull(shellType);
        MethodInfo? method = shellType.GetMethod("Shell_NotifyIcon");
        Assert.IsNotNull(method);
        DllImportAttribute? import = method.GetCustomAttribute<DllImportAttribute>();
        Assert.IsNotNull(import);

        Assert.AreEqual("Shell_NotifyIconW", import.EntryPoint);
        Assert.AreEqual(CharSet.Unicode, import.CharSet);
        Assert.IsTrue(import.ExactSpelling);

        Type dataType = method.GetParameters()[1].ParameterType;
        Assert.AreEqual(CharSet.Unicode, dataType.StructLayoutAttribute!.CharSet);
        Assert.AreEqual(IntPtr.Size == 8 ? 976 : 956, Marshal.SizeOf(dataType));
    }

    [TestMethod]
    public void BitmapSourceTypeConverter_RejectsUnsupportedValues()
    {
        var converter = new BitmapSourceTypeConverter();

        Assert.ThrowsExactly<NotSupportedException>(
            () => converter.ConvertTo(null, null, new object(), typeof(System.Drawing.Bitmap)));
    }
}
