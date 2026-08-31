using System.Windows;

namespace Snet.Windows.Controls.edit
{
    /// <summary>
    /// 自带默认样式资源的文本编辑器控件。<br/>
    /// 继承 TextEditor 并在构造函数中将 edit 主题资源（TextEditor.xaml 模板、
    /// TextArea 模板、补全窗口、搜索面板、行号等）合并进自身资源字典，
    /// 同时用 BasedOn 让自身类型的隐式样式继承 TextEditor 的默认样式，
    /// 使 DefaultStyleKey 直接解析到模板，无需在应用级合并资源即可正常显示。
    /// </summary>
    public class TextEditorControl : TextEditor
    {
        /// <summary>
        /// edit 主题资源路径（含 TextEditor/TextArea 模板及补全、搜索等依赖样式）
        /// </summary>
        private const string EditThemeResourceUri = "pack://application:,,,/Snet.Windows.Controls;component/edit/Themes.xaml";

        // 主题资源与隐式样式在进程内只需解析一次：ResourceDictionary 被多个元素的
        // MergedDictionaries 引用是 WPF 的标准共享方式（只读使用），Style 同理。
        private static readonly ResourceDictionary _theme = new ResourceDictionary
        {
            Source = new Uri(EditThemeResourceUri, UriKind.Absolute)
        };

        private static readonly Style _selfStyle = _theme[typeof(TextEditor)] as Style;

        static TextEditorControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextEditorControl),
                new FrameworkPropertyMetadata(typeof(TextEditorControl)));
        }

        /// <summary>
        /// 构造函数：合并共享的 edit 主题资源，并应用基于 TextEditor 默认样式的隐式样式。
        /// </summary>
        public TextEditorControl()
        {
            Resources.MergedDictionaries.Add(_theme);

            // 自身类型的隐式样式：继承 TextEditor 的默认样式（含 ScrollViewer 模板）。
            // 样式查找需在合并字典之后，此时 TextEditor 的隐式样式已可用。
            if (_selfStyle != null)
            {
                Resources[typeof(TextEditorControl)] = _selfStyle;
            }
        }
    }
}
