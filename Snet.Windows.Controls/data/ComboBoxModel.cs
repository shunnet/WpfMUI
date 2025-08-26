using Snet.Windows.Core.mvvm;

namespace Snet.Windows.Controls.data
{
    /// <summary>
    /// 下拉框模型
    /// </summary>
    public class ComboBoxModel : BindNotify
    {
        /// <summary>
        /// 下拉框模型构造函数
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public ComboBoxModel(string key, object value)
        {
            this.Key = key;
            this.Value = value;
        }

        /// <summary>
        /// 键
        /// </summary>
        public string Key
        {
            get => GetProperty(() => Key);
            set => SetProperty(() => Key, value);
        }

        /// <summary>
        /// 值
        /// </summary>
        public object Value
        {
            get => GetProperty(() => Value);
            set => SetProperty(() => Value, value);
        }
    }
}