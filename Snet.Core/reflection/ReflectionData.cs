using System.Reflection;

namespace Snet.Core.reflection
{
    /// <summary>
    /// 反射
    /// </summary>
    public class ReflectionData
    {
        /// <summary>
        /// 基础数据
        /// </summary>
        public class Basics
        {
            /// <summary>
            /// 可以同时处反射多个Dll到内存中
            /// </summary>
            public List<DllData> DllDatas { get; set; }
        }
        /// <summary>
        /// 动态库数据
        /// </summary>
        public class DllData
        {
            /// <summary>
            /// 动态链接库路径
            /// </summary>
            public string DllPath { get; set; }

            /// <summary>
            /// 绝对路径
            /// true:是绝对路径，反之false（探测程序集的基目录+DllPath）
            /// </summary>
            public bool IsAbsolutePath { get; set; } = false;

            /// <summary>
            /// 命名空间集合，一个DLL中会出现多个命名空间
            /// </summary>
            public List<NamespaceData> NamespaceDatas { get; set; }
        }
        /// <summary>
        /// 命名空间数据
        /// </summary>
        public class NamespaceData
        {
            /// <summary>
            /// 命名空间
            /// </summary>
            public string Namespace { get; set; }

            /// <summary>
            /// 在同一个命名空间存在多个类
            /// </summary>
            public List<ClassData> ClassDatas { get; set; }
        }
        /// <summary>
        /// 类数据
        /// </summary>
        public class ClassData
        {
            /// <summary>
            /// 多个实例的区分的唯一标识符
            /// </summary>
            public string SN { get; set; }

            /// <summary>
            /// 类名
            /// </summary>
            public string ClassName { get; set; }

            /// <summary>
            /// 构造函数参数，
            /// 创建实例的参数，也就是构造函数传入的参数，
            /// 包含要传递给构造函数的自变量的数组。 此自变量数组在数量、顺序和类型方面必须与要调用的构造函数的参数匹配。 如果需要无参数构造函数，则 args 必须是空数组或 null。
            /// </summary>
            public object[]? ConstructorParam { get; set; }

            /// <summary>
            /// 方法名 获取到多个方法，并存入内存
            /// 方法名，是否是异步方法
            /// </summary>
            public List<MethodData>? MethodDatas { get; set; } = null;

            /// <summary>
            /// 事件数据
            /// </summary>
            public List<EventData>? EventDatas { get; set; } = null;


        }
        /// <summary>
        /// 方法数据
        /// </summary>
        public class MethodData
        {
            /// <summary>
            /// 用于外部获取或调用使用的唯一标识符
            /// </summary>
            public string SN { get; set; }

            /// <summary>
            /// 是否执行，不判断执行结果状态
            /// </summary>
            public bool WhetherExecute { get; set; }

            /// <summary>
            /// 方法名称
            /// </summary>
            public string MethodName { get; set; }

            /// <summary>
            /// 在初始化时需要执行的方法参数
            /// </summary>
            public object[]? MethodParam { get; set; }
        }
        /// <summary>
        /// 事件数据
        /// </summary>
        public class EventData
        {
            /// <summary>
            /// 事件的唯一标识符
            /// </summary>
            public string SN { get; set; }

            /// <summary>
            /// 事件的名称
            /// </summary>
            public string EventName { get; set; }
        }

        /// <summary>
        /// 反射方法结果数据
        /// </summary>
        public class ReflectionMethodResult
        {
            /// <summary>
            /// 方法信息
            /// </summary>
            public MethodInfo Method { get; set; }

            /// <summary>
            /// 实例对象
            /// </summary>
            public object InstanceObject { get; set; }
        }

        /// <summary>
        /// 反射事件结果数据
        /// </summary>
        public class ReflectionEventResult
        {
            /// <summary>
            /// 事件信息
            /// </summary>
            public EventInfo Event { get; set; }

            /// <summary>
            /// 实例对象
            /// </summary>
            public object InstanceObject { get; set; }
        }
    }
}