using Newtonsoft.Json.Linq;
using Snet.Core.extend;
using Snet.Model.data;
using Snet.Unility;
using System.Collections.Concurrent;
using System.Reflection;
using static Snet.Core.reflection.ReflectionData;

namespace Snet.Core.reflection
{
    /// <summary>
    /// 反射操作；<br/>
    /// 你得先生成配置；<br/>
    /// 在初始化；<br/>
    /// 在调用方法或注册事件
    /// </summary>
    public class ReflectionOperate : CoreUnify<ReflectionOperate, Basics>, IDisposable
    {
        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="basics">基础数据</param>
        public ReflectionOperate(Basics basics) : base(basics) { }

        /// <summary>
        /// 反射状态
        /// </summary>
        private bool ReflectionState;

        /// <summary>
        /// 获取反射状态
        /// </summary>
        /// <returns></returns>
        public bool GetStatus()
        {
            return ReflectionState;
        }

        /// <summary>
        /// 方法的IOC容器<br/>
        /// key 标识符，$"{ClassData.SN}{MethodData.SN}"
        /// value (个个Dll反射的结果)
        /// </summary>
        private ConcurrentDictionary<string, ReflectionMethodResult> MethodIocContainer = new ConcurrentDictionary<string, ReflectionMethodResult>();

        /// <summary>
        /// 事件的IOC容器<br/>
        /// key 标识符，$"{ClassData.SN}{MethodData.SN}"
        /// value (个个Dll反射的结果)
        /// </summary>
        private ConcurrentDictionary<string, ReflectionEventResult> EventIocContainer = new ConcurrentDictionary<string, ReflectionEventResult>();

