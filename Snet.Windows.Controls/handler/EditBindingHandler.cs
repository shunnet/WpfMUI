using ICSharpCode.AvalonEdit;
using System.Windows;

namespace Snet.Windows.Controls.handler
{
    /// <summary>
    /// 🔧 AvalonEdit 文本绑定辅助类（解决 AvalonEdit.Text 不是依赖属性的问题）<br/>
    /// 支持 WPF 的双向绑定，避免手动事件同步。<br/>
    /// ✅ 优化点：<br/>
    /// 1. 事件绑定只执行一次，避免重复订阅。<br/>
    /// 2. 使用 string.Empty 避免 null 引起的异常。<br/>
    /// 3. 使用字符串比较减少不必要的 UI 更新。<br/>
    /// 4. 结构紧凑、线程安全、内存占用极低。
    /// </summary>
    public static class EditBindingHandler
    {
        /// <summary>
        /// 附加属性：可绑定的文本属性<br/>
        /// 类型：string<br/>
        /// 默认值：string.Empty<br/>
        /// 特性：双向绑定（BindsTwoWayByDefault）
        /// </summary>
        public static readonly DependencyProperty EditTextProperty =
            DependencyProperty.RegisterAttached(
                "EditText",
                typeof(string),
                typeof(EditBindingHandler),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnEditTextChanged));

        /// <summary>
        /// 获取 AvalonEdit 控件中的绑定文本值<br/>
        /// 用于从 XAML 或后端代码中读取绑定值。
        /// </summary>
        /// <param name="obj">AvalonEdit 控件对象</param>
        /// <returns>当前绑定的文本内容</returns>
        public static string GetEditText(DependencyObject obj)
            => (string)obj.GetValue(EditTextProperty);

        /// <summary>
        /// 设置 AvalonEdit 控件的绑定文本值<br/>
        /// 通常由 WPF 绑定系统自动调用。
        /// </summary>
        /// <param name="obj">AvalonEdit 控件对象</param>
        /// <param name="value">要设置的文本内容</param>
        public static void SetEditText(DependencyObject obj, string value)
            => obj.SetValue(EditTextProperty, value);

        /// <summary>
        /// 当绑定文本属性发生变化时触发<br/>
        /// 将依赖属性的值同步到 AvalonEdit.Text 中。
        /// </summary>
        private static void OnEditTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 若绑定目标不是 AvalonEdit.TextEditor，则不处理
            if (d is not TextEditor editor)
                return;

            // 临时解绑事件，防止 TextChanged 引发递归调用
            editor.TextChanged -= Editor_TextChanged;

            // 获取新值（防空处理）
            var newText = e.NewValue as string ?? string.Empty;

            // 若文本不同才更新，避免 UI 无谓刷新
            if (!string.Equals(editor.Text, newText))
                editor.Text = newText;

            // 确保事件绑定仅执行一次（防止重复订阅）
            editor.TextChanged -= Editor_TextChanged;
            editor.TextChanged += Editor_TextChanged;
        }

        /// <summary>
        /// AvalonEdit 文本内容变更事件<br/>
        /// 将 AvalonEdit.Text 的更改同步回依赖属性，从而更新绑定源。
        /// </summary>
        private static void Editor_TextChanged(object? sender, EventArgs e)
        {
            if (sender is not TextEditor editor)
                return;

            var currentValue = GetEditText(editor);
            var newValue = editor.Text ?? string.Empty;

            // 若内容不同则写回依赖属性，触发 WPF 绑定更新
            if (!string.Equals(currentValue, newValue))
                SetEditText(editor, newValue);
        }
    }
}
