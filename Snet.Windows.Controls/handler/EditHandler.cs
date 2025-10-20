using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using Snet.Windows.Controls.data;
using Snet.Windows.Core.handler;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace Snet.Windows.Controls.handler
{
    /// <summary>
    /// AvalonEdit编辑器的高性能扩展类<br/>
    /// 功能包括自动补全、语法高亮、主题切换、悬停提示、行号控制<br/>
    /// 极致优化版本，减少临时对象创建，缓存画刷和模板，提高性能<br/>
    /// </summary>
    public class EditHandler : IDisposable
    {
        private readonly TextEditor _editor;                       // 编辑器实例
        private readonly Dictionary<string, EditModel> _kwMap;    // 关键字字典（忽略大小写）
        private CompletionWindow? _completionWindow;              // 当前补全窗口
        private readonly ToolTip _hoverTooltip = new();           // 悬停提示控件
        private readonly KeywordColorizer _colorizer;             // 语法高亮逻辑
        private bool _isDark;                                     // 当前主题状态
        private bool _disposed;                                   // 是否已释放资源

        // 主题缓存画刷<br/>
        // 使用预定义画刷避免重复创建，提高性能<br/>
        private readonly Brush _dark;
        private readonly Brush _light;

        // 补全项高度估算（像素）<br/>
        // 用于计算补全窗口的最大高度<br/>
        public int ItemHeight { get; set; } = 30;

        // 补全窗口最大显示行数<br/>
        // 限制补全窗口大小，避免遮挡编辑器内容<br/>
        public int MaxCompletionRows;

        // 缓存事件处理器（避免重复绑定/解绑）<br/>
        // 使用字段缓存委托，提高事件处理性能<br/>
        private readonly TextCompositionEventHandler _textEnteredHandler;
        private readonly MouseEventHandler _textViewMouseHoverHandler;
        private readonly MouseEventHandler _textViewMouseHoverStoppedHandler;
        private readonly MouseButtonEventHandler _editorPreviewMouseDownHandler;
        private readonly KeyEventHandler _editorPreviewKeyDownHandler;

        // 补全模板缓存（避免每次 XAML 解析）<br/>
        // 静态字段确保模板只创建一次<br/>
        private static readonly DataTemplate CompletionItemTemplate;

        // 关键字画刷缓存<br/>
        // 按关键字名称缓存对应的颜色画刷<br/>
        private readonly Dictionary<string, Brush> _kwBrushCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 判断是否为单词分隔符<br/>
        /// 空格、换行、制表符等作为分隔符<br/>
        /// 统一在这里定义，避免重复<br/>
        /// </summary>
        private static bool IsWordSeparator(char c)
        {
            return char.IsWhiteSpace(c) ||
                   c == '.' || c == ',' || c == ';' || c == ':' ||
                   c == '!' || c == '?' || c == '(' || c == ')' ||
                   c == '[' || c == ']' || c == '{' || c == '}' ||
                   c == '<' || c == '>' || c == '"' || c == '\'';
        }

        /// <summary>
        /// 静态构造函数<br/>
        /// 初始化补全项模板，只创建一次，减少性能开销<br/>
        /// </summary>
        static EditHandler()
        {
            string itemTemplateXaml =
            @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                <Grid Margin='2'>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width='255'/>
                        <ColumnDefinition Width='*'/>
                    </Grid.ColumnDefinitions>
                    <!-- 左侧关键字 -->
                    <TextBlock Grid.Column='0' 
                               Text='{Binding Text}' 
                               FontWeight='Bold' 
                               VerticalAlignment='Center' 
                               Margin='0,0,10,0'
                               TextAlignment='Center'/>
                    <!-- 右侧描述 -->
                    <TextBlock Grid.Column='1' 
                               Text='{Binding Describe}' 
                               VerticalAlignment='Center'
                               TextTrimming='CharacterEllipsis'
                               Opacity='0.6'
                               TextAlignment='Center'/>
                </Grid>
              </DataTemplate>";

            CompletionItemTemplate = (DataTemplate)XamlReader.Parse(itemTemplateXaml);

        }

        /// <summary>
        /// 构造函数<br/>
        /// 初始化关键字、主题、事件和高亮器<br/>
        /// </summary>
        /// <param name="editor">文本编辑器实例</param>
        /// <param name="keywords">关键字集合</param>
        /// <param name="maxCompletionRows">最大补全行数</param>
        /// <param name="showLineNumbers">是否显示行号</param>
        /// <param name="color">主题颜色配置</param>
        public EditHandler(TextEditor editor, IEnumerable<EditModel> keywords, int maxCompletionRows = 8, bool showLineNumbers = true, (string dark, string light)? color = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            MaxCompletionRows = Math.Max(1, maxCompletionRows);

            // 初始化主题画刷<br/>
            _dark = color != null ? FreezeBrush(color.Value.dark, Brushes.DimGray) : Brushes.DimGray;
            _light = color != null ? FreezeBrush(color.Value.light, Brushes.WhiteSmoke) : Brushes.WhiteSmoke;

            _editor.ShowLineNumbers = showLineNumbers;

            // 初始化关键字字典<br/>
            _kwMap = new Dictionary<string, EditModel>(StringComparer.OrdinalIgnoreCase);
            SetKeywords(keywords);

            // 初始化语法高亮器<br/>
            _colorizer = new KeywordColorizer(_kwMap, _kwBrushCache, () => _editor.Foreground ?? Brushes.Black);
            _editor.TextArea.TextView.LineTransformers.Add(_colorizer);

            // 初始化事件处理器<br/>
            _textEnteredHandler = Editor_TextEntered;
            _textViewMouseHoverHandler = TextView_MouseHover;
            _textViewMouseHoverStoppedHandler = (s, e) => _hoverTooltip.IsOpen = false;
            _editorPreviewMouseDownHandler = (s, e) => _completionWindow?.Close();
            _editorPreviewKeyDownHandler = Editor_PreviewKeyDown;

            // 绑定事件<br/>
            _editor.TextArea.TextEntered += _textEnteredHandler;
            _editor.TextArea.TextView.MouseHover += _textViewMouseHoverHandler;
            _editor.TextArea.TextView.MouseHoverStopped += _textViewMouseHoverStoppedHandler;
            _editor.PreviewMouseDown += _editorPreviewMouseDownHandler;
            _editor.PreviewKeyDown += _editorPreviewKeyDownHandler;

            // 注册主题切换事件<br/>
            SkinHandler.OnSkinEventAsync -= SkinHandler_OnSkinEventAsync;
            SkinHandler.OnSkinEventAsync += SkinHandler_OnSkinEventAsync;

            // 默认应用浅色主题<br/>
            SetTheme(false);
        }

        /// <summary>
        /// 将颜色字符串转换为画刷并冻结<br/>
        /// </summary>
        private static Brush FreezeBrush(string colorString, Brush fallback)
        {
            try
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString)!);
                if (brush.CanFreeze) brush.Freeze();
                return brush;
            }
            catch
            {
                return fallback;
            }
        }

        #region 关键词管理
        /// <summary>
        /// 设置关键字集合<br/>
        /// </summary>
        public void SetKeywords(IEnumerable<EditModel> keywords)
        {
            if (keywords == null) return;

            bool firstInit = _kwMap.Count == 0;
            if (firstInit)
            {
                _kwMap.Clear();
                _kwBrushCache.Clear();
            }

            foreach (var k in keywords)
            {
                if (string.IsNullOrWhiteSpace(k?.Name))
                    continue;

                _kwMap[k.Name] = new EditModel
                {
                    Name = k.Name,
                    Description = k.Description,
                    Color = k.Color
                };

                if (!string.IsNullOrWhiteSpace(k.Color))
                {
                    try
                    {
                        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(k.Color)!);
                        if (brush.CanFreeze) brush.Freeze();
                        _kwBrushCache[k.Name] = brush;
                    }
                    catch
                    {
                        _kwBrushCache[k.Name] = Brushes.DodgerBlue;
                    }
                }
                else if (!_kwBrushCache.ContainsKey(k.Name))
                {
                    _kwBrushCache[k.Name] = Brushes.DodgerBlue;
                }
            }

            _editor.TextArea.TextView.InvalidateVisual();
        }
        #endregion

        #region 自动补全
        /// <summary>
        /// 键盘按下事件处理<br/>
        /// </summary>
        private void Editor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 回退键触发补全
            if (e.Key == Key.Back)
            {
                // 延迟触发，等待删除操作完成
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    string currentWord = GetWordBeforeCaret();

                    // 如果回退后没有单词内容或者是分隔符，关闭补全窗口
                    if (string.IsNullOrEmpty(currentWord) || IsWordSeparator(currentWord[0]))
                    {
                        _completionWindow?.Close();
                        _completionWindow = null;
                    }
                    else
                    {
                        ShowCompletion(false);
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            // ESC键关闭补全窗口
            else if (e.Key == Key.Escape)
            {
                _completionWindow?.Close();
            }
            // Ctrl+Space强制显示补全
            else if (e.Key == Key.J && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                ShowCompletion(true);
            }
        }

        /// <summary>
        /// 文本输入事件处理<br/>
        /// 简化版本：只处理非分隔符输入
        /// </summary>
        private void Editor_TextEntered(object? sender, TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.Text)) return;

            char ch = e.Text[0];

            // 只对非分隔符字符显示补全
            if (!IsWordSeparator(ch))
            {
                ShowCompletion(false);
            }
        }

        /// <summary>
        /// 获取光标前单词<br/>
        /// </summary>
        private string GetWordBeforeCaret()
        {
            if (_editor?.Document == null || _editor.Document.TextLength == 0)
                return string.Empty;

            int offset = Math.Min(_editor.CaretOffset, _editor.Document.TextLength);
            if (offset == 0) return string.Empty;

            var text = _editor.Document.Text;

            // 从光标位置向前查找单词边界
            int start = offset - 1;
            while (start >= 0 && !IsWordSeparator(text[start]))
            {
                start--;
            }
            start++; // 调整到单词起始位置

            // 提取单词
            if (start < offset)
            {
                string word = text.Substring(start, offset - start);
                return word;
            }

            return string.Empty;
        }

        /// <summary>
        /// 显示补全窗口<br/>
        /// </summary>
        private void ShowCompletion(bool showAll = false)
        {
            var doc = _editor.Document;
            if (doc == null) return;

            string currentWord = GetWordBeforeCaret();

            // 如果当前单词是分隔符，不显示补全
            if (!showAll && !string.IsNullOrEmpty(currentWord) && IsWordSeparator(currentWord[0]))
            {
                _completionWindow?.Close();
                _completionWindow = null;
                return;
            }

            var matches = new List<EditModel>();

            // 如果没有当前单词或者显示全部，则显示所有关键字
            if (showAll || string.IsNullOrEmpty(currentWord))
            {
                matches.AddRange(_kwMap.Values);
            }
            else
            {
                // 只匹配名称
                foreach (var kw in _kwMap.Values)
                {
                    // 只检查名称匹配（前缀、包含、完全匹配）
                    bool nameMatches = kw.Name.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase) ||
                                      kw.Name.Contains(currentWord, StringComparison.OrdinalIgnoreCase) ||
                                      kw.Name.Equals(currentWord, StringComparison.OrdinalIgnoreCase);

                    if (nameMatches)
                    {
                        matches.Add(kw);
                    }
                }
            }

            // 按匹配质量排序
            if (!string.IsNullOrEmpty(currentWord))
            {
                matches.Sort((a, b) =>
                {
                    int scoreA = GetMatchScore(a, currentWord);
                    int scoreB = GetMatchScore(b, currentWord);
                    return scoreB.CompareTo(scoreA);
                });
            }

            // 如果没有匹配项，关闭补全窗口
            if (matches.Count == 0)
            {
                _completionWindow?.Close();
                _completionWindow = null;
                return;
            }

            // 创建或更新补全窗口
            if (_completionWindow != null)
            {
                // 更新现有窗口的数据
                var completionList = _completionWindow.CompletionList;
                completionList.CompletionData.Clear();

                foreach (var kw in matches)
                    completionList.CompletionData.Add(new KeywordCompletionData(kw));

                // 重置选择
                completionList.SelectedItem = completionList.CompletionData.FirstOrDefault();
            }
            else
            {
                // 创建新窗口
                _completionWindow = new CompletionWindow(_editor.TextArea)
                {
                    MaxHeight = MaxCompletionRows * ItemHeight,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    BorderThickness = new Thickness(1),
                    SizeToContent = SizeToContent.WidthAndHeight
                };

                ApplyCompletionTheme(_completionWindow);

                foreach (var kw in matches)
                    _completionWindow.CompletionList.CompletionData.Add(new KeywordCompletionData(kw));

                _completionWindow.Closed += (_, _) =>
                {
                    _completionWindow = null;
                };

                _completionWindow.Show();
            }
        }

        /// <summary>
        /// 计算匹配分数<br/>
        /// 用于排序补全项
        /// </summary>
        private int GetMatchScore(EditModel model, string currentWord)
        {
            if (string.IsNullOrEmpty(currentWord)) return 0;

            int score = 0;

            // 精确匹配最高分
            if (model.Name.Equals(currentWord, StringComparison.OrdinalIgnoreCase))
                score += 100;

            // 前缀匹配次高分
            if (model.Name.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                score += 50;

            // 包含匹配
            if (model.Name.Contains(currentWord, StringComparison.OrdinalIgnoreCase))
                score += 25;

            return score;
        }

        /// <summary>
        /// 关键字补全数据实现<br/>
        /// </summary>
        private sealed class KeywordCompletionData : ICompletionData
        {
            private readonly EditModel _kw;

            public KeywordCompletionData(EditModel kw) => _kw = kw;

            public ImageSource? Image => null;
            public string Text => _kw.Name;
            public object Content => _kw.Name;
            public object Description => null; // 隐藏提示框的提示
            public string Describe => _kw.Description; // 模板右侧显示用
            public double Priority => 0;

            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                var doc = textArea.Document;
                int start = completionSegment.Offset;
                int end = completionSegment.EndOffset;

                // 扩展选区包含整个单词
                while (start > 0 && !EditHandler.IsWordSeparator(doc.GetCharAt(start - 1)))
                    start--;
                while (end < doc.TextLength && !EditHandler.IsWordSeparator(doc.GetCharAt(end)))
                    end++;

                int replaceLength = end - start;
                string currentText = doc.GetText(start, replaceLength);

                // 替换文本
                doc.Replace(start, replaceLength, Text);
                textArea.Caret.Offset = start + Text.Length;
            }
        }
        #endregion

        #region 语法高亮
        /// <summary>
        /// 关键字颜色高亮器<br/>
        /// </summary>
        private sealed class KeywordColorizer : DocumentColorizingTransformer
        {
            private readonly Dictionary<string, EditModel> _kwMap;
            private readonly Dictionary<string, Brush> _kwBrushCache;
            private readonly Func<Brush> _defaultBrushProvider;

            public KeywordColorizer(Dictionary<string, EditModel> kwMap, Dictionary<string, Brush> kwBrushCache, Func<Brush> defaultBrushProvider)
            {
                _kwMap = kwMap;
                _kwBrushCache = kwBrushCache;
                _defaultBrushProvider = defaultBrushProvider;
            }

            protected override void ColorizeLine(DocumentLine line)
            {
                if (line.IsDeleted) return;
                var doc = CurrentContext.Document;
                string text = doc.GetText(line);

                // 设置默认画刷
                Brush defaultBrush = _defaultBrushProvider() ?? Brushes.Black;
                ChangeLinePart(line.Offset, line.EndOffset, e => e.TextRunProperties.SetForegroundBrush(defaultBrush));

                if (_kwMap.Count == 0) return;

                // 对每个关键字进行匹配（支持中文）
                foreach (var keyword in _kwMap.Keys)
                {
                    if (string.IsNullOrEmpty(keyword)) continue;

                    int index = 0;
                    while (index < text.Length)
                    {
                        int foundIndex = text.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase);
                        if (foundIndex == -1) break;

                        // 检查是否是完整单词（前后是分隔符或边界）
                        bool isWordStart = foundIndex == 0 || EditHandler.IsWordSeparator(text[foundIndex - 1]);
                        bool isWordEnd = foundIndex + keyword.Length == text.Length ||
                                        EditHandler.IsWordSeparator(text[foundIndex + keyword.Length]);

                        if (isWordStart && isWordEnd && _kwBrushCache.TryGetValue(keyword, out var brush))
                        {
                            ChangeLinePart(
                                line.Offset + foundIndex,
                                line.Offset + foundIndex + keyword.Length,
                                e => e.TextRunProperties.SetForegroundBrush(brush));
                        }

                        index = foundIndex + keyword.Length;
                    }
                }
            }
        }
        #endregion

        #region 悬停提示 & 主题
        /// <summary>
        /// 文本视图鼠标悬停事件<br/>
        /// 在关键字上悬停时显示描述信息<br/>
        /// </summary>
        private void TextView_MouseHover(object? sender, MouseEventArgs e)
        {
            var textView = _editor.TextArea.TextView;
            var posInView = e.GetPosition(textView);
            var pos = textView.GetPosition(posInView + textView.ScrollOffset);
            if (pos == null) return;

            // 获取鼠标位置对应的文档偏移量<br/>
            int offset = _editor.Document.GetOffset(pos.Value.Location);
            var word = GetWordAtOffset(offset);
            if (word == null || !_kwMap.TryGetValue(word, out var kw)) return;

            // 设置并显示悬停提示<br/>
            _hoverTooltip.Content = kw.Description;
            _hoverTooltip.PlacementTarget = textView;
            _hoverTooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            _hoverTooltip.HorizontalOffset = posInView.X + 10;
            _hoverTooltip.VerticalOffset = posInView.Y + 20;
            _hoverTooltip.IsOpen = true;
        }

        /// <summary>
        /// 设置编辑器主题<br/>
        /// </summary>
        private void SetTheme(bool isDark)
        {
            _isDark = isDark;
            _editor.Background = isDark ? _dark : _light;
            _editor.Foreground = isDark ? Brushes.LightGray : Brushes.Black;
            _editor.TextArea.Background = _editor.Background;
            _editor.TextArea.Foreground = _editor.Foreground;

            if (_completionWindow != null) ApplyCompletionTheme(_completionWindow);
            _editor.TextArea.TextView.InvalidateVisual();
        }

        /// <summary>
        /// 应用补全窗口主题<br/>
        /// </summary>
        private void ApplyCompletionTheme(CompletionWindow win)
        {
            win.CompletionList.ListBox.BorderThickness = new Thickness(0);
            win.CompletionList.ListBox.ItemTemplate = CompletionItemTemplate;

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
        /// 获取指定偏移量处的关键字文本<br/>
        /// </summary>
        private string? GetWordAtOffset(int offset)
        {
            if (_editor?.Document == null || _kwMap.Count == 0)
                return null;

            string text = _editor.Document.Text;
            if (string.IsNullOrEmpty(text) || offset < 0 || offset > text.Length) return null;

            // 向左找到单词开始
            int start = offset;
            while (start > 0 && !IsWordSeparator(text[start - 1])) start--;

            // 向右找到单词结束（exclusive）
            int end = offset;
            while (end < text.Length && !IsWordSeparator(text[end])) end++;

            if (end > start)
            {
                string word = text.Substring(start, end - start);
                if (_kwMap.ContainsKey(word))
                    return word;
            }

            return null;
        }
        #endregion

        #region IDisposable
        /// <summary>
        /// 释放资源<br/>
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _editor.TextArea.TextEntered -= _textEnteredHandler;
            _editor.TextArea.TextView.MouseHover -= _textViewMouseHoverHandler;
            _editor.TextArea.TextView.MouseHoverStopped -= _textViewMouseHoverStoppedHandler;
            _editor.PreviewMouseDown -= _editorPreviewMouseDownHandler;
            _editor.PreviewKeyDown -= _editorPreviewKeyDownHandler;

            _editor.TextArea.TextView.LineTransformers.Remove(_colorizer);
            _completionWindow?.Close();
            SkinHandler.OnSkinEventAsync -= SkinHandler_OnSkinEventAsync;
        }
        #endregion

        /// <summary>
        /// 皮肤事件处理<br/>
        /// </summary>
        private Task SkinHandler_OnSkinEventAsync(object? sender, Snet.Windows.Core.data.EventSkinResult e)
        {
            switch (e.Skin.Value)
            {
                case Snet.Windows.Core.@enum.SkinType.Dark: SetTheme(true); break;
                case Snet.Windows.Core.@enum.SkinType.Light: SetTheme(false); break;
            }
            return Task.CompletedTask;
        }
    }
}