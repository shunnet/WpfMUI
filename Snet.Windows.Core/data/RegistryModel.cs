using Microsoft.Win32;

namespace Snet.Windows.Core.data
{
    /// <summary>
    /// 注册表模型
    /// </summary>
    public class RegistryModel
    {
        /// <summary>
        /// 无参构造函数
        /// </summary>
        public RegistryModel()
        { }

        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="path">注册表路径</param>
        /// <param name="name">注册表要修改的名称</param>
        /// <param name="value">注册表要修改的值</param>
        /// <param name="valueType">值类型</param>
        public RegistryModel(string path, string name, object value, RegistryValueKind valueType)
        {
            this.Path = path;
            this.Name = name;
            this.Value = value;
            this.ValueType = valueType;
        }

        /// <summary>
        /// 注册表要修改的名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 注册表路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 注册表要修改的值
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 值类型
        /// </summary>
        public RegistryValueKind ValueType { get; set; }
    }
}