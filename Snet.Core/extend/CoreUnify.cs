using Snet.Core.handler;
using Snet.Log;
using Snet.Model.attribute;
using Snet.Model.data;
using Snet.Model.@enum;
using Snet.Model.@interface;
using Snet.Unility;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Snet.Model.data.ParamModel;

namespace Snet.Core.extend
{
    /// <summary>
    /// 核心统一类，统一实现单例、事件、统一出入参、函数过程时间记录、详细日志输出、创建实例、获取参数、日志管理、多语言；<br/>
    /// O、D都约定为类；
    /// </summary>
    /// <typeparam name="O">操作类</typeparam>
    /// <typeparam name="D">基础数据类，构造参数类</typeparam>
    public class CoreUnify<O, D> : IGetParam, ICreateInstance, IEvent, ILog, ILanguage, IDisposable
        where O : class
        where D : class
    {
        /// <summary>
        /// 无惨构造函数
        /// </summary>
        protected CoreUnify()
        {
            RegisterLanguageEvent();
        }

        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="param">参数</param>
        protected CoreUnify(D param)
        {
            basics = param;
            RegisterLanguageEvent();
        }

        /// <summary>
        /// 释放所有资源<br/>
        /// 注意:<br/>
        /// 外部重写此方法并且释放完自身资源后<br/>
        /// 请在执行如下方法<br/>
        /// base.Dispose();
        /// </summary>
        public virtual void Dispose()
        {
            objList.Remove(basics, out _);
            GC.Collect();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 异步释放所有资源<br/>
        /// 注意:<br/>
        /// 外部重写此方法并且释放完自身资源后<br/>
        /// 请在执行如下方法<br/>
        /// base.DisposeAsync();
        /// </summary>
        public virtual Task DisposeAsync()
        {
            Dispose();
            return Task.CompletedTask;
        }

        #region 基础属性
        /// <summary>
        /// 锁
        /// </summary>
        private static readonly object objLock = new object();

        /// <summary>
        /// 最大实例数量
        /// </summary>
        protected static readonly int maxInstanceCount = 200;

        /// <summary>
        /// 实例数超过限制提示
        /// </summary>
        private static readonly string exceedMaxInstanceCountTips = string.Format(LanguageHandler.GetLanguageValue("exceedMaxInstanceCountTips"), maxInstanceCount);

        /// <summary>
        /// 创建实例失败
        /// </summary>
        private static readonly string createInstanceErrorTips = string.Format(LanguageHandler.GetLanguageValue("createInstanceErrorTips"), maxInstanceCount);

        /// <summary>
        /// 单例集合；<br/>
        /// Key:基础数据；<br/>
        /// Value:操作对象；
        /// </summary>
        protected static readonly ConcurrentDictionary<D, O> objList = new ConcurrentDictionary<D, O>();

        /// <summary>
        /// 基础参数
        /// </summary>
        protected D basics { get; set; }

        /// <summary>
        /// 全局标识，用于统一返回
        /// </summary>
        protected string TAG => typeof(O).Name;

        #region 获取参数使用

        /// <summary>
        /// 中文名称；<br/>
        /// 虚属性，可选重写；<br/>
        /// 如果无法使用中文就使用英文即可；
        /// </summary>
        protected virtual string CN { get; }
        /// <summary>
        /// 中文描述；<br/>
        /// 虚属性，可选重写
        /// </summary>
        protected virtual string CD { get; }

        /// <summary>
        /// 额外添加属性值，获取参数时使用；<br/>
        /// 虚属性，可选重写
        /// </summary>
        protected virtual List<propertie> AP { get; }

        #endregion 获取参数使用

        #endregion 基础属性

        #region 事件

        #region 数据
        /// <inheritdoc/>
        public event EventHandler<EventDataResult> OnDataEvent;
        /// <inheritdoc/>
        public event EventHandlerAsync<EventDataResult> OnDataEventAsync
        {
            add => OnDataEventWrapperAsync.AddHandler(value);
            remove => OnDataEventWrapperAsync.RemoveHandler(value);
        }
        /// <summary>
        /// 数据传递包装器异步
        /// </summary>
        private EventingWrapperAsync<EventDataResult> OnDataEventWrapperAsync;
        /// <summary>
        /// 数据源传递
        /// </summary>
        /// <param name="sender">自身对象</param>
        /// <param name="e">事件结果</param>
        protected void OnDataEventHandler(object? sender, EventDataResult e)
        {
            OnDataEvent?.Invoke(sender, e);
            OnDataEventWrapperAsync.InvokeAsync(sender, e);
        }

        /// <summary>
        /// 异步数据源传递
        /// </summary>
        /// <param name="sender">自身对象</param>
        /// <param name="e">事件结果</param>
        protected Task OnDataEventHandlerAsync(object? sender, EventDataResult e)
        {
            OnDataEvent?.Invoke(sender, e);
            OnDataEventWrapperAsync.InvokeAsync(sender, e);
            return Task.CompletedTask;
        }
        #endregion

        #region 信息
        /// <inheritdoc/>
        public event EventHandler<EventInfoResult> OnInfoEvent;
        /// <inheritdoc/>
        public event EventHandlerAsync<EventInfoResult> OnInfoEventAsync
        {
            add => OnInfoEventWrapperAsync.AddHandler(value);
            remove => OnInfoEventWrapperAsync.RemoveHandler(value);
        }
        /// <summary>
        /// 信息传递包装器异步
        /// </summary>
        private EventingWrapperAsync<EventInfoResult> OnInfoEventWrapperAsync;
        /// <summary>
        /// 消息源传递
        /// </summary>
        /// <param name="sender">自身对象</param>
        /// <param name="e">事件结果</param>
        protected void OnInfoEventHandler(object? sender, EventInfoResult e)
        {
            OnInfoEvent?.Invoke(sender, e);
            OnInfoEventWrapperAsync.InvokeAsync(sender, e);
        }

        /// <summary>
        /// 异步消息源传递
        /// </summary>
        /// <param name="sender">自身对象</param>
        /// <param name="e">事件结果</param>
        protected Task OnInfoEventHandlerAsync(object? sender, EventInfoResult e)
        {
            OnInfoEvent?.Invoke(sender, e);
            OnInfoEventWrapperAsync.InvokeAsync(sender, e);
            return Task.CompletedTask;
        }
        #endregion

        #region 语言
        /// <inheritdoc/>
        public event EventHandler<EventLanguageResult> OnLanguageEvent;
        /// <inheritdoc/>
        public event EventHandlerAsync<EventLanguageResult> OnLanguageEventAsync
        {
            add => OnLanguageEventWrapperAsync.AddHandler(value);
            remove => OnLanguageEventWrapperAsync.RemoveHandler(value);
        }
        /// <summary>
        /// 信息传递包装器异步
        /// </summary>
        private EventingWrapperAsync<EventLanguageResult> OnLanguageEventWrapperAsync;
        /// <summary>
        /// 消息源传递
        /// </summary>
        /// <param name="sender">自身对象</param>
        /// <param name="e">事件结果</param>
        protected void OnLanguageEventHandler(object? sender, EventLanguageResult e)
        {
            OnLanguageEvent?.Invoke(sender, e);
            OnLanguageEventWrapperAsync.InvokeAsync(sender, e);
        }

        /// <summary>
        /// 消息源传递
        /// </summary>
        /// <param name="sender">自身对象</param>
        /// <param name="e">事件结果</param>
        protected Task OnLanguageEventHandlerAsync(object? sender, EventLanguageResult e)
        {
            OnLanguageEvent?.Invoke(sender, e);
            OnLanguageEventWrapperAsync.InvokeAsync(sender, e);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 注册语言事件
        /// </summary>
        private void RegisterLanguageEvent()
        {
            EventHandler<EventLanguageResult> languageEvent = (sender, e) =>
            {
                OnLanguageEvent?.Invoke(sender, e);
            };
            EventHandlerAsync<EventLanguageResult> languageEventAsync = (sender, e) => Task.Run(() =>
            {
                OnLanguageEventWrapperAsync.InvokeAsync(sender, e);
            });
            //防止重复注册
            LanguageHandler.OnLanguageEvent -= languageEvent;
            LanguageHandler.OnLanguageEvent += languageEvent;
            LanguageHandler.OnLanguageEventAsync -= languageEventAsync;
            LanguageHandler.OnLanguageEventAsync += languageEventAsync;
        }

        #endregion

        #endregion

        #region 基础方法
        /// <summary>
        /// 控制台输出标题<br/>
        /// 虚方法，可选重写
        /// </summary>
        /// <returns>控制台输出标题</returns>
        public virtual string Title()
        {
            return @"https://Shunnet.top";
        }

        /// <summary>
        /// 全局配置文件默认路径<br/>
        /// 虚方法，可选重写
        /// </summary>
        /// <returns>返回定义好的路径，供全局使用</returns>
        public virtual string GlobalConfigDefaultPath()
        {
            return AppContext.BaseDirectory + "config";
        }

        /// <summary>
        /// 创建单例
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns>对象</returns>
        private static O? CreateInstance(object param)
        {
            ConstructorInfo? cInfo = typeof(O).GetConstructor([typeof(D)]);
            if (cInfo != null)
            {
                return cInfo.Invoke([param]) as O;
            }
            return null;
        }

        /// <summary>
        /// 单例模式
        /// </summary>
        /// <param name="param">
        /// 基础参数<br/>
        /// 不传入则使用默认，直接创建默认对象<br/>
        /// 如果这个单例还想使用时，请使用InstanceBasics方法设置基础数据
        /// </param>
        /// <returns>对象</returns>
        public static O Instance(D? param = null)
        {
            //判断单例对象数
            if (objList.Count >= maxInstanceCount)
            {
                //超过限定值肯定是有问题直接提示出来
                throw new Exception(exceedMaxInstanceCountTips);
            }

            //如果为空则创建一个新的对象
            if (param == null)
            {
                param = Activator.CreateInstance(typeof(D)) as D;
                if (param == null)
                {
                    throw new Exception($"{typeof(D).Name} {LanguageHandler.GetLanguageValue("实例无法创建")}");
                }
            }

            lock (objLock)
            {
                //检索对象集合，查找是否有相同参数的对象
                O? exp = objList.FirstOrDefault(c => c.Key.Comparer(param).result).Value;
                if (exp == null)
                {

                    //说明不存在此对象，直接创建
                    O? o = CreateInstance(param);
                    //判断实例是否创建成功
                    if (o == null)
                    {
                        //创建实例失败
                        throw new Exception(createInstanceErrorTips);
                    }
                    else
                    {
                        //添加到集合
                        objList.TryAdd(param, o);
                        //返回对象
                        return o;
                    }
                }
                return exp;
            }
        }


        /// <summary>
        /// 设置单例基础数据（用于未传参创建后，绑定实际参数）
        /// </summary>
        /// <param name="param">新的基础参数</param>
        /// <returns>
        /// true：成功更新绑定参数并重新注册到单例容器中；<br/>
        /// false：更新失败（如对象未在容器中）
        /// </returns>
        public bool InstanceBasics(D param)
        {
            lock (objLock)
            {
                // 如果当前对象未绑定旧 key（即 basics），无法操作
                if (basics == null) return false;

                // 尝试从容器中移除旧 key 对应的实例
                if (!objList.TryRemove(basics, out O? instance)) return false;

                // 替换基础数据
                basics = param;

                // 尝试将当前对象用新 key 添加回容器
                return objList.TryAdd(basics, instance);
            }
        }

        /// <summary>
        /// 异步单例模式
        /// </summary>
        /// <param name="param">
        /// 基础参数<br/>
        /// 不传入则使用默认，直接创建默认对象<br/>
        /// 如果这个单例还想使用时，请使用InstanceBasics方法设置基础数据
        /// </param>
        /// <param name="token">传播消息取消通知</param>
        /// <returns>对象</returns>
        public static async Task<O> InstanceAsync(D? param = null, CancellationToken token = default)
            => await Task.Run(() => Instance(param), token);

        /// <summary>
        /// 异步设置单例基础数据<br/>
        /// 此功能用于创建的单例没有传入基础数据的情况,并且还要继续使用此单例对象<br/>
        /// 此方法对应 Instance 方法未传入基础数据后使用
        /// </summary>
        /// <param name="param">基础参数</param>
        /// <param name="token">传播消息取消通知</param>
        /// <returns>
        /// true : 此单例对象基础数据已被重置为信的对象<br/>
        /// false : 重置失败,此单例对象不在单例容器中,可能是直接new的对象
        /// </returns>
        public async Task<bool> InstanceBasicsAsync(D param, CancellationToken token = default)
            => await Task.Run(() => InstanceBasics(param), token);



        /// <summary>
        /// 开始操作<br/>
        /// 记录函数开始时间
        /// </summary>
        /// <param name="methodName">方法名</param>
        /// <returns>方法名</returns>
        protected string BegOperate([CallerMemberName] string methodName = "")
        {
            TimeHandler.Instance($"{TAG}.{methodName}").StartRecord();
            return methodName;
        }

        /// <summary>
        /// 异步开始操作<br/>
        /// 记录函数开始时间
        /// </summary>
        /// <param name="token">传播消息取消通知</param>
        /// <param name="methodName">方法名</param>
        /// <returns>方法名</returns>
        protected async Task<string> BegOperateAsync(CancellationToken token = default, [CallerMemberName] string methodName = "")
            => await Task.Run(() => BegOperate(methodName), token);

        /// <summary>
        /// 结束操作<br/>
        /// 记录函数运行结束时间，并返回运行时间
        /// </summary>
        /// <param name="status">状态</param>
        /// <param name="message">消息</param>
        /// <param name="resultData">结果数据</param>
        /// <param name="exception">异常信息</param>
        /// <param name="logOutput">
        /// 日志输出<br/>
        /// true : 本地路径日志输出<br/>
        /// false : 控制台输出也会失效
        /// </param>
        /// <param name="consoleOutput">控制台输出</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="methodName">方法名</param>
        /// <param name="lineNumber">行号</param>
        /// <returns>组织好的统一结果</returns>
        protected OperateResult EndOperate(bool status, string? message = null, object? resultData = null, Exception? exception = null, bool logOutput = true, bool consoleOutput = true, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "", [CallerLineNumber] int lineNumber = 0)
        {
            //返回运行时间（毫秒）
            int runTime = TimeHandler.Instance($"{TAG}.{methodName}").StopRecord().milliseconds;
            //组织逗号
            string comma = GetLanguage() == LanguageType.en ? "," : "，";
            //消息数据组织
            string msg = $"[ {TAG} ]( {methodName} {(status ? GetLanguageValue("成功") : GetLanguageValue("异常") + $" < {GetLanguageValue("在")} {Path.GetFileName(filePath)} {GetLanguageValue("文件")}{comma}{GetLanguageValue("第")} {lineNumber} {GetLanguageValue("行")} >")} ){(string.IsNullOrEmpty(message) ? string.Empty : $" : {message}")}";
            //简化信息
            msg = msg.MessageStandard();
            //异常则输出日志
            if (!status && logOutput) { LogHelper.Error(msg, $"{TAG}/{methodName}.log", exception, consoleOutput); }
            //返回状态
            return new OperateResult(status, msg, runTime, resultData);
        }

        /// <summary>
        /// 异步结束操作<br/>
        /// 记录函数运行结束时间，并返回运行时间
        /// </summary>
        /// <param name="status">状态</param>
        /// <param name="message">消息</param>
        /// <param name="resultData">结果数据</param>
        /// <param name="exception">异常信息</param>
        /// <param name="logOutput">
        /// 日志输出<br/>
        /// true : 本地路径日志输出<br/>
        /// false : 控制台输出也会失效
        /// </param>
        /// <param name="consoleOutput">控制台输出</param>
        /// <param name="token">传播消息取消通知</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="methodName">方法名</param>
        /// <param name="lineNumber">行号</param>
        /// <returns>组织好的统一结果</returns>
        protected async Task<OperateResult> EndOperateAsync(bool status, string? message = null, object? resultData = null, Exception? exception = null, bool logOutput = true, bool consoleOutput = true, CancellationToken token = default, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "", [CallerLineNumber] int lineNumber = 0)
            => await Task.Run(() => EndOperate(status, message, resultData, exception, logOutput, consoleOutput, filePath, methodName, lineNumber), token);


        /// <summary>
        /// 结束操作
        /// </summary>
        /// <param name="result">已经组织到的结果数据</param>
        /// <param name="logOutput">
        /// 日志输出<br/>
        /// true : 本地路径日志输出<br/>
        /// false : 控制台输出也会失效
        /// </param>
        /// <param name="consoleOutput">控制台输出</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="methodName">方法名</param>
        /// <param name="lineNumber">行号</param>
        /// <returns>组织好的统一结果</returns>
        protected OperateResult EndOperate(OperateResult result, bool logOutput = true, bool consoleOutput = true, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "", [CallerLineNumber] int lineNumber = 0)
        {
            //返回运行时间（毫秒）
            int runTime = TimeHandler.Instance($"{TAG}.{methodName}").StopRecord().milliseconds;
            //组织逗号
            string comma = GetLanguage() == LanguageType.en ? "," : "，";
            //消息数据组织
            string message = $"[ {TAG} ]( {methodName} {(result.Status ? GetLanguageValue("成功") : GetLanguageValue("异常") + $" < {GetLanguageValue("在")} {Path.GetFileName(filePath)} {GetLanguageValue("文件")}{comma}{GetLanguageValue("第")} {lineNumber} {GetLanguageValue("行")} >")} ){(string.IsNullOrEmpty(result.Message) ? string.Empty : $" : {result.Message}")}";
            //简化信息
            message = message.MessageStandard();
            //异常则输出日志
            if (!result.Status && logOutput) { LogHelper.Error(message: message, filename: $"{TAG}/{methodName}.log", consoleShow: consoleOutput); }
            //返回状态
            return new OperateResult(result, runTime);
        }

        /// <summary>
        /// 异步结束操作
        /// </summary>
        /// <param name="result">已经组织到的结果数据</param>
        /// <param name="logOutput">
        /// 日志输出<br/>
        /// true : 本地路径日志输出<br/>
        /// false : 控制台输出也会失效
        /// </param>
        /// <param name="consoleOutput">控制台输出</param>
        /// <param name="token">传播消息取消通知</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="methodName">方法名</param>
        /// <param name="lineNumber">行号</param>
        /// <returns>组织好的统一结果</returns>
        protected async Task<OperateResult> EndOperateAsync(OperateResult result, bool logOutput = true, bool consoleOutput = true, CancellationToken token = default, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "", [CallerLineNumber] int lineNumber = 0)
            => await Task.Run(() => EndOperate(result, logOutput, consoleOutput, filePath, methodName, lineNumber), token);

        #endregion

        #region 统一实现部分接口方法
        /// <inheritdoc/>
        public OperateResult GetParam(bool getBasicsParam = false)
        {
            BegOperate();
            try
            {
                D bd = Activator.CreateInstance<D>();
                if (getBasicsParam)
                {
                    return EndOperate(true, bd.ToJson(true), bd);
                }
                return EndOperate(ParamHandler.Get(bd, string.IsNullOrWhiteSpace(CN) ? typeof(O).FullName : CN, CD ?? GetLanguageValue("暂无描述"), AP));
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public OperateResult GetAutoAllocatingParam()
        {
            BegOperate();
            try
            {
                if (ExistsAutoAllocatingParam().GetDetails(out string? message, out object? rData))
                {
                    //获取属性名称
                    string? pName = rData?.GetSource<Tuple<Type, string, ReflexHandler.LibInstanceParam>>()?.Item2.ToString();
                    //获取参数
                    if (GetParam().GetDetails(out message, out rData))
                    {
                        subsetSimplify? param = rData?.GetSource<ParamModel>()?.Subset?.FirstOrDefault(c => c.Name == typeof(D)?.GetProperty(pName)?.GetValue(basics)?.ToString()).ToJson()?.ToJsonEntity<subsetSimplify>();
                        return EndOperate(true, param?.ToJson(), param);
                    }
                }
                return EndOperate(false, $"{message}，{GetLanguageValue("无法获取自动分配标识的属性值所对应参数集合")}");
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public OperateResult ExistsAutoAllocatingParam()
        {
            BegOperate();
            try
            {
                if (basics != null)
                {
                    //通过反射得到当前类的所有属性信息
                    List<ReflexHandler.LibInstanceParam>? libInstanceParams = ReflexHandler.GetClassAllPropertyData<D>();
                    //得到是否存在下标标识属性,并得到这个标识的枚举类型,这是用于自动分配属性的标识
                    Tuple<Type, string, ReflexHandler.LibInstanceParam>? enumType = libInstanceParams.Select(c =>
                    {
                        AutoAllocatingTagAttribute? indexesTagAttribute = typeof(D).GetProperty(c.Name).GetCustomAttribute<AutoAllocatingTagAttribute>();
                        if (indexesTagAttribute != null)
                        {
                            return new Tuple<Type, string, ReflexHandler.LibInstanceParam>(indexesTagAttribute.EnumType, c.Name, c);
                        }
                        return null;
                    }).FirstOrDefault(c => c != null);
                    //存在自动分配属性枚举　AutoAllocatingTagAttribute　特性
                    if (enumType != null)
                    {
                        return EndOperate(true, resultData: enumType, logOutput: false);
                    }
                    return EndOperate(false, GetLanguageValue("ExistsAutoAllocatingParam_msg1"), logOutput: false);
                }
                return EndOperate(false, GetLanguageValue("ExistsAutoAllocatingParam_msg2"), logOutput: false);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public OperateResult CreateInstance<T>(T param)
        {
            BegOperate();
            try
            {
                //先判断对象类型是否一致
                if (typeof(T).FullName.Equals(typeof(D).FullName))
                {
                    return EndOperate(true, resultData: Instance(param as D));
                }
                else
                {
                    return EndOperate(false, GetLanguageValue("对象类型错误，无法创建实例"));
                }
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public OperateResult CreateInstance(string json)
        {
            D? basics = json.ToJsonEntity<D>();
            if (basics == null)
            {
                return OperateResult.CreateFailureResult(GetLanguageValue("JSON反序列化失败，JSON数据对象类型错误，无法创建实例"));
            }
            return CreateInstance(basics);
        }
        /// <inheritdoc/>
        public OperateResult LogOperateSet(bool logOut = true, bool? consoleOut = null)
        {
            BegOperate();
            try
            {
                LogModel logModel = LogHelper.Get();
                logModel.Out = logOut;
                logModel.ConsoleOut = consoleOut;
                LogHelper.Set(logModel);
                return EndOperate(true, GetLanguageValue("日志参数设置成功"));
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public OperateResult LogOperateSet(bool logOut = true, bool? consoleOut = null, Action<object, Serilog.Events.LogEventLevel, string?, Exception?>? notice = null)
        {
            BegOperate();
            try
            {
                LogModel logModel = LogHelper.Get();
                logModel.Notice = notice;
                logModel.Out = logOut;
                logModel.ConsoleOut = consoleOut;
                LogHelper.Set(logModel);
                return EndOperate(true, GetLanguageValue("日志参数设置成功"));
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public OperateResult LogOperateSet(bool logOut = true, bool? consoleOut = null, Func<object, Serilog.Events.LogEventLevel, string?, Exception?, Task>? noticeAsync = null)
        {
            BegOperate();
            try
            {
                LogModel logModel = LogHelper.Get();
                logModel.NoticeAsync = noticeAsync;
                logModel.Out = logOut;
                logModel.ConsoleOut = consoleOut;
                LogHelper.Set(logModel);
                return EndOperate(true, GetLanguageValue("日志参数设置成功"));
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public OperateResult LogOperateSet(LogModel logModel)
        {
            BegOperate();
            try
            {
                LogHelper.Set(logModel);
                return EndOperate(true, GetLanguageValue("日志参数设置成功"));
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public OperateResult LogOperateGet()
        {
            BegOperate();
            try
            {
                return EndOperate(true, GetLanguageValue("日志参数获取成功"), LogHelper.Get());
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
        /// <inheritdoc/>
        public OperateResult GetBasicsData()
        {
            if (basics == null)
            {
                return OperateResult.CreateFailureResult(GetLanguageValue("获取失败，基础数据尚未实例化"));
            }
            return OperateResult.CreateSuccessResult(GetLanguageValue("获取成功"), basics);
        }

        /// <inheritdoc/>
        public async Task<OperateResult> GetParamAsync(bool getBasicsParam = false, CancellationToken token = default)
            => await Task.Run(() => GetParam(getBasicsParam), token);
        /// <inheritdoc/>
        public async Task<OperateResult> GetAutoAllocatingParamAsync(CancellationToken token = default)
            => await Task.Run(() => GetAutoAllocatingParam(), token);
        /// <inheritdoc/>
        public async Task<OperateResult> ExistsAutoAllocatingParamAsync(CancellationToken token = default)
            => await Task.Run(() => ExistsAutoAllocatingParam(), token);
        /// <inheritdoc/>
        public async Task<OperateResult> CreateInstanceAsync<T>(T param, CancellationToken token = default)
            => await Task.Run(() => CreateInstance(param), token);
        /// <inheritdoc/>
        public async Task<OperateResult> CreateInstanceAsync(string json, CancellationToken token = default)
            => await Task.Run(() => CreateInstance(json), token);
        /// <inheritdoc/>
        public async Task<OperateResult> LogOperateSetAsync(bool logOut = true, bool? consoleOut = null, CancellationToken token = default)
            => await Task.Run(() => LogOperateSet(logOut, consoleOut), token);
        /// <inheritdoc/>
        public async Task<OperateResult> LogOperateSetAsync(bool logOut = true, bool? consoleOut = null, Action<object, Serilog.Events.LogEventLevel, string?, Exception?>? notice = null, CancellationToken token = default)
            => await Task.Run(() => LogOperateSet(logOut, consoleOut, notice), token);
        /// <inheritdoc/>
        public async Task<OperateResult> LogOperateSetAsync(bool logOut = true, bool? consoleOut = null, Func<object, Serilog.Events.LogEventLevel, string?, Exception?, Task>? noticeAsync = null, CancellationToken token = default)
            => await Task.Run(() => LogOperateSet(logOut, consoleOut, noticeAsync), token);
        /// <inheritdoc/>
        public async Task<OperateResult> LogOperateSetAsync(LogModel logModel, CancellationToken token = default)
            => await Task.Run(() => LogOperateSet(logModel), token);
        /// <inheritdoc/>
        public async Task<OperateResult> LogOperateGetAsync(CancellationToken token = default)
            => await Task.Run(() => LogOperateGet(), token);
        /// <inheritdoc/>
        public async Task<OperateResult> GetBasicsDataAsync(CancellationToken token = default)
            => await Task.Run(() => GetBasicsData(), token);


        #endregion

        #region 国际化多语言

        /// <inheritdoc/>
        public virtual LanguageModel LanguageOperate { get; set; }

        /// <inheritdoc/>
        public string? GetLanguageValue(string key, LanguageModel? languageModel = null)
            => LanguageHandler.GetLanguageValue(key, languageModel);
        /// <inheritdoc/>
        public LanguageType GetLanguage()
            => LanguageHandler.GetLanguage();
        /// <inheritdoc/>
        public bool SetLanguage(LanguageType language)
            => LanguageHandler.SetLanguage(language);

        /// <inheritdoc/>
        public async Task<string?> GetLanguageValueAsync(string key, LanguageModel? languageModel = null, CancellationToken token = default)
            => await LanguageHandler.GetLanguageValueAsync(key, languageModel, token);
        /// <inheritdoc/>
        public async Task<LanguageType> GetLanguageAsync(CancellationToken token = default)
            => await LanguageHandler.GetLanguageAsync(token);
        /// <inheritdoc/>
        public async Task<bool> SetLanguageAsync(LanguageType language, CancellationToken token = default)
            => await LanguageHandler.SetLanguageAsync(language, token);

        #endregion
    }
}
