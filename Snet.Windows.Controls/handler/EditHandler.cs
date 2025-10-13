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

        // 补全模板缓存（避免每次 XAML 解析）<br/>
        // 静态字段确保模板只创建一次<br/>
        private static readonly DataTemplate CompletionItemTemplate;

        // 关键字画刷缓存<br/>
        // 按关键字名称缓存对应的颜色画刷<br/>
        private readonly Dictionary<string, Brush> _kwBrushCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 静态构造函数<br/>
        /// 初始化补全项模板，只创建一次，减少性能开销<br/>
        /// </summary>
        static EditHandler()
        {
            // 使用XAML字符串创建数据模板<br/>
            // 避免从文件加载的性能开销<br/>
            string itemTemplateXaml =
                @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                    <TextBlock Text='{Binding Text}' Padding='4,2,4,2' VerticalAlignment='Center'/>
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
            // 使用预定义颜色或默认颜色<br/>
            _dark = color != null ? FreezeBrush(color.Value.dark, Brushes.DimGray) : Brushes.DimGray;
            _light = color != null ? FreezeBrush(color.Value.light, Brushes.WhiteSmoke) : Brushes.WhiteSmoke;

            _editor.ShowLineNumbers = showLineNumbers;

            // 初始化关键字字典<br/>
            // 使用忽略大小写的比较器<br/>
            _kwMap = new Dictionary<string, EditModel>(StringComparer.OrdinalIgnoreCase);
            SetKeywords(keywords);

            // 初始化语法高亮器<br/>
            // 传入关键字字典和画刷缓存<br/>
            _colorizer = new KeywordColorizer(_kwMap, _kwBrushCache, () => _editor.Foreground ?? Brushes.Black);
            _editor.TextArea.TextView.LineTransformers.Add(_colorizer);

            // 初始化事件处理器<br/>
            // 缓存委托避免重复创建<br/>
            _textEnteredHandler = Editor_TextEntered;
            _textViewMouseHoverHandler = TextView_MouseHover;
            _textViewMouseHoverStoppedHandler = (s, e) => _hoverTooltip.IsOpen = false;
            _editorPreviewMouseDownHandler = (s, e) => _completionWindow?.Close();

            // 绑定事件<br/>
            // 使用缓存的事件处理器<br/>
            _editor.TextArea.TextEntered += _textEnteredHandler;
            _editor.TextArea.TextView.MouseHover += _textViewMouseHoverHandler;
            _editor.TextArea.TextView.MouseHoverStopped += _textViewMouseHoverStoppedHandler;
            _editor.PreviewMouseDown += _editorPreviewMouseDownHandler;

            // 注册主题切换事件<br/>
            SkinHandler.OnSkinEventAsync -= SkinHandler_OnSkinEventAsync;
            SkinHandler.OnSkinEventAsync += SkinHandler_OnSkinEventAsync;

            // 默认应用浅色主题<br/>
            SetTheme(false);
        }

        /// <summary>
        /// 将颜色字符串转换为画刷并冻结<br/>
        /// 避免重复创建画刷对象，提高性能<br/>
        /// </summary>
        /// <param name="colorString">颜色字符串</param>
        /// <param name="fallback">备用画刷</param>
        /// <returns>冻结的画刷对象</returns>
        private static Brush FreezeBrush(string colorString, Brush fallback)
        {
            try
            {
                // 从字符串创建颜色画刷<br/>
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString)!);
                // 冻结画刷提高性能<br/>
                if (brush.CanFreeze) brush.Freeze();
                return brush;
            }
            catch
            {
                // 颜色转换失败时返回备用画刷<br/>
                return fallback;
            }
        }

        #region 关键词管理
        /// <summary>
        /// 设置关键字集合<br/>
        /// 清空现有缓存，并缓存关键字颜色画刷<br/>
        /// </summary>
        /// <param name="keywords">关键字模型集合</param>
        public void SetKeywords(IEnumerable<EditModel> keywords)
        {
            // 清空现有关键字和画刷缓存<br/>
            _kwMap.Clear();
            _kwBrushCache.Clear();

            // 遍历关键字集合并添加到字典<br/>
            foreach (var k in keywords)
            {
                if (string.IsNullOrWhiteSpace(k?.Name)) continue;

                // 添加关键字到字典<br/>
                _kwMap[k.Name] = new EditModel
                {
                    Name = k.Name,
                    Description = k.Description,
                    Color = k.Color
                };

                // 缓存关键字颜色画刷<br/>
                if (!string.IsNullOrWhiteSpace(k.Color))
                {
                    try
                    {
                        // 创建并冻结颜色画刷<br/>
                        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(k.Color)!);
                        if (b.CanFreeze) b.Freeze();
                        _kwBrushCache[k.Name] = b;
                    }
                    catch
                    {
                        // 颜色转换失败时使用默认蓝色<br/>
                        _kwBrushCache[k.Name] = Brushes.DodgerBlue;
                    }
                }
            }

            // 强制刷新视图以应用新的关键字高亮<br/>
            _editor.TextArea.TextView.InvalidateVisual();
        }
        #endregion

        #region 自动补全
        /// <summary>
        /// 文本输入事件处理<br/>
        /// 监听用户输入，在适当的时候触发自动补全<br/>
        /// </summary>
        private void Editor_TextEntered(object? sender, TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.Text)) return;
            char ch = e.Text[0];

            // 字母数字和下划线触发补全<br/>
            if (char.IsLetterOrDigit(ch) || ch == '_')
                ShowCompletion(triggeredBySpace: false);
            // 空格键触发补全（如果当前没有补全窗口）<br/>
            else if (ch == ' ')
            {
                if (_completionWindow != null) _completionWindow.Close();
                else ShowCompletion(triggeredBySpace: true);
            }
        }

        /// <summary>
        /// 显示补全窗口<br/>
        /// 根据当前输入的前缀筛选匹配的关键字<br/>
        /// </summary>
        /// <param name="triggeredBySpace">是否由空格键触发</param>
        private void ShowCompletion(bool triggeredBySpace = false)
        {
            var doc = _editor.Document;
            if (doc == null) return;

            int caret = _editor.CaretOffset;
            if (caret < 0) return;

            // 获取光标前单词前缀<br/>
            int start = caret;
            while (start > 0 && IsWordChar(doc.GetCharAt(start - 1))) start--;
            string prefix = caret > start ? doc.GetText(start, caret - start) : string.Empty;

            // 筛选匹配关键字（优化：直接循环，避免LINQ开销）<br/>
            var matches = new List<EditModel>();
            foreach (var kw in _kwMap.Values)
            {
                if (triggeredBySpace || kw.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    matches.Add(kw);
            }

            // 没有匹配项时关闭补全窗口<br/>
            if (!matches.Any())
            {
                _completionWindow?.Close();
                _completionWindow = null;
                return;
            }

            // 已存在窗口则刷新数据<br/>
            if (_completionWindow != null)
            {
                var list = _completionWindow.CompletionList.CompletionData;
                list.Clear();
                foreach (var kw in matches) list.Add(new KeywordCompletionData(kw));
                return;
            }

            // 创建新的补全窗口<br/>
            _completionWindow = new CompletionWindow(_editor.TextArea)
            {
                MaxHeight = MaxCompletionRows * ItemHeight,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                BorderThickness = new Thickness(1),
            };

            // 应用主题到补全窗口<br/>
            ApplyCompletionTheme(_completionWindow);

            // 添加匹配的补全项<br/>
            foreach (var kw in matches)
                _completionWindow.CompletionList.CompletionData.Add(new KeywordCompletionData(kw));

            // 窗口关闭时清空引用<br/>
            _completionWindow.Closed += (_, _) => _completionWindow = null;
            _completionWindow.Show();
        }

        /// <summary>
        /// 关键字补全数据实现<br/>
        /// 封装关键字的补全显示和插入逻辑<br/>
        /// </summary>
        private sealed class KeywordCompletionData : ICompletionData
        {
            private readonly EditModel _kw;

            /// <summary>
            /// 构造函数<br/>
            /// 使用关键字模型初始化补全数据<br/>
            /// </summary>
            public KeywordCompletionData(EditModel kw) => _kw = kw;

            public ImageSource? Image => null;
            public string Text => _kw.Name;
            public object Content => _kw.Name;
            public object Description => _kw.Description;
            public double Priority => 0;

            /// <summary>
            /// 完成补全操作<br/>
            /// 将选中的关键字插入到文档中<br/>
            /// </summary>
            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                var doc = textArea.Document;
                int start = completionSegment.Offset;
                int end = completionSegment.EndOffset;

                // 扩展选区包含整个单词，避免部分替换<br/>
                while (start > 0 && IsWordChar(doc.GetCharAt(start - 1))) start--;
                while (end < doc.TextLength && IsWordChar(doc.GetCharAt(end))) end++;

                // 替换文本并设置光标位置<br/>
                doc.Replace(start, end - start, Text);
                textArea.Caret.Offset = start + Text.Length;
            }
        }
        #endregion

        #region 语法高亮
        /// <summary>
        /// 关键字颜色高亮器<br/>
        /// 负责文档中关键字的语法高亮显示<br/>
        /// </summary>
        private sealed class KeywordColorizer : DocumentColorizingTransformer
        {
            private readonly Dictionary<string, EditModel> _kwMap;
            private readonly Dictionary<string, Brush> _kwBrushCache;
            private readonly Func<Brush> _defaultBrushProvider;

            /// <summary>
            /// 构造函数<br/>
            /// 初始化关键字映射和画刷缓存<br/>
            /// </summary>
            public KeywordColorizer(Dictionary<string, EditModel> kwMap, Dictionary<string, Brush> kwBrushCache, Func<Brush> defaultBrushProvider)
            {
                _kwMap = kwMap;
                _kwBrushCache = kwBrushCache;
                _defaultBrushProvider = defaultBrushProvider;
            }

            /// <summary>
            /// 颜色化行内容<br/>
            /// 遍历行中的每个单词并应用对应的颜色<br/>
            /// </summary>
            protected override void ColorizeLine(DocumentLine line)
            {
                if (line.IsDeleted) return;

                var doc = CurrentContext.Document;
                string text = doc.GetText(line);

                // 设置整行默认颜色<br/>
                Brush defaultBrush = _defaultBrushProvider() ?? Brushes.Black;
                ChangeLinePart(line.Offset, line.EndOffset, e => e.TextRunProperties.SetForegroundBrush(defaultBrush));

                // 遍历文本查找关键字<br/>
                int i = 0;
                while (i < text.Length)
                {
                    if (IsWordChar(text[i]))
                    {
                        // 提取完整单词<br/>
                        int s = i;
                        while (i < text.Length && IsWordChar(text[i])) i++;
                        string token = text.Substring(s, i - s);

                        // 如果是关键字则应用高亮颜色<br/>
                        if (_kwMap.ContainsKey(token) && _kwBrushCache.TryGetValue(token, out var brush))
                        {
                            ChangeLinePart(line.Offset + s, line.Offset + i, e =>
                                e.TextRunProperties.SetForegroundBrush(brush));
                        }
                    }
                    else i++;
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
        /// 根据主题类型切换颜色方案<br/>
        /// </summary>
        /// <param name="isDark">是否为深色主题</param>
        private void SetTheme(bool isDark)
        {
            _isDark = isDark;
            _editor.Background = isDark ? _dark : _light;
            _editor.Foreground = isDark ? Brushes.LightGray : Brushes.Black;
            _editor.TextArea.Background = _editor.Background;
            _editor.TextArea.Foreground = _editor.Foreground;

            // 更新补全窗口主题<br/>
            if (_completionWindow != null) ApplyCompletionTheme(_completionWindow);

            // 刷新文本视图<br/>
            _editor.TextArea.TextView.InvalidateVisual();
        }

        /// <summary>
        /// 应用补全窗口主题<br/>
        /// 根据当前主题设置补全窗口的颜色<br/>
        /// </summary>
        /// <param name="win">补全窗口实例</param>
        private void ApplyCompletionTheme(CompletionWindow win)
        {
            win.CompletionList.ListBox.BorderThickness = new Thickness(0);
            win.CompletionList.ListBox.ItemTemplate = CompletionItemTemplate;

            if (_isDark)
            {
                // 深色主题配色<br/>
                win.Background = _dark;
                win.Foreground = Brushes.White;
                win.BorderBrush = Brushes.Gray;
                win.CompletionList.ListBox.Background = _dark;
                win.CompletionList.ListBox.Foreground = Brushes.White;
            }
            else
            {
                // 浅色主题配色<br/>
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
        /// 获取指定偏移量处的单词<br/>
        /// 向前和向后查找单词边界<br/>
        /// </summary>
        /// <param name="offset">文档偏移量</param>
        /// <returns>找到的单词或null</returns>
        private string? GetWordAtOffset(int offset)
        {
            if (offset < 0 || offset >= _editor.Document.TextLength) return null;
            var text = _editor.Document.Text;

            // 向前查找单词起始位置<br/>
            int start = offset;
            while (start > 0 && IsWordChar(text[start - 1])) start--;

            // 向后查找单词结束位置<br/>
            int end = offset;
            while (end < text.Length && IsWordChar(text[end])) end++;

            return end > start ? text.Substring(start, end - start) : null;
        }

        /// <summary>
        /// 判断字符是否为单词字符<br/>
        /// 字母、数字和下划线被视为单词字符<br/>
        /// </summary>
        /// <param name="c">要检查的字符</param>
        /// <returns>是否为单词字符</returns>
        private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
        #endregion

        #region IDisposable
        /// <summary>
        /// 释放资源<br/>
        /// 解除事件绑定并清理高亮器<br/>
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 解除事件绑定<br/>
            _editor.TextArea.TextEntered -= _textEnteredHandler;
            _editor.TextArea.TextView.MouseHover -= _textViewMouseHoverHandler;
            _editor.TextArea.TextView.MouseHoverStopped -= _textViewMouseHoverStoppedHandler;
            _editor.PreviewMouseDown -= _editorPreviewMouseDownHandler;

            // 移除语法高亮器<br/>
            _editor.TextArea.TextView.LineTransformers.Remove(_colorizer);
        }
        #endregion

        /// <summary>
        /// 皮肤事件处理<br/>
        /// 响应系统主题切换事件<br/>
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