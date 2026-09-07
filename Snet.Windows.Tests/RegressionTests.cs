using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snet.Windows.Controls.edit.Document;
using Snet.Windows.Controls.edit.Search;
using Snet.Windows.Controls.handler;
using Snet.Windows.Controls.property.wpf;
using Snet.Windows.Core.handler;
using System.Reflection;
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
}
