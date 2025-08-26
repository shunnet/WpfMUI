using Snet.Model.attribute;
using Snet.Model.data;
using Snet.Unility;
using System.ComponentModel;

namespace Snet.Core.subscription
{
    /// <summary>
    /// 轮询数据
    /// </summary>
    public class SubscribeData
    {
        /// <summary>
        /// 基础数据
        /// </summary>
        public class Basics : SCData
        {
            /// <summary>
            /// 唯一标识符
            /// </summary>
            [Category("基础数据")]
            [Description("唯一标识符")]
            public string? SN { get; set; } = Guid.NewGuid().ToUpperNString();

            /// <summary>
            /// 点位，可为空，可后期赋值
            /// </summary>
            [Description("点位，可为空，可后期赋值")]
            public Address? Address { get; set; }

            /// <summary>
            /// 执行方法的委托，读取方法( Read )，每个通信库都应该存在<br/>
            /// Address：请求参数<br/>
            /// OperateResult：操作结果
            /// </summary>
            [Description("执行方法的委托，读取方法( Read )，每个通信库都应该存在")]
            public Func<Address, OperateResult>? Function { get; set; }
        }

        /// <summary>
        /// 订阅核心数据
        /// </summary>
        public class SCData
        {
            /// <summary>
            /// 处理间隔；<br/>
            /// 多少毫秒读取一次点位；<br/>
            /// 越小CPU占用越高，速度越快
            /// </summary>
            [Category("基础数据")]
            [Description("处理间隔")]
            [Unit("ms")]
            [Display(true, true, true, ParamModel.dataCate.unmber)]
            public int HandleInterval { get; set; } = 1000;

            /// <summary>
            /// 变化抛出<br/>
            /// true : 数据变化抛出，只抛出变化项<br/> 
            /// false : 则为实时数据<br/>
            /// 数据库查询，此项无效
            /// </summary>
            [Description("变化抛出")]
            [Display(true, true, true, ParamModel.dataCate.radio)]
            public bool ChangeOut { get; set; } = true;

            /// <summary>
            /// 变化项与未变项一同抛出<br/>
            /// 当 ChangeOut 为 true 此项生效<br/>
            /// true : 当节点数据变化，有一项数据未变化，把未变项与变化项一同抛出，确保此批数据的完整性<br/>
            /// false : 只抛出变化项<br/>
            /// 数据库订阅，此项无效
            /// </summary>
            [Description("变化项与未变项一同抛出")]
            [Display(true, true, true, ParamModel.dataCate.radio)]
            public bool AllOut { get; set; } = false;

            /// <summary>
            /// 任务数量；<br/>
            /// 一个任务属于一个队列
            /// </summary>
            [Description("任务数量")]
            [Display(true, true, true, ParamModel.dataCate.unmber)]
            public int TaskNumber { get; set; } = 5;
        }
    }
}