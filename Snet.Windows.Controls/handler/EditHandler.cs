using Snet.Windows.Controls.data;
using Snet.Windows.Controls.edit;
using Snet.Windows.Controls.edit.CodeCompletion;
using Snet.Windows.Controls.edit.Document;
using Snet.Windows.Controls.edit.Editing;
using Snet.Windows.Controls.edit.Rendering;
using Snet.Windows.Core.@enum;
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
            SetTheme(SkinHandler.GetSkin());
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

            // 如果已有关键字（非首次初始化），先清空旧数据再重新填充
            if (_kwMap.Count > 0)
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
        /// 只取光标所在行文本，避免每次按键复制整篇文档
        /// </summary>
        private string GetWordBeforeCaret()
        {
            if (_editor?.Document == null || _editor.Document.TextLength == 0)
                return string.Empty;

            int offset = Math.Min(_editor.CaretOffset, _editor.Document.TextLength);
            if (offset == 0) return string.Empty;

            // 只取光标所在行文本，避免复制整篇文档
            var line = _editor.Document.GetLineByOffset(offset);
            string text = _editor.Document.GetText(line);

            // 光标在该行内的位置
            int local = offset - line.Offset;

            // 从光标位置向前查找单词边界
            int start = local - 1;
            while (start >= 0 && !IsWordSeparator(text[start]))
            {
                start--;
            }
            start++; // 调整到单词起始位置

            // 提取单词
            if (start < local)
            {
                return text.Substring(start, local - start);
            }

            return string.Empty;
        }

        // 补全候选缓存（用于判断候选是否变化，避免每次按键都 Clear+重建）
        private string[]? _lastCompletionKeys;
        private string? _lastCompletionWord;

        /// <summary>
        /// 判断两次候选列表是否相同（按名称逐项比较，忽略大小写）
        /// </summary>
        private static bool SameKeys(string[]? a, string[]? b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (!string.Equals(a[i], b[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 显示补全窗口<br/>
        /// 优化：前缀快速过滤候选、候选超限截断、预计算匹配分数、候选未变化时跳过重建
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

            // 候选数量上限，避免候选过多导致排序和重建开销
            const int MaxCandidates = 50;

            var matches = new List<EditModel>();

            // 如果没有当前单词或者显示全部，则显示所有关键字（超限截断）
            if (showAll || string.IsNullOrEmpty(currentWord))
            {
                matches.AddRange(_kwMap.Values);
                if (matches.Count > MaxCandidates)
                    matches = matches.GetRange(0, MaxCandidates);
            }
            else
            {
                // 快路径：先做 StartsWith 前缀过滤（忽略大小写），命中才加入候选
                var prefixMatched = new HashSet<EditModel>();
                foreach (var kw in _kwMap.Values)
                {
                    if (kw.Name.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(kw);
                        prefixMatched.Add(kw);
                        if (matches.Count >= MaxCandidates) break;
                    }
                }

                // 前缀匹配不足时，再补充包含匹配（保持原有"前缀或包含"的匹配语义）
                if (matches.Count < MaxCandidates)
                {
                    foreach (var kw in _kwMap.Values)
                    {
                        if (prefixMatched.Contains(kw)) continue;
                        if (kw.Name.Contains(currentWord, StringComparison.OrdinalIgnoreCase))
                        {
                            matches.Add(kw);
                            if (matches.Count >= MaxCandidates) break;
                        }
                    }
                }
            }

            // 按匹配质量排序（预先计算分数存入列表，避免比较器重复调用 GetMatchScore）
            if (!string.IsNullOrEmpty(currentWord))
            {
                var scored = new List<(EditModel model, int score)>(matches.Count);
                foreach (var m in matches)
                    scored.Add((m, GetMatchScore(m, currentWord)));
                scored.Sort((x, y) => y.score.CompareTo(x.score));

                matches.Clear();
                foreach (var s in scored)
                    matches.Add(s.model);
            }

            // 如果没有匹配项，关闭补全窗口
            if (matches.Count == 0)
            {
                _completionWindow?.Close();
                _completionWindow = null;
                _lastCompletionKeys = null;
                _lastCompletionWord = null;
                return;
            }

            // 创建或更新补全窗口
            if (_completionWindow != null)
            {
                // 候选内容未变化时跳过 Clear+重建，减少每按键的分配
                var keys = new string[matches.Count];
                for (int i = 0; i < matches.Count; i++) keys[i] = matches[i].Name;

                bool sameCandidates = string.Equals(_lastCompletionWord, currentWord, StringComparison.OrdinalIgnoreCase) &&
                                      SameKeys(_lastCompletionKeys, keys);

                if (!sameCandidates)
                {
                    var completionList = _completionWindow.CompletionList;
                    completionList.CompletionData.Clear();

                    foreach (var kw in matches)
                        completionList.CompletionData.Add(new KeywordCompletionData(kw));

                    _lastCompletionKeys = keys;
                    _lastCompletionWord = currentWord;
                }

                // 重置选择
                _completionWindow.CompletionList.SelectedItem = _completionWindow.CompletionList.CompletionData.FirstOrDefault();
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

                foreach (var kw in matches)
                    _completionWindow.CompletionList.CompletionData.Add(new KeywordCompletionData(kw));

                var keys = new string[matches.Count];
                for (int i = 0; i < matches.Count; i++) keys[i] = matches[i].Name;
                _lastCompletionKeys = keys;
                _lastCompletionWord = currentWord;

                _completionWindow.Closed += (_, _) =>
                {
                    _completionWindow = null;
                    _lastCompletionKeys = null;
                    _lastCompletionWord = null;
                };

                _completionWindow.Show();

                // 主题必须在此应用：Show() 触发 ApplyTemplate 后 CompletionList.ListBox 才非空；
                // 若在 Show() 之前应用会因 CompletionList.ListBox 为 null 抛出 NullReferenceException
                ApplyCompletionTheme(_completionWindow);
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

            /// <summary>
            /// 含特殊字符的关键字（如 "[ Info ]"），必须整串匹配<br/>
            /// 按长度降序排列，优先匹配更长的短语（避免 "[ Info]" 抢先匹配 "[ Info ]"）
            /// </summary>
            private readonly string[] _phraseKeywords;
            private readonly Dictionary<string, Brush> _phraseBrushCache;

            /// <summary>
            /// 命中区间复用列表（避免每行分配），按行内偏移收集后排序统一着色
            /// </summary>
            private readonly List<(int start, int end, Brush brush)> _spans = new();

            public KeywordColorizer(Dictionary<string, EditModel> kwMap, Dictionary<string, Brush> kwBrushCache, Func<Brush> defaultBrushProvider)
            {
                _kwMap = kwMap;
                _kwBrushCache = kwBrushCache;
                _defaultBrushProvider = defaultBrushProvider;

                // 分离"整串短语"关键字：含空格/方括号等非标识符字符的（如 "[ Info ]"、"[ Error ]"），
                // 单遍标识符扫描永远无法命中它们（"[ Info ]" 会被切分成 "Info"），必须按子串整串查找
                var phrases = new List<string>();
                var phraseBrushes = new Dictionary<string, Brush>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in _kwMap)
                {
                    if (IsPureIdentifier(kv.Key)) continue;
                    phrases.Add(kv.Key);
                    if (kwBrushCache.TryGetValue(kv.Key, out var brush))
                        phraseBrushes[kv.Key] = brush;
                }
                phrases.Sort((a, b) => b.Length.CompareTo(a.Length));
                _phraseKeywords = phrases.ToArray();
                _phraseBrushCache = phraseBrushes;
            }

            /// <summary>
            /// 判断字符串是否全部由标识符字符组成<br/>
            /// （字母/数字/下划线，含中文等 Unicode 字母）
            /// </summary>
            private static bool IsPureIdentifier(string s)
            {
                if (string.IsNullOrEmpty(s)) return false;
                foreach (char c in s)
                {
                    if (!(char.IsLetterOrDigit(c) || c == '_'))
                        return false;
                }
                return true;
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

                _spans.Clear();

                // 1. 单遍扫描：逐字符识别标识符（字母/数字/下划线，支持中文），命中关键字才着色<br/>
                // 替代"每行文本 × 每关键字 IndexOf"嵌套循环，避免重复扫描
                int length = text.Length;
                int i = 0;
                while (i < length)
                {
                    // 跳过非标识符字符
                    while (i < length && !IsIdentifierChar(text[i])) i++;
                    if (i >= length) break;

                    int start = i;
                    while (i < length && IsIdentifierChar(text[i])) i++;
                    int end = i; // 单词结束位置（不包含）

                    // 查关键字字典（忽略大小写），命中才着色
                    string word = text.Substring(start, end - start);
                    if (_kwMap.ContainsKey(word) && _kwBrushCache.TryGetValue(word, out var brush))
                    {
                        _spans.Add((start, end, brush));
                    }
                }

                // 2. 短语关键字整串匹配：处理 "[ Info ]" 等含特殊字符的行首标签。
                //    短语数量少（通常十几个），每行 IndexOf 开销可忽略
                if (_phraseKeywords.Length > 0)
                {
                    foreach (var phrase in _phraseKeywords)
                    {
                        if (!_phraseBrushCache.TryGetValue(phrase, out var phraseBrush)) continue;
                        int pos = 0;
                        while ((pos = text.IndexOf(phrase, pos, StringComparison.Ordinal)) >= 0)
                        {
                            _spans.Add((pos, pos + phrase.Length, phraseBrush));
                            pos += phrase.Length;
                        }
                    }
                }

                // 3. 按偏移排序，跳过重叠区间后统一着色（ChangeLinePart 要求偏移递增且不重叠）
                if (_spans.Count > 0)
                {
                    _spans.Sort((a, b) => a.start != b.start ? a.start.CompareTo(b.start) : b.end.CompareTo(a.end));
                    int lastEnd = -1;
                    foreach (var (s, e, brush) in _spans)
                    {
                        if (s < lastEnd || s >= e) continue;
                        ChangeLinePart(
                            line.Offset + s,
                            line.Offset + e,
                            o => o.TextRunProperties.SetForegroundBrush(brush));
                        lastEnd = e;
                    }
                }
            }

            /// <summary>
            /// 判断是否为标识符字符（字母、数字、下划线，含中文等 Unicode 字母）
            /// </summary>
            private static bool IsIdentifierChar(char c)
            {
                return char.IsLetterOrDigit(c) || c == '_';
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
        public void SetTheme(SkinType isDark)
        {
            bool status = isDark == SkinType.Dark;
            _isDark = status;
            _editor.Background = status ? _dark : _light;
            _editor.Foreground = status ? Brushes.LightGray : Brushes.Black;
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
            // CompletionList.ListBox 是模板子元素：窗口 Show() 之后才非空；
            // 此处做空保护，避免任何时序问题（如窗口尚未应用模板）导致 NullReferenceException
            win?.CompletionList?.ListBox?.SetValue(Control.BorderThicknessProperty, new Thickness(0));
            if (win?.CompletionList?.ListBox is ListBox listBox)
            {
                listBox.ItemTemplate = CompletionItemTemplate;

                if (_isDark)
                {
                    listBox.Background = _dark;
                    listBox.Foreground = Brushes.White;
                }
                else
                {
                    listBox.Background = _light;
                    listBox.Foreground = Brushes.Black;
                }
            }

            if (win == null) return;

            if (_isDark)
            {
                win.Background = _dark;
                win.Foreground = Brushes.White;
                win.BorderBrush = Brushes.Gray;
            }
            else
            {
                win.Background = _light;
                win.Foreground = Brushes.Black;
                win.BorderBrush = Brushes.LightGray;
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 获取指定偏移量处的关键字文本<br/>
        /// 只取光标所在行文本，避免悬停时复制整篇文档
        /// </summary>
        private string? GetWordAtOffset(int offset)
        {
            if (_editor?.Document == null || _kwMap.Count == 0)
                return null;

            if (offset < 0 || offset > _editor.Document.TextLength) return null;

            // 只取光标所在行文本，避免复制整篇文档
            var line = _editor.Document.GetLineByOffset(offset);
            string text = _editor.Document.GetText(line);

            // 光标在该行内的位置
            int local = offset - line.Offset;

            // 向左找到单词开始
            int start = local;
            while (start > 0 && !IsWordSeparator(text[start - 1])) start--;

            // 向右找到单词结束（exclusive）
            int end = local;
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
            SetTheme(e.Skin.Value);
            return Task.CompletedTask;
        }
    }
}