namespace Snet.Core.mq
{
    /// <summary>
    /// 消息中间件数据
    /// </summary>
    public class MqData
    {
        /// <summary>
        /// 库文件统一存放文件夹，绝对路径
        /// </summary>
        public string? LibFolder { get; set; } = $"{AppContext.BaseDirectory}lib\\mq";

        /// <summary>
        /// 库配置文件夹，绝对路径
        /// </summary>
        public string? LibConfigFolder { get; set; } = $"{AppContext.BaseDirectory}config\\mq";

        /// <summary>
        /// 库配置唯一标识符键
        /// </summary>
        public string? LibConfigSNKey { get; set; } = "SN";

        /// <summary>
        /// 动态库监控格式
        /// </summary>
        public string? DllWatcherFormat { get; set; } = "Snet.*.dll";

        /// <summary>
        /// 配置监控格式
        /// </summary>
        public string? ConfigWatcherFormat { get; set; } = "*.Mq.Config.json";

        /// <summary>
        /// 配置文件名称的格式 * 与配置数据中的SN一致
        /// 库配置：命名空间 + 类名.SN.Config.json
        /// </summary>
        public string? ConfigFileNameFormat { get; set; } = "{0}.*.Mq.Config.json";

        /// <summary>
        /// 配置替换格式
        /// </summary>
        public string? ConfigReplaceFormat { get; set; } = ".Mq.Config.json";

        /// <summary>
        /// 接口名称
        /// </summary>
        public string? InterfaceFullName { get; set; } = "Snet.Model.interface.IMq";

        /// <summary>
        /// 自动打开，创建实例成功后
        /// </summary>
        public bool AutoOn { get; set; } = true;

        /// <summary>
        /// 任务处理数量
        /// </summary>
        public int TaskNumber { get; set; } = 5;
    }
}
