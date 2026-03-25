namespace Snet.Windows.Controls.data
{
    /// <summary>
    /// AvalonEdit 编辑器关键字数据模型<br/>
    /// 用于定义代码编辑器中的关键字名称、描述信息和高亮颜色<br/>
    /// 支持自动补全和语法高亮功能
    /// </summary>
    public class EditModel
    {
        /// <summary>
        /// 关键字名称<br/>
        /// 用于自动补全匹配和语法高亮识别
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 关键字描述信息<br/>
        /// 显示在补全列表右侧和悬停提示中
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 关键字显示颜色<br/>
        /// 十六进制颜色字符串（如 "#FF0000"），用于语法高亮<br/>
        /// 为 null 或空时使用默认颜色 DodgerBlue
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// 重写 ToString 方法，返回关键字名称
        /// </summary>
        /// <returns>关键字名称</returns>
        public override string ToString() => Name;
    }
}
