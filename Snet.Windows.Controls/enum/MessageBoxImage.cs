using System.Text.Json.Serialization;

namespace Snet.Windows.Controls.@enum
{
    /// <summary>
    /// 消息框图标类型枚举<br/>
    /// 定义消息对话框中显示的图标类型，对应 Windows 系统图标<br/>
    /// 支持 JSON 序列化
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MessageBoxImage
    {
        /// <summary>
        /// 感叹号图标（黄色三角形背景 + 黑色感叹号）<br/>
        /// 用于警告提示
        /// </summary>
        Exclamation,

        /// <summary>
        /// 应用程序图标<br/>
        /// 显示当前应用程序的默认图标
        /// </summary>
        Application,

        /// <summary>
        /// 信息图标（蓝色圆形背景 + 白色 i 符号）<br/>
        /// 用于一般信息提示
        /// </summary>
        Asterisk,

        /// <summary>
        /// 错误图标（红色圆形背景 + 白色 X 符号）<br/>
        /// 用于错误提示
        /// </summary>
        Error,

        /// <summary>
        /// 停止图标（红色圆形背景 + 白色 X 符号）<br/>
        /// 与 Error 图标相同，用于严重错误提示
        /// </summary>
        Hand,

        /// <summary>
        /// 信息图标（蓝色圆形背景 + 白色 i 符号）<br/>
        /// 用于一般性信息通知
        /// </summary>
        Information,

        /// <summary>
        /// 问号图标（蓝色圆形背景 + 白色问号）<br/>
        /// 用于询问类提示
        /// </summary>
        Question,

        /// <summary>
        /// 盾牌图标<br/>
        /// 用于安全相关的提示
        /// </summary>
        Shield,

        /// <summary>
        /// 警告图标（黄色三角形背景 + 黑色感叹号）<br/>
        /// 与 Exclamation 相同，用于警告提示
        /// </summary>
        Warning,

        /// <summary>
        /// Windows 徽标图标<br/>
        /// 显示 Windows 系统徽标
        /// </summary>
        WinLogo
    }
}