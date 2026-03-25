using Snet.Windows.Controls.data;
using System.Collections.ObjectModel;

namespace Snet.Windows.Controls.handler
{
    /// <summary>
    /// ItemsControl 集合操作扩展类<br/>
    /// 提供 ObservableCollection&lt;ItemsControlModel&gt; 的便捷查询和设置方法
    /// </summary>
    public static class ItemsControlHandler
    {
        /// <summary>
        /// 获取集合中第一个被选中的项<br/>
        /// 遍历集合查找 IsChecked 为 true 的第一个元素
        /// </summary>
        /// <param name="items">ItemsControlModel 可观察集合</param>
        /// <returns>第一个选中的项，若无选中项则返回 null</returns>
        public static ItemsControlModel? GetCheckedItem(this ObservableCollection<ItemsControlModel> items)
        {
            return items.FirstOrDefault(c => c.IsChecked);
        }

        /// <summary>
        /// 根据 Key 匹配并设置指定项为选中状态<br/>
        /// 在集合中查找与传入项具有相同 Key 的元素，将其 IsChecked 设为 true
        /// </summary>
        /// <param name="items">ItemsControlModel 可观察集合</param>
        /// <param name="item">目标项（通过 Key 匹配）</param>
        /// <returns>原始集合引用，支持链式调用</returns>
        public static ObservableCollection<ItemsControlModel> SetCheckedItem(this ObservableCollection<ItemsControlModel> items, ItemsControlModel item)
        {
            var result = items.FirstOrDefault(c => c.Key == item.Key);
            if (result != null)
            {
                result.IsChecked = true;
            }
            return items;
        }
    }
}
