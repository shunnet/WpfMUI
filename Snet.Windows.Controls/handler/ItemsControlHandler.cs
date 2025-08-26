using Snet.Windows.Controls.data;
using System.Collections.ObjectModel;

namespace Snet.Windows.Controls.handler
{
    public static class ItemsControlHandler
    {
        /// <summary>
        /// 获取选中的项
        /// </summary>
        /// <param name="items">集合</param>
        /// <returns>选中的项</returns>
        public static ItemsControlModel? GetCheckedItem(this ObservableCollection<ItemsControlModel> items)
        {
            return items.Where(c => c.IsChecked).FirstOrDefault();
        }

        /// <summary>
        /// 获取选中的项
        /// </summary>
        /// <param name="items">集合</param>
        /// <param name="item">项</param>
        /// <returns>选中的项</returns>
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
