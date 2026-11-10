using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snet.Windows.Controls.edit;
using Snet.Windows.Controls.edit.Document;
using Snet.Windows.Controls.edit.Folding;
using Snet.Windows.Controls.edit.Search;
using Snet.Windows.Controls.handler;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace Snet.Windows.Tests;

/// <summary>
/// edit 模块（AvalonEdit 移植）审查（2026-11）发现的缺陷的回归测试。
/// </summary>
[TestClass]
public sealed class EditRegressionTests
{
    public sealed class BindingSource : INotifyPropertyChanged
    {
        private string _code = string.Empty;

        public string Code
        {
            get => _code;
            set
            {
                _code = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Code)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    [STATestMethod]
    public void EditBindingHandler_EmptyInitialSource_WritesTypedTextBack()
    {
        var source = new BindingSource { Code = string.Empty };
        var editor = new TextEditor();
        BindingOperations.SetBinding(editor, EditBindingHandler.EditTextProperty, new Binding(nameof(BindingSource.Code))
        {
            Source = source,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });

        editor.Text = "typed";

        // 修复前：源的初值是空串，与依赖属性默认值相同，属性变更回调不触发，TextChanged 从未订阅，用户输入不会回写到绑定源
        Assert.AreEqual("typed", source.Code);
        Assert.AreEqual("typed", EditBindingHandler.GetEditText(editor));
    }

    [TestMethod]
    public void LiteralSearchResult_ReplaceWith_ExpandsLikeTheRegexPath()
    {
        var document = new TextDocument("xx abc yy ABC");

        // 区分大小写的字面量搜索：修复前 ReplaceWith 抛 NullReferenceException（SearchResult.Data 为 null）
        ISearchResult sensitive = SearchStrategyFactory.Create("abc", false, false, SearchMode.Normal)
            .FindAll(document, 0, document.TextLength).Single();
        Assert.AreEqual("[abc]", sensitive.ReplaceWith("[$&]"));
        Assert.AreEqual("$", sensitive.ReplaceWith("$$"));
        Assert.AreEqual("plain", sensitive.ReplaceWith("plain"));

        // 忽略大小写时 $& 必须是文档里的实际文本，而不是搜索模式
        ISearchResult[] insensitive = SearchStrategyFactory.Create("abc", true, false, SearchMode.Normal)
            .FindAll(document, 0, document.TextLength).ToArray();
        Assert.HasCount(2, insensitive);
        Assert.AreEqual("[ABC]", insensitive[1].ReplaceWith("[$&]"));
    }

    [TestMethod]
    public void FoldingMargin_MergingLinesThatCarryMarkers_DoesNotThrowDuringLayout() => WpfTestHost.RunVoid(() =>
    {
        // TextEditorControl 的静态构造会解析 pack:// URI，需要宿主线程上已存在 Application（注册 pack 方案）
        var editor = new TextEditorControl
        {
            Width = 400,
            Height = 300,
            Document = new TextDocument("head\n{\n  body\n}\ntail\n")
        };
        var window = new Window { Content = editor, Width = 500, Height = 400, ShowInTaskbar = false };
        FoldingManager manager = FoldingManager.Install(editor.TextArea);
        try
        {
            window.Show();
            editor.UpdateLayout();

            // 折叠区从第 2 行（"{"）开始：布局后该行的 VisualLine 上会挂一个折叠标记
            DocumentLine openBrace = editor.Document.GetLineByNumber(2);
            DocumentLine closeBrace = editor.Document.GetLineByNumber(4);
            manager.CreateFolding(openBrace.Offset, closeBrace.EndOffset);
            editor.UpdateLayout();

            // 合并第 1、2 行：删除第 1 行的换行符，第 2 行的 DocumentLine 随之被标记为已删除
            DocumentLine head = editor.Document.GetLineByNumber(1);
            editor.Document.Remove(head.EndOffset, head.DelimiterLength);

            // 修复前：FoldingMargin 在布局期间读取旧标记所指已删除行的 Offset，抛 InvalidOperationException
            editor.UpdateLayout();
        }
        finally
        {
            window.Close();
        }
    });
}
