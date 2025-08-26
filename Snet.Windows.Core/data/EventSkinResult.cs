using Snet.Unility;
using Snet.Windows.Core.@enum;
using System.Text.Json.Serialization;

namespace Snet.Windows.Core.data
{
    /// <summary>
    /// 事件皮肤结果
    /// </summary>
    public class EventSkinResult : EventArgsAsync
    {
        /// <summary>
        /// 事件语言结果；<br/>
        /// 无参构造函数
        /// </summary>
        public EventSkinResult() { }

        /// <summary>
        /// 事件语言结果；<br/>
        /// 有参构造函数
        /// </summary>
        /// <param name="result">事件信息结果</param>
        public EventSkinResult(EventSkinResult result)
        {
            this.Status = result.Status;
            this.Message = result.Message;
            this.Skin = result.Skin;
            this.Time = result.Time;
        }

        /// <summary>
        /// 事件语言结果；<br/>
        /// 有参构造函数
        /// </summary>
        /// <param name="status">状态</param>
        /// <param name="message">消息</param>
        /// <param name="skin">皮肤类型</param>
        public EventSkinResult(bool status, string message, SkinType? skin = null)
        {
            this.Status = status;
            this.Skin = skin;
            this.Message = message;
        }
        /// <summary>
        /// 状态
        /// </summary>
        public bool Status { get; set; }

        /// <summary>
        /// 信息
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 语言类型
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SkinType? Skin { get; set; }

        /// <summary>
        /// 时间
        /// </summary>
        public DateTime Time { get; set; } = DateTime.Now;

        /// <summary>
        /// 获取详情
        /// </summary>
        /// <param name="message">抛出描述信息</param>
        /// <param name="skin">皮肤类型</param>
        /// <returns>状态</returns>
        public bool GetDetails(out string? message, out SkinType? skin)
        {
            skin = Skin;
            message = Message;
            return Status;
        }

        /// <summary>
        /// 获取详情
        /// </summary>
        /// <param name="skin">皮肤类型</param>
        /// <returns>状态</returns>
        public bool GetDetails(out SkinType? skin)
        {
            skin = Skin;
            return Status;
        }

        /// <summary>
        /// 获取详情
        /// </summary>
        /// <param name="message">抛出描述信息</param>
        /// <returns>状态</returns>
        public bool GetDetails(out string? message)
        {
            message = Message;
            return Status;
        }

        /// <summary>
        /// 快速创建一个成功的结果
        /// </summary>
        /// <param name="successMessage">成功消息</param>
        /// <returns>结果对象</returns>
        public static EventSkinResult CreateSuccessResult(string successMessage)
        {
            return new(true, successMessage);
        }

        /// <summary>
        /// 快速创建一个失败的结果
        /// </summary>
        /// <param name="failureMessage">失败的消息</param>
        /// <returns>结果对象</returns>
        public static EventSkinResult CreateFailureResult(string failureMessage)
        {
            return new(false, failureMessage);
        }

        /// <summary>
        /// 快速创建一个成功的结果
        /// </summary>
        /// <param name="successMessage">成功消息</param>
        /// <param name="skin">皮肤类型</param>
        /// <returns>结果对象</returns>
        public static EventSkinResult CreateSuccessResult(string successMessage, SkinType? skin)
        {
            return new(true, successMessage, skin);
        }

        /// <summary>
        /// 快速创建一个失败的结果
        /// </summary>
        /// <param name="failureMessage">失败的消息</param>
        /// <param name="skin">皮肤类型</param>
        /// <returns>结果对象</returns>
        public static EventSkinResult CreateFailureResult(string failureMessage, SkinType? skin)
        {
            return new(false, failureMessage, skin);
        }

        /// <summary>
        /// 重写ToString；<br/>
        /// 响应 json 字符串
        /// </summary>
        /// <returns>json 字符串</returns>
        public override string ToString()
        {
            return this.ToJson(true) ?? string.Empty;
        }
    }
}
