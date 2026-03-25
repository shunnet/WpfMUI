using System.Windows;
using System.Windows.Documents;

namespace Snet.Windows.Controls.drag
{
    /// <summary>
    /// 控件拖动实现类。<br/>
    /// 管理控件的装饰器层（AdornerLayer）和拖拽基类（DragControlsBase），<br/>
    /// 支持添加和移除控件的拖动与拖拽大小功能。
    /// </summary>
    public class DragControlsHelper
    {
        /// <summary>
        /// 数据字典，存储控件与其装饰器层及拖拽基类的映射关系。<br/>
        /// Key: 要拖动的控件<br/>
        /// Value: (装饰器层, 拖拽控件基类实例)
        /// </summary>
        private readonly Dictionary<UIElement, (AdornerLayer? Layer, DragControlsBase Base)> DictionaryDataList = [];

        /// <summary>
        /// 添加拖动与拖拽大小功能。<br/>
        /// 创建装饰器并将其添加到控件的装饰器层中。<br/>
        /// 如果控件未在界面呈现，装饰器层可能为 null，拖拽大小功能将不可用。
        /// </summary>
        /// <param name="Controls">要拖动的控件</param>
        /// <param name="LlayoutContainer">窗体的布局容器：即该控件所在的父容器</param>
        /// <param name="Move">是否启用移动功能</param>
        /// <param name="DragSize">是否启用拖拽大小功能</param>
        /// <returns>操作结果消息字符串</returns>
        public string Insert(UIElement Controls, FrameworkElement LlayoutContainer, bool Move, bool DragSize)
        {
            string Message = "功能都已启用";
            if (!DictionaryDataList.ContainsKey(Controls))
            {
                var dragControlsBase = new DragControlsBase(Controls, LlayoutContainer, Move, DragSize);
                var adornerLayer = AdornerLayer.GetAdornerLayer(Controls);
                adornerLayer?.Add(dragControlsBase);
                DictionaryDataList.Add(Controls, (adornerLayer, dragControlsBase));
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
        /// 移除控件的拖动功能。<br/>
        /// 从装饰器层中移除装饰器，并从数据字典中删除记录。<br/>
        /// 当 AdornerLayer 为空时（控件未呈现时添加的），跳过装饰器移除操作。
        /// </summary>
        /// <param name="Controls">要移除拖动功能的控件</param>
        public void Remove(UIElement Controls)
        {
            if (DictionaryDataList.TryGetValue(Controls, out var entry))
            {
                // AdornerLayer 可能为 null（控件在界面未呈现时添加的情况）
                entry.Layer?.Remove(entry.Base);
                DictionaryDataList.Remove(Controls);
            }
        }
    }
}
