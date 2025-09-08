namespace Snet.Windows.Controls.data
{
    /// <summary>
    /// 编辑器的关键字模型
    /// </summary>
    public class EditModel
    {
        /// <summary>关键字名称</summary>
        public string Name { get; set; } = "";

        /// <summary>关键字描述信息</summary>
        public string Description { get; set; } = "";

        /// <summary>关键字显示颜色（由外部传入的十六进制颜色）</summary>
        public string Color { get; set; }

        /// <summary>重写ToString方法，返回关键字名称</summary>
        public override string ToString() => Name;
    }
}
