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

[assembly: DoNotParallelize]

namespace Snet.Windows.Tests;

[TestClass]
public sealed class RegressionTests
{
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
    public void BitmapSourceTypeConverter_RejectsUnsupportedValues()
    {
        var converter = new BitmapSourceTypeConverter();

        Assert.ThrowsExactly<NotSupportedException>(
            () => converter.ConvertTo(null, null, new object(), typeof(System.Drawing.Bitmap)));
    }
}
