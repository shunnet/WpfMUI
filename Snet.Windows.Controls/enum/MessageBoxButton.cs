using System.Text.Json.Serialization;

namespace Snet.Windows.Controls.@enum
{
    /// <summary>
    /// 消息框按钮类型枚举<br/>
    /// 定义消息对话框中显示的按钮组合，支持 JSON 序列化
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MessageBoxButton
    {
        /// <summary>
        /// 仅显示“确认”按钮<br/>
        /// 用于简单的信息提示，用户只能点击确认
        /// </summary>
        OK = 0,

        /// <summary>
        /// 显示“确认”和“取消”按钮<br/>
        /// 用于需要用户确认或取消的操作场景
        /// </summary>
        OKCancel = 1,

        /// <summary>
        /// 仅显示“是”按钮<br/>
        /// 用于简单的确认提示，语义上表示同意
        /// </summary>
        Yes = 2,

        /// <summary>
        /// 显示“是”和“否”按钮<br/>
        /// 用于需要用户做出“是/否”选择的场景
        /// </summary>
        YesNo = 3
    }
}