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

        static TextEditorControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextEditorControl),
                new FrameworkPropertyMetadata(typeof(TextEditorControl)));
        }

        /// <summary>
        /// 构造函数：合并 edit 主题资源，并构建基于 TextEditor 默认样式的隐式样式。
        /// </summary>
        public TextEditorControl()
        {
            var editResources = new ResourceDictionary
            {
                Source = new Uri(EditThemeResourceUri, UriKind.Absolute)
            };
            Resources.MergedDictionaries.Add(editResources);

            // 自身类型的隐式样式：继承 TextEditor 的默认样式（含 ScrollViewer 模板）。
            // 样式查找需在合并字典之后，此时 TextEditor 的隐式样式已可用。
            if (editResources[typeof(TextEditor)] is Style baseStyle)
            {
                var selfStyle = new Style(typeof(TextEditorControl), baseStyle);
                Resources[typeof(TextEditorControl)] = selfStyle;
            }
        }
    }
}
