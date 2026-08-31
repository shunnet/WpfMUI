namespace Snet.Windows.Controls.drag
{
    /// <summary>
    /// 画布拖拽布局（JSON 序列化模型）。<br/>
    /// 可直接通过 System.Text.Json 保存/加载整个画布布局，
    /// 配合 DragControlsAnimate 实现布局的持久化与还原。
    /// </summary>
    public class DragControlsLayout
    {
        /// <summary>布局格式版本号（后续扩展字段时用于兼容判断）</summary>
        public int Version { get; set; } = 1;

        /// <summary>布局中的控件集合</summary>
        public List<DragControlsLayoutItem> Items { get; set; } = [];
    }

    /// <summary>
    /// 画布拖拽布局中的单个控件描述：类型 + 画布坐标 + 尺寸 + 内容。
    /// </summary>
    public class DragControlsLayoutItem
    {
        /// <summary>控件类型（如 Button / CheckBox / TextBox / ComboBox / Rectangle）</summary>
        public string Type { get; set; } = "";

        /// <summary>画布 X 坐标（对应 Canvas.Left）</summary>
        public double X { get; set; }

        /// <summary>画布 Y 坐标（对应 Canvas.Top）</summary>
        public double Y { get; set; }

        /// <summary>控件宽度</summary>
        public double Width { get; set; }

        /// <summary>控件高度</summary>
        public double Height { get; set; }

        /// <summary>旋转角度（度，绕中心、顺时针；0 表示不旋转）</summary>
        public double Angle { get; set; }

        /// <summary>文本内容（按钮文字 / 勾选框标签 / 文本框内容 / 下拉框选中项等）</summary>
        public string? Text { get; set; }

        /// <summary>勾选状态（CheckBox；其他控件为 null）</summary>
        public bool? IsChecked { get; set; }

        /// <summary>填充颜色（Rectangle，#RRGGBB 或 #AARRGGBB）</summary>
        public string? Fill { get; set; }

        /// <summary>控件源名称（左侧注册的源标识；布局加载时按此名称实例化源控件）</summary>
        public string? SourceName { get; set; }

        /// <summary>标识 SN（序列号）</summary>
        public string? SN { get; set; }

        /// <summary>扩展数据（任意文本，如 JSON 串）</summary>
        public string? ExtensionData { get; set; }

        /// <summary>扩展字段（按键值对保存应用自定义信息，类型不匹配时安全忽略）</summary>
        public Dictionary<string, string>? Extra { get; set; }
    }
}
