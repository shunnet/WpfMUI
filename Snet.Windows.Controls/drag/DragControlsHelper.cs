using System.Windows;
using System.Windows.Documents;

namespace Snet.Windows.Controls.drag
{
    /// <summary>
    /// 控件拖动实现类
    /// </summary>
    public class DragControlsHelper
    {
        /// <summary>
        /// 数据字典
        /// UIElement：要拖动的控件
        /// AdornerLayer：装饰器
        /// DragControlsBase：装饰器实现类
        /// </summary>
        readonly Dictionary<UIElement, Tuple<AdornerLayer, DragControlsBase>> DictionaryDataList = new Dictionary<UIElement, Tuple<AdornerLayer, DragControlsBase>>();
        /// <summary>
        /// 添加项
        /// </summary>
        /// <param name="Controls">控件</param>
        /// <param name="LlayoutContainer">窗体的布局容器：意思就是这个控件是被谁包这的就传它，我一般传窗体对象，窗体包着所有的控件，小范围拖动，自行建布局容器包着要拖动的控件 </param>
        /// <param name="DragSize">拖拽大小</param>
        /// <param name="Move">移动</param>
        public string Insert(UIElement Controls, FrameworkElement LlayoutContainer, bool Move, bool DragSize)
        {
            string Message = "功能都已启用";
            if (!DictionaryDataList.ContainsKey(Controls))
            {
                DragControlsBase dragControlsBase = new DragControlsBase(Controls, LlayoutContainer, Move, DragSize);
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(Controls);  //获取装饰器，控件还没在界面呈现，所以获取不到装饰器，就无法实现拖拽大小的功能
                adornerLayer?.Add(dragControlsBase);
                Tuple<AdornerLayer, DragControlsBase> tuple = new Tuple<AdornerLayer, DragControlsBase>(adornerLayer, dragControlsBase);
                DictionaryDataList.Add(Controls, tuple);
                if (DragSize && adornerLayer == null)
                {
                    Message = "拖拽大小失败，原因是控件还没在界面呈现，获取不到装饰器，无法实现拖拽大小的功能";
                }
            }
            else
            {
                Message = "已经存在此控件，如果界面中已移除此控件，请调用[ Remove ]方法，在操作";
            }
            return Message;
        }
        /// <summary>
        /// 移除拖动
        /// </summary>
        /// <param name="Controls">控件</param>
        public void Remove(UIElement Controls)
        {
            if (DictionaryDataList.ContainsKey(Controls))
            {
                DictionaryDataList[Controls].Item1.Remove(DictionaryDataList[Controls].Item2);  //移除此项属性
                Delete(Controls);   //在集合移除此项
            }
        }
        /// <summary>
        /// 删除此项
        /// </summary>
        /// <param name="Controls">控件</param>
        private void Delete(UIElement Controls)
        {
            DictionaryDataList.Remove(Controls);  //直接移除
        }
    }
}