        /// <summary>
        /// 对象容器
        /// </summary>
        private ConcurrentDictionary<string, object> ObjectIocContainer = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public OperateResult Init()
        {
            BegOperate();
            try
            {
                //得到DLL数据
                foreach (var DllData in basics.DllDatas)
                {
                    Assembly? assembly = null;
                    //是不是绝对路径
                    if (DllData.IsAbsolutePath)
                    {
                        if (File.Exists(DllData.DllPath))
                        {
                            assembly = Assembly.LoadFile(DllData.DllPath);
                        }
                        else
                        {
                            return EndOperate(false, $"{DllData.DllPath} 文件不存在");
                        }
                    }
                    else
                    {
                        //文件路径拼接
                        string path = Path.Combine(AppContext.BaseDirectory, DllData.DllPath);
                        //文件判断
                        if (File.Exists(path))
                        {
                            assembly = Assembly.LoadFile(path);
                        }
                        else
                        {
                            return EndOperate(false, $"{path} 文件不存在");
                        }
                    }

                    foreach (var NamespaceData in DllData.NamespaceDatas)
                    {
                        //Dll下需要反射的类数据
                        foreach (var ClassData in NamespaceData.ClassDatas)
                        {
                            //命名空间加类名
                            string NamespaceAndClassName = $"{NamespaceData.Namespace}.{ClassData.ClassName}";

                            //获取类型
                            Type? NamespaceAndClassNameType = assembly.GetType(NamespaceAndClassName, false, true);

                            //类型不能为空
                            if (NamespaceAndClassNameType != null)
                            {
                                //创建该对象的实例
                                object? instanceObject = CreateInstance(NamespaceAndClassNameType, ClassData.ConstructorParam);
                                if (instanceObject != null)
                                {
                                    if (!ObjectIocContainer.ContainsKey(NamespaceAndClassNameType.FullName))
                                    {
                                        ObjectIocContainer.TryAdd(NamespaceAndClassNameType.FullName, instanceObject);
                                    }
                                }
                                if (ClassData.MethodDatas != null)
                                {
                                    //遍历方法
                                    foreach (var MethodData in ClassData.MethodDatas)
                                    {
                                        //获取方法
                                        MethodInfo? methodInfo = NamespaceAndClassNameType.GetMethod(MethodData.MethodName);
                                        if (methodInfo != null)
                                        {
                                            //结果数据
                                            ReflectionMethodResult reflectResult = new ReflectionMethodResult()
                                            {
                                                InstanceObject = ObjectIocContainer[NamespaceAndClassNameType.FullName],
                                                Method = methodInfo
                                            };
                                            //存在直接更新，不存在新增
                                            MethodIocContainer.AddOrUpdate($"{ClassData.SN}{MethodData.SN}", reflectResult, (K, V) => reflectResult);

                                            //执行此方法
                                            if (MethodData.WhetherExecute)
                                            {
                                                //执行此函数，不管返回结果
                                                methodInfo?.Invoke(ObjectIocContainer[NamespaceAndClassNameType.FullName], ParamTypeConvert(MethodData.MethodParam, methodInfo));
                                            }
                                        }
                                        else
                                        {
                                            return EndOperate(false, $"{DllData.DllPath} -> {NamespaceData.Namespace}.{ClassData.ClassName} -> {MethodData.MethodName} 方法不存在");
                                        }
                                    }
                                }
                                if (ClassData.EventDatas != null)
                                {
                                    //遍历事件
                                    foreach (var EventData in ClassData.EventDatas)
                                    {
                                        //获取方法
                                        EventInfo? eventInfo = NamespaceAndClassNameType.GetEvent(EventData.EventName);
                                        if (eventInfo != null)
                                        {
                                            //结果数据
                                            ReflectionEventResult reflectResult = new ReflectionEventResult()
                                            {
                                                InstanceObject = ObjectIocContainer[NamespaceAndClassNameType.FullName],
                                                Event = eventInfo
                                            };
                                            //存在直接更新，不存在新增
                                            EventIocContainer.AddOrUpdate($"{ClassData.SN}{EventData.SN}", reflectResult, (K, V) => reflectResult);
                                        }
                                        else
                                        {
                                            return EndOperate(false, $"{DllData.DllPath} -> {NamespaceData.Namespace}.{ClassData.ClassName} -> {EventData.EventName} 事件不存在");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                return EndOperate(false, $"请检查 {DllData.DllPath} -> {NamespaceData.Namespace}.{ClassData.ClassName} 类名的准确性");
                            }
                        }
                    }
                }
                ReflectionState = true;
                return EndOperate(true, "反射初始化成功", MethodIocContainer);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="NamespaceAndClassNameType">类型</param>
        /// <param name="ConstructorParam">构造函数入参</param>
        /// <returns></returns>
        private object? CreateInstance(Type? NamespaceAndClassNameType, object[]? ConstructorParam)
        {
            //转换后的参数
            object[]? Param = null;

            if (ConstructorParam != null)
            {
                //获取构造函数信息
                ConstructorInfo? constructorInfo = NamespaceAndClassNameType?.GetConstructors().FirstOrDefault();
                //数据类型转换
                Param = ParamTypeConvert(ConstructorParam, constructorInfo);
            }
            //创建实例
            return Activator.CreateInstance(NamespaceAndClassNameType, Param);
        }

        /// <summary>
        /// 参数类型转换
        /// </summary>
        /// <returns></returns>
        private object[]? ParamTypeConvert(object[] Data, object Info)
        {
            if (Data != null)
            {
                ParameterInfo[]? ParamArray = null;
                List<object> param = new List<object>();
                if (Info.GetType().ToString().Contains("MethodInfo"))
                {
                    ParamArray = (Info as MethodInfo).GetParameters();
                    if (ParamArray != null)
                    {
                        for (int i = 0; i < ParamArray.Count(); i++)
                        {
                            if (string.IsNullOrEmpty(ParamArray[i].ParameterType.FullName))
                            {
                                param.Add(Data[i]);
                            }
                            else
                            {
                                param.Add(Convert.ChangeType(Data[i], ParamArray[i].ParameterType));
                            }
                        }
                    }
                }
                if (Info.GetType().ToString().Contains("ConstructorInfo"))
                {
                    ParamArray = (Info as ConstructorInfo).GetParameters();
                    if (ParamArray != null)
                    {
                        for (int i = 0; i < ParamArray.Count(); i++)
                        {
                            object? model = Activator.CreateInstance(ParamArray[i].ParameterType);
                            PropertyInfo[] properties = ParamArray[i].ParameterType.GetProperties();
                            foreach (var propertie in properties)
                            {
                                JObject? JsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(Data[i].ToJson());
                                propertie.SetValue(model, Convert.ChangeType(JsonObject[propertie.Name], propertie.PropertyType));
                            }
                            if (model != null)
                            {
                                param.Add(model);
                            }
                        }
                    }
                }
                if (param.Count > 0)
                {
                    return param.ToArray();
                }
            }
            return null;
        }

        /// <summary>
        /// 执行方法
        /// </summary>
        /// <param name="SN">方法的唯一标识符</param>
        /// <param name="MethodParam">方法参数</param>
        /// <returns></returns>
        public object? ExecuteMethod(string SN, object[]? MethodParam = null)
        {
            ReflectionMethodResult? reflectionMethodResult = GetMethod(SN);

            if (reflectionMethodResult != null)
            {
                //转换后的参数
                object[]? Param = ParamTypeConvert(MethodParam, reflectionMethodResult.Method);

                //返回执行结果
                return reflectionMethodResult.Method?.Invoke(reflectionMethodResult.InstanceObject, Param);
            }
            else
            {
                return EndOperate(false, "执行失败，未找到反射的数据", methodName: BegOperate());
            }
        }

        /// <summary>
        /// 获取所有方法
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<string, ReflectionMethodResult>? GetMethod()
        {
            if (MethodIocContainer != null && MethodIocContainer.Count > 0)
            {
                return MethodIocContainer;
            }
            return null;
        }

        /// <summary>
        /// 获取指定SN的方法
        /// </summary>
        /// <returns></returns>
        public ReflectionMethodResult? GetMethod(string SN)
        {
            if (MethodIocContainer != null && MethodIocContainer.Count > 0)
            {
                return MethodIocContainer[SN];
            }
            return null;
        }

        /// <summary>
        /// 注册事件
        /// </summary>
        /// <param name="SN">唯一标识符</param>
        /// <param name="Register">是否注册 ，true 注册 false 移除</param>
        /// <param name="P1">一个参数的事件动作</param>
        /// <param name="P2">二个参数的事件动作</param>
        /// <param name="P3">三个参数的事件动作</param>
        /// <param name="P4">四个参数的事件动作</param>
        /// <param name="P5">五个参数的事件动作</param>
        /// <param name="P6">六个参数的事件动作</param>
        /// <returns>统一返回</returns>
        public OperateResult RegisterEvent(string SN, bool Register,
            Action<object>? P1 = null,
            Action<object, object>? P2 = null,
            Action<object, object, object>? P3 = null,
            Action<object, object, object, object>? P4 = null,
            Action<object, object, object, object, object>? P5 = null,
            Action<object, object, object, object, object, object>? P6 = null)
        {
            BegOperate();
            ReflectionEventResult? reflectionEventResult = GetEvent(SN);
            if (reflectionEventResult != null)
            {
                if (reflectionEventResult.Event.EventHandlerType != null)
                {
                    Delegate? @delegate = null;
                    if (P1 != null)
                    {
                        @delegate = Delegate.CreateDelegate(reflectionEventResult.Event.EventHandlerType, P1.Target, P1.Method);
                    }
                    if (P2 != null)
                    {
                        @delegate = Delegate.CreateDelegate(reflectionEventResult.Event.EventHandlerType, P2.Target, P2.Method);
                    }
                    if (P3 != null)
                    {
                        @delegate = Delegate.CreateDelegate(reflectionEventResult.Event.EventHandlerType, P3.Target, P3.Method);
                    }
                    if (P4 != null)
                    {
                        @delegate = Delegate.CreateDelegate(reflectionEventResult.Event.EventHandlerType, P4.Target, P4.Method);
                    }
                    if (P5 != null)
                    {
                        @delegate = Delegate.CreateDelegate(reflectionEventResult.Event.EventHandlerType, P5.Target, P5.Method);
                    }
                    if (P6 != null)
                    {
                        @delegate = Delegate.CreateDelegate(reflectionEventResult.Event.EventHandlerType, P6.Target, P6.Method);
                    }
                    if (Register)
                    {
                        reflectionEventResult.Event.AddEventHandler(reflectionEventResult.InstanceObject, @delegate);
                        return EndOperate(true, "事件注册成功");
                    }
                    else
                    {
                        reflectionEventResult.Event.RemoveEventHandler(reflectionEventResult.InstanceObject, @delegate);
                        return EndOperate(true, "事件移除成功");
                    }
                }
                else
                {
                    return EndOperate(false, "事件操作失败，未找到反射的数据类型");
                }
            }
            else
            {
                return EndOperate(false, "事件操作失败，未找到反射的数据");
            }
        }

        /// <summary>
        /// 获取反射的实例
        /// </summary>
        /// <param name="SN"></param>
        /// <returns></returns>
        public object? ReflectionInstance(string SN)
        {
            if (ObjectIocContainer.Count > 0)
            {
                return ObjectIocContainer[SN];
            }
            return null;
        }

        /// <summary>
        /// 获取所有事件
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<string, ReflectionEventResult>? GetEvent()
        {
            if (EventIocContainer != null && EventIocContainer.Count > 0)
            {
                return EventIocContainer;
            }
            return null;
        }

        /// <summary>
        /// 获取指定SN的事件
        /// </summary>
        /// <returns></returns>
        public ReflectionEventResult? GetEvent(string SN)
        {
            if (EventIocContainer != null && EventIocContainer.Count > 0)
            {
                return EventIocContainer[SN];
            }
            return null;
        }
        /// <inheritdoc/>
        public override void Dispose()
        {
            ReflectionState = false;
            ObjectIocContainer.Clear();
            MethodIocContainer.Clear();
            EventIocContainer.Clear();
            base.Dispose();
        }

        /// <inheritdoc/>
        public override async Task DisposeAsync()
        {
            Dispose();
            await base.DisposeAsync();
        }
    }
}