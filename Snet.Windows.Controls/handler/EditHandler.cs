using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using Snet.Unility;
using Snet.Windows.Controls.data;
using Snet.Windows.Core.handler;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace Snet.Windows.Controls.handler
{
    /// <summary>
    /// AvalonEdit编辑器的高性能扩展类<br/>
    /// 提供以下功能：<br/>
    /// 1. 自动补全（支持前缀过滤/空格触发/全词替换）<br/>
    /// 2. 语法高亮（逐词扫描，关键字着色）<br/>
    /// 3. 主题切换（白天/黑夜模式）<br/>
    /// 4. 悬停提示（显示关键字说明）<br/>
    /// 5. 行号显示控制<br/>
    /// </summary>

    public class EditHandler : IDisposable
    {
        private readonly TextEditor _editor;                       // 编辑器实例
        private readonly Dictionary<string, EditModel> _kwMap;   // 关键字映射表（使用忽略大小写的比较器）
        private CompletionWindow? _completionWindow;               // 当前活动的补全窗口
        private readonly ToolTip _hoverTooltip = new();            // 悬停提示控件
        private readonly KeywordColorizer _colorizer;              // 语法高亮逻辑实例
        private bool _isDark;                                      // 当前主题状态（是否为暗色主题）
        private bool _disposed;                                    // 资源释放标记

        // 缓存的主题背景画刷
        private Brush _dark;
        private Brush _light;

        /// <summary>补全项高度估算（像素，用于计算最大显示高度）</summary>
        public int ItemHeight { get; set; } = 30;

        /// <summary>补全框最大显示行数</summary>
        public int MaxCompletionRows;

        // 事件处理器缓存（用于后续解绑事件）
        private readonly TextCompositionEventHandler _textEnteredHandler;
        private readonly MouseEventHandler _textViewMouseHoverHandler;
        private readonly MouseEventHandler _textViewMouseHoverStoppedHandler;
        private readonly MouseButtonEventHandler _editorPreviewMouseDownHandler;

        /// <summary>
        /// 构造函数：初始化扩展功能
        /// </summary>
        /// <param name="editor">目标文本编辑器</param>
        /// <param name="keywords">关键字集合</param>
        /// <param name="maxCompletionRows">最大补全行数</param>
        /// <param name="showLineNumbers">是否显示行号</param>
        /// <param name="color">颜色,不传入使用皮肤事件触发后的颜色</param>
        public EditHandler(TextEditor editor, IEnumerable<EditModel> keywords, int maxCompletionRows = 8, bool showLineNumbers = true, (string dark, string light)? color = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));

            if (color != null && !color.Value.dark.IsNullOrWhiteSpace() && !color.Value.light.IsNullOrWhiteSpace())
            {
                _dark = CloneBrush(new SolidColorBrush((Color)ColorConverter.ConvertFromString(color.Value.dark)!));
                if (_dark.CanFreeze) _dark.Freeze();// 冻结画刷以提高性能（避免多次修改）
                _light = CloneBrush(new SolidColorBrush((Color)ColorConverter.ConvertFromString(color.Value.light)!));
                if (_light.CanFreeze) _light.Freeze();// 冻结画刷以提高性能（避免多次修改）
            }

            _editor.ShowLineNumbers = showLineNumbers;
            MaxCompletionRows = Math.Max(1, maxCompletionRows);

            // 初始化关键字字典（使用不区分大小写的比较器）
            _kwMap = new Dictionary<string, EditModel>(StringComparer.OrdinalIgnoreCase);
            SetKeywords(keywords);

            // 初始化语法高亮器
            _colorizer = new KeywordColorizer(_kwMap, () => _editor.Foreground ?? Brushes.Black);
            _editor.TextArea.TextView.LineTransformers.Add(_colorizer);

            // 初始化事件处理器（缓存以便后续解绑）
            _textEnteredHandler = Editor_TextEntered;
            _textViewMouseHoverHandler = TextView_MouseHover;
            _textViewMouseHoverStoppedHandler = (s, e) => _hoverTooltip.IsOpen = false;
            _editorPreviewMouseDownHandler = (s, e) => _completionWindow?.Close();

            // 注册事件处理
            _editor.TextArea.TextEntered += _textEnteredHandler;
            _editor.TextArea.TextView.MouseHover += _textViewMouseHoverHandler;
            _editor.TextArea.TextView.MouseHoverStopped += _textViewMouseHoverStoppedHandler;
            _editor.PreviewMouseDown += _editorPreviewMouseDownHandler;

            SkinHandler.OnSkinEventAsync -= SkinHandler_OnSkinEventAsync;
            SkinHandler.OnSkinEventAsync += SkinHandler_OnSkinEventAsync;
        }

        /// <summary>
        /// 皮肤切换事件处理
        /// </summary>
        /// <param name="sender">源</param>
        /// <param name="e">事件</param>
        private Task SkinHandler_OnSkinEventAsync(object? sender, Snet.Windows.Core.data.EventSkinResult e)
        {
            switch (e.Skin.Value)
            {
                case Snet.Windows.Core.@enum.SkinType.Dark:
                    if (_dark == null)
                    {
                        _dark = CloneBrush(new SolidColorBrush((Color)ColorConverter.ConvertFromString(sender.ToString())!));
                        if (_dark.CanFreeze) _dark.Freeze();// 冻结画刷以提高性能（避免多次修改）
                    }
                    SetTheme(true);
                    break;
                case Snet.Windows.Core.@enum.SkinType.Light:
                    if (_light == null)
                    {
                        _light = CloneBrush(new SolidColorBrush((Color)ColorConverter.ConvertFromString(sender.ToString())!));
                        if (_light.CanFreeze) _light.Freeze();// 冻结画刷以提高性能（避免多次修改）
                    }
                    SetTheme(false);
                    break;
            }
            return Task.CompletedTask;
        }

        #region 关键词管理
        /// <summary>
        /// 设置关键字集合<br/>
        /// 会清空现有关键字缓存，并强制重绘编辑器
        /// </summary>
        /// <param name="keywords">新的关键字集合</param>
        public void SetKeywords(IEnumerable<EditModel> keywords)
        {
            _kwMap.Clear();
            foreach (var k in keywords.Where(k => !string.IsNullOrWhiteSpace(k?.Name)))
            {
                // 将关键字添加到缓存字典
                _kwMap[k.Name] = new EditModel
                {
                    Name = k.Name,
                    Description = k.Description,
                    Color = k.Color,
                };
            }
            _editor.TextArea.TextView.InvalidateVisual(); // 强制重绘视图
        }

        /// <summary>
        /// 画刷克隆方法<br/>
        /// 创建画刷的副本并冻结以提高性能
        /// </summary>
        /// <param name="brush">原始画刷</param>
        /// <returns>克隆后的画刷</returns>
        private static Brush CloneBrush(Brush brush)
        {
            try
            {
                var clone = brush.CloneCurrentValue();
                if (clone.CanFreeze) clone.Freeze();
                return clone;
            }
            catch { return Brushes.DodgerBlue; }
        }
        #endregion

        #region 自动补全
        /// <summary>
        /// 文本输入事件处理<br/>
        /// 根据输入字符触发不同类型的补全
        /// </summary>
        private void Editor_TextEntered(object? sender, TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.Text)) return;
            char ch = e.Text[0];

            // 字母数字和下划线触发前缀过滤补全
            if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                ShowCompletion(triggeredBySpace: false);
            }
            // 空格键触发全量补全
            else if (ch == ' ')
            {
                if (_completionWindow != null) _completionWindow.Close(); // 已存在补全窗口时关闭
                else ShowCompletion(triggeredBySpace: true);              // 否则显示全量补全
            }
        }

        /// <summary>
        /// 显示补全窗口
        /// </summary>
        /// <param name="triggeredBySpace">是否由空格触发（true显示全量，false按前缀过滤）</param>
        private void ShowCompletion(bool triggeredBySpace = false)
        {
            var doc = _editor.Document;
            if (doc == null) return;

            int caret = _editor.CaretOffset;
            if (caret < 0) return;

            // 获取当前输入的前缀文本
            int start = caret;
            while (start > 0 && IsWordChar(doc.GetCharAt(start - 1))) start--;
            string prefix = caret > start ? doc.GetText(start, caret - start) : string.Empty;

            // 根据触发方式筛选匹配的关键字
            var matches = triggeredBySpace
                ? _kwMap.Values
                : _kwMap.Values.Where(k => k.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            if (!matches.Any())
            {
                _completionWindow?.Close();
                _completionWindow = null;
                return;
            }

            // 已存在补全窗口时刷新数据
            if (_completionWindow != null)
            {
                var list = _completionWindow.CompletionList.CompletionData;
                list.Clear();
                foreach (var kw in matches) list.Add(new KeywordCompletionData(kw));
                return;
            }

            // 创建新的补全窗口
            _completionWindow = new CompletionWindow(_editor.TextArea)
            {
                MaxHeight = MaxCompletionRows * ItemHeight,  // 计算最大高度
                WindowStyle = WindowStyle.None,              // 无窗口样式
                AllowsTransparency = true,                   // 允许透明
                BorderThickness = new Thickness(1),          // 边框厚度
            };

            // 应用当前主题到补全窗口
            ApplyCompletionTheme(_completionWindow);
            foreach (var kw in matches) _completionWindow.CompletionList.CompletionData.Add(new KeywordCompletionData(kw));

            // 注册关闭事件清理引用
            _completionWindow.Closed += (_, _) => _completionWindow = null;
            _completionWindow.Show();
        }

        /// <summary>
        /// 补全项数据实现类
        /// </summary>
        private sealed class KeywordCompletionData : ICompletionData
        {
            private readonly EditModel _kw;
            public KeywordCompletionData(EditModel kw) => _kw = kw;

            public ImageSource? Image => null;
            public string Text => _kw.Name;
            public object Content => _kw.Name;
            public object Description => _kw.Description;
            public double Priority => 0;

            /// <summary>
            /// 补全操作执行方法<br/>
            /// 会替换整个单词而不仅仅是部分匹配
            /// </summary>
            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                var doc = textArea.Document;
                int start = completionSegment.Offset;
                int end = completionSegment.EndOffset;

                // 扩展选区以包含整个单词
                while (start > 0 && IsWordChar(doc.GetCharAt(start - 1))) start--;
                while (end < doc.TextLength && IsWordChar(doc.GetCharAt(end))) end++;

                // 替换文本并设置光标位置
                doc.Replace(start, end - start, Text);
                textArea.Caret.Offset = start + Text.Length;
            }
        }
        #endregion

        #region 语法高亮
        /// <summary>
        /// 关键字高亮转换器<br/>
        /// 继承自DocumentColorizingTransformer，逐行处理文本高亮
        /// </summary>
        private sealed class KeywordColorizer : DocumentColorizingTransformer
        {
            private readonly Dictionary<string, EditModel> _kwMap;
            private readonly Func<Brush> _defaultBrushProvider;

            public KeywordColorizer(Dictionary<string, EditModel> kwMap, Func<Brush> defaultBrushProvider)
            {
                _kwMap = kwMap;
                _defaultBrushProvider = defaultBrushProvider;
            }

            /// <summary>
            /// 逐行着色处理<br/>
            /// 先设置整行默认颜色，然后扫描并高亮关键字
            /// </summary>
            /// <param name="line">当前处理的行</param>
            protected override void ColorizeLine(DocumentLine line)
            {
                if (line.IsDeleted) return;

                var doc = CurrentContext.Document;
                string text = doc.GetText(line);

                // 设置整行默认前景色
                Brush defaultBrush = _defaultBrushProvider() ?? Brushes.Black;
                ChangeLinePart(line.Offset, line.EndOffset, e => e.TextRunProperties.SetForegroundBrush(defaultBrush));

                // 扫描行中的单词token
                int i = 0;
                while (i < text.Length)
                {
                    if (IsWordChar(text[i]))
                    {
                        int s = i;
                        // 找到单词结束位置
                        while (i < text.Length && IsWordChar(text[i])) i++;
                        string token = text.Substring(s, i - s);

                        // 如果是关键字则应用颜色
                        if (_kwMap.TryGetValue(token, out var kw) && kw.Color != null)
                        {
                            ChangeLinePart(line.Offset + s, line.Offset + i, e =>
                                e.TextRunProperties.SetForegroundBrush(CloneBrush(new SolidColorBrush((Color)ColorConverter.ConvertFromString(kw.Color)!))));
                        }
                    }
                    else i++;
                }
            }
        }
        #endregion

        #region 悬停提示 & 主题管理
        /// <summary>
        /// 鼠标悬停事件处理<br/>
        /// 显示当前单词的关键字描述
        /// </summary>
        private void TextView_MouseHover(object? sender, MouseEventArgs e)
        {
            var textView = _editor.TextArea.TextView;
            var posInView = e.GetPosition(textView);
            var pos = textView.GetPosition(posInView + textView.ScrollOffset);
            if (pos == null) return;

            // 获取光标位置的单词
            int offset = _editor.Document.GetOffset(pos.Value.Location);
            var word = GetWordAtOffset(offset);
            if (word == null) return;

            // 如果是关键字则显示提示
            if (_kwMap.TryGetValue(word, out var kw))
            {
                _hoverTooltip.Content = kw.Description;
                _hoverTooltip.PlacementTarget = textView;
                _hoverTooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
                _hoverTooltip.HorizontalOffset = posInView.X + 10;
                _hoverTooltip.VerticalOffset = posInView.Y + 20;
                _hoverTooltip.IsOpen = true;
            }
        }

        /// <summary>
        /// 切换编辑器主题<br/>
        /// 同步更新编辑器背景/前景色和补全窗口样式
        /// </summary>
        /// <param name="isDark">是否为暗色主题</param>
        private void SetTheme(bool isDark)
        {
            _isDark = isDark;
            _editor.Background = isDark ? _dark : _light;
            _editor.Foreground = isDark ? Brushes.LightGray : Brushes.Black;
            _editor.TextArea.Background = _editor.Background;
            _editor.TextArea.Foreground = _editor.Foreground;

            // 更新补全窗口主题（如果存在）
            if (_completionWindow != null) ApplyCompletionTheme(_completionWindow);
            _editor.TextArea.TextView.InvalidateVisual(); // 强制重绘
        }

        /// <summary>
        /// 应用主题到补全窗口<br/>
        /// 设置颜色和调整列表项模板
        /// </summary>
        private void ApplyCompletionTheme(CompletionWindow win)
        {
            win.CompletionList.ListBox.BorderThickness = new Thickness(0);

            // 使用简化的列表项模板（去除图标占位符）
            string templateXaml =
                @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                <TextBlock Text='{Binding Text}' Padding='4,2,4,2' VerticalAlignment='Center' />
              </DataTemplate>";
            win.CompletionList.ListBox.ItemTemplate = (DataTemplate)XamlReader.Parse(templateXaml);

            // 根据主题设置颜色
            if (_isDark)
            {
                win.Background = _dark;
                win.Foreground = Brushes.White;
                win.BorderBrush = Brushes.Gray;
                win.CompletionList.ListBox.Background = _dark;
                win.CompletionList.ListBox.Foreground = Brushes.White;
            }
            else
            {
                win.Background = _light;
                win.Foreground = Brushes.Black;
                win.BorderBrush = Brushes.LightGray;
                win.CompletionList.ListBox.Background = _light;
                win.CompletionList.ListBox.Foreground = Brushes.Black;
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 获取指定偏移位置的单词
        /// </summary>
        /// <param name="offset">文本偏移量</param>
        /// <returns>找到的单词或null</returns>
        private string? GetWordAtOffset(int offset)
        {
            if (offset < 0 || offset >= _editor.Document.TextLength) return null;
            var text = _editor.Document.Text;
            int start = offset, end = offset;
            // 向前查找单词起始
            while (start > 0 && IsWordChar(text[start - 1])) start--;
            // 向后查找单词结束
            while (end < text.Length && IsWordChar(text[end])) end++;
            return end > start ? text.Substring(start, end - start) : null;
        }

        /// <summary>
        /// 判断字符是否为单词字符<br/>
        /// 包括字母、数字和下划线
        /// </summary>
        private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
        #endregion

        #region IDisposable实现
        /// <summary>
        /// 释放资源方法<br/>
        /// 解绑事件处理器和清理资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 解绑所有事件处理器
            _editor.TextArea.TextEntered -= _textEnteredHandler;
            _editor.TextArea.TextView.MouseHover -= _textViewMouseHoverHandler;
            _editor.TextArea.TextView.MouseHoverStopped -= _textViewMouseHoverStoppedHandler;
            _editor.PreviewMouseDown -= _editorPreviewMouseDownHandler;

            // 移除高亮转换器
            _editor.TextArea.TextView.LineTransformers.Remove(_colorizer);
        }
        #endregion
    }
}
