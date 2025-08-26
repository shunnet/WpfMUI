using Newtonsoft.Json.Linq;
using Snet.Core.extend;
using Snet.Model.data;
using Snet.Model.@interface;
using Snet.Unility;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Channels;
namespace Snet.Core.mq
{
    /// <summary>
    /// 消息中间件操作；<br/>
    /// 库：*.dll；<br/>
    /// 库配置：命名空间 + 类名.ISn.Config.json
    /// </summary>
    public class MqOperate : CoreUnify<MqOperate, MqData>, IDisposable
    {
        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="basics">基础数据</param>
        public MqOperate(MqData basics) : base(basics) { Monitor(); }

        /// <summary>
        /// 实例容器集合
        /// </summary>
        private ConcurrentDictionary<string, IMq> InstanceIoc = new ConcurrentDictionary<string, IMq>();

        /// <summary>
        /// 库类型容器
        /// </summary>
        private ConcurrentDictionary<string, Type>? TypeIoc = null;

        /// <summary>
        /// 文件夹监控
        /// </summary>
        private FileSystemWatcher? watcherLibFolder = null;

        /// <summary>
        /// 文件夹监控
        /// </summary>
        private FileSystemWatcher? watcherLibConfigFolder = null;

        /// <summary>
        /// 任务集合
        /// </summary>
        private ConcurrentDictionary<CancellationTokenSource, Task>? TaskArray = null;

        /// <summary>
        /// 数据队列
        /// </summary>
        private Channel<QueueData>? DataQueue = null;

        /// <summary>
        /// 通道设置
        /// </summary>
        private BoundedChannelOptions channelOptions = new BoundedChannelOptions(int.MaxValue)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        /// <summary>
        /// 队列里面的数据
        /// </summary>
        private class QueueData
        {
            /// <summary>
            /// 主题
            /// </summary>
            public string? Topic { get; set; }

            /// <summary>
            /// 内容
            /// </summary>
            public object? Content { get; set; }

            /// <summary>
            /// 实例唯一标识符集合，空则全部发送
            /// </summary>
            public List<string>? ISns { get; set; } = null;
        }

        /// <summary>
        /// 监控文件夹
        /// </summary>
        private void Monitor()
        {
            //打开失败监控
            if (OnFailToken == null)
            {
                OnFailToken = new CancellationTokenSource();
                OnFailTaskHandler(OnFailToken.Token);
            }

            //程序集
            if (TypeIoc == null)
            {
                TypeIoc = new ConcurrentDictionary<string, Type>();
            }

            if (basics != null)
            {
                //创建文件
                if (!Directory.Exists(basics.LibFolder))
                {
                    Directory.CreateDirectory(basics.LibFolder);
                }
                //创建文件
                if (!Directory.Exists(basics.LibConfigFolder))
                {
                    Directory.CreateDirectory(basics.LibConfigFolder);
                }

                //检索
                Search();

                //文件夹监控
                watcherLibFolder = new FileSystemWatcher(basics.LibFolder);
                // 设置监控类型
                watcherLibFolder.NotifyFilter =
                NotifyFilters.FileName |                    //文件的名称
                NotifyFilters.DirectoryName |               //目录名
                NotifyFilters.Attributes |                  //文件或文件夹的属性
                NotifyFilters.Size |                        //文件或文件夹的大小
                NotifyFilters.LastWrite |                   //文件或文件夹最后写入内容的日期
                NotifyFilters.LastAccess |                  //文件或文件夹最后被打开的日期
                NotifyFilters.CreationTime |                //创建文件或文件夹的时间
                NotifyFilters.Security;                     //文件或文件夹的安全设置
                                                            //当文件夹中新增文件
                watcherLibFolder.Created += delegate (object sender, FileSystemEventArgs e) { Watcher_Created(sender, e, 0); };
                //当文件夹中删除文件
                watcherLibFolder.Deleted += delegate (object sender, FileSystemEventArgs e) { Watcher_Deleted(sender, e, 0); };
                //启动监听
                watcherLibFolder.EnableRaisingEvents = true;

                //文件夹监视
                watcherLibConfigFolder = new FileSystemWatcher(basics.LibConfigFolder);
                //监控的配置
                watcherLibConfigFolder.Filter = basics.ConfigWatcherFormat;
                // 设置监控类型
                watcherLibFolder.NotifyFilter =
                NotifyFilters.FileName |                    //文件的名称
                NotifyFilters.DirectoryName |               //目录名
                NotifyFilters.Attributes |                  //文件或文件夹的属性
                NotifyFilters.Size |                        //文件或文件夹的大小
                NotifyFilters.LastWrite |                   //文件或文件夹最后写入内容的日期
                NotifyFilters.LastAccess |                  //文件或文件夹最后被打开的日期
                NotifyFilters.CreationTime |                //创建文件或文件夹的时间
                NotifyFilters.Security;                     //文件或文件夹的安全设置
                                                            //当文件夹中新增文件
                watcherLibConfigFolder.Created += delegate (object sender, FileSystemEventArgs e) { Watcher_Created(sender, e, 1); };
                //当文件夹中删除文件
                watcherLibConfigFolder.Deleted += delegate (object sender, FileSystemEventArgs e) { Watcher_Deleted(sender, e, 1); };
                //启动监听
                watcherLibConfigFolder.EnableRaisingEvents = true;
            }
            else
            {
                OnInfoEventHandler(this, new EventInfoResult(false, $"配置文件不存在"));
            }
        }

        /// <summary>
        /// 数据队列
        /// </summary>
        private Channel<WatcherData>? WatcherQueue = null;

        /// <summary>
        /// 监控开关
        /// </summary>
        private CancellationTokenSource? WatcherToken = null;

        /// <summary>
        /// 文件监控数据
        /// </summary>
        private class WatcherData
        {
            public FileSystemEventArgs e { get; set; }
            public int Type { get; set; }
            public WatcherType WType { get; set; }

            public enum WatcherType
            {
                Deleted,
                Created
            }
        }

        /// <summary>
        /// 监控任务
        /// </summary>
        /// <returns></returns>
        public async Task WatcherTask(CancellationToken token)
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (await WatcherQueue.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                    {
                        while (WatcherQueue.Reader.TryRead(out var watcherData))
                        {
                            await Task.Delay(1000);

                            FileSystemEventArgs e = watcherData.e;
                            int Type = watcherData.Type;
                            //程序集ISn
                            string TypeIocSN = string.Empty;
                            switch (watcherData.WType)
                            {
                                case WatcherData.WatcherType.Deleted:

                                    switch (Type)
                                    {
                                        case 0:
                                            //这是文件夹
                                            if (Directory.Exists(e.FullPath))
                                            {
                                                //检索里面的文件
                                                List<string> libs = Directory.GetFiles(e.FullPath, basics.DllWatcherFormat, SearchOption.AllDirectories).ToList();
                                                //检索文件
                                                foreach (var lib in libs)
                                                {
                                                    //文件名称
                                                    string fileName = FileHandler.GetFileName(lib);

                                                    //程序集ISn
                                                    TypeIocSN = fileName.Replace(".dll", string.Empty);

                                                    //检索实例容器
                                                    foreach (var item in InstanceIoc)
                                                    {
                                                        if (item.Key.Contains(TypeIocSN))
                                                        {
                                                            InstanceIoc[item.Key].Dispose();
                                                            if (InstanceIoc.Remove(item.Key, out _))
                                                            {
                                                                OnInfoEventHandler(this, new EventInfoResult(true, $"{e.Name} 移除配置实例 {item.Key} 成功"));
                                                            }
                                                            else
                                                            {
                                                                OnInfoEventHandler(this, new EventInfoResult(false, $"{e.Name} 移除配置实例 {item.Key} 失败"));
                                                            }
                                                        }
                                                    }
                                                    //检索库类型容器
                                                    foreach (var item in TypeIoc)
                                                    {
                                                        if (item.Key.Contains(TypeIocSN))
                                                        {
                                                            if (TypeIoc.Remove(item.Key, out _))
                                                            {
                                                                OnInfoEventHandler(this, new EventInfoResult(true, $"{e.Name} 移除程序集 {item.Key} 成功"));
                                                            }
                                                            else
                                                            {
                                                                OnInfoEventHandler(this, new EventInfoResult(false, $"{e.Name} 移除程序集 {item.Key} 失败"));
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            break;

                                        case 1:
                                            OnInfoEventHandler(this, new EventInfoResult(true, $"{e.Name} 文件被删除，移除对应配置实例"));
                                            //实例的ISn
                                            string InstanceIocSN = e.Name.Replace(basics.ConfigReplaceFormat, string.Empty);
                                            //程序集ISn
                                            TypeIocSN = InstanceIocSN.Replace($".{InstanceIocSN.Split('.')[InstanceIocSN.Split('.').Length - 1]}", string.Empty);
                                            if (InstanceIoc.ContainsKey(InstanceIocSN))
                                            {
                                                InstanceIoc[InstanceIocSN].Dispose();
                                                if (InstanceIoc.Remove(InstanceIocSN, out _))
                                                {
                                                    OnInfoEventHandler(this, new EventInfoResult(true, $"{e.Name} 移除配置实例成功"));
                                                }
                                                else
                                                {
                                                    OnInfoEventHandler(this, new EventInfoResult(false, $"{e.Name} 移除配置实例失败"));
                                                }
                                            }
                                            else
                                            {
                                                OnInfoEventHandler(this, new EventInfoResult(false, $"{e.Name} 移除配置实例失败 {TypeIocSN} 实例不存在"));
                                            }
                                            break;
                                    }

                                    break;

                                case WatcherData.WatcherType.Created:

                                    switch (Type)
                                    {
                                        case 0:
                                            //这是文件夹
                                            if (Directory.Exists(e.FullPath))
                                            {
                                                //检索里面的文件
                                                Search(e.FullPath);
                                            }
                                            break;

                                        case 1:
                                            OnInfoEventHandler(this, new EventInfoResult(true, $"{e.Name} 文件新增，新增对应配置实例"));

                                            #region 新增对应配置实例

                                            int until = 5;
                                            int i = 0;
                                            bool success = false;
                                            while (!success && i < until)
                                            {
                                                try
                                                {
                                                    string path = String.Format(e.FullPath);
                                                    string? filename = Path.GetFileName(path);
                                                    using (Stream fs = System.IO.File.OpenRead(@path))
                                                    {
                                                        StreamReader srdPreview = new StreamReader(fs);
                                                        String temp = string.Empty;
                                                        while (srdPreview.Peek() > -1)
                                                        {
                                                            String input = srdPreview.ReadLine();
                                                            temp += input;
                                                        }
                                                        srdPreview.Close();
                                                        srdPreview.Dispose();

                                                        //实例的ISn
                                                        string InstanceIocSN = e.Name.Replace(basics.ConfigReplaceFormat, string.Empty);
                                                        //程序集ISn
                                                        TypeIocSN = InstanceIocSN.Replace($".{InstanceIocSN.Split('.')[InstanceIocSN.Split('.').Length - 1]}", string.Empty);
                                                        //判断是否存在此程序集ISn
                                                        if (TypeIoc.ContainsKey(TypeIocSN))
                                                        {
                                                            if (!InstanceIoc.ContainsKey(InstanceIocSN))
                                                            {
                                                                ConfigCreateInstance(TypeIoc[TypeIocSN], temp);
                                                            }
                                                            else
                                                            {
                                                                OnInfoEventHandler(this, new EventInfoResult(false, $" {e.Name} 此配置实例已存在"));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            OnInfoEventHandler(this, new EventInfoResult(false, $" {e.Name} 新增对应配置创建实例失败 {TypeIocSN} 程序集不存在"));
                                                        }
                                                        fs.Close();
                                                        fs.Dispose();
                                                    }
                                                    success = true;
                                                }
                                                catch
                                                {
                                                    i++;
                                                    await Task.Delay(TimeSpan.FromSeconds(1));
                                                }
                                            }

                                            #endregion 新增对应配置实例

                                            break;
                                    }

                                    break;
                            }
                        }
                    }
                }, token);
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }

        /// <summary>
        /// 当文件夹中删除文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="Type">【0：库文件】【1：配置文件】</param>
        private void Watcher_Deleted(object sender, FileSystemEventArgs e, int Type)
        {
            if (WatcherToken == null && WatcherQueue == null)
            {
                WatcherQueue = Channel.CreateBounded<WatcherData>(channelOptions);
                WatcherToken = new CancellationTokenSource();
                WatcherTask(WatcherToken.Token);
            }
            WatcherQueue?.Writer.WriteAsync(new WatcherData { e = e, Type = Type, WType = WatcherData.WatcherType.Deleted }, WatcherToken.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// 当文件夹中新增文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="Type">【0：库文件】【1：配置文件】</param>
        private void Watcher_Created(object sender, FileSystemEventArgs e, int Type)
        {
            if (WatcherToken == null && WatcherQueue == null)
            {
                WatcherQueue = Channel.CreateBounded<WatcherData>(channelOptions);
                WatcherToken = new CancellationTokenSource();
                WatcherTask(WatcherToken.Token);
            }
            WatcherQueue?.Writer.WriteAsync(new WatcherData { e = e, Type = Type, WType = WatcherData.WatcherType.Created }, WatcherToken.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// 检索文件并创建实例
        /// </summary>
        private void Search(string? path = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = basics.LibFolder;
                }
                //库
                List<string> libs = Directory.GetFiles(path, basics.DllWatcherFormat, SearchOption.AllDirectories).ToList();
                //循环文件，添加程序集
                foreach (var lib in libs)
                {
                    SearchType(lib);
                }
                //获取配置
                foreach (var type in TypeIoc)
                {
                    TypeSearchConfig(type.Value);
                }
            }
            catch (Exception ex)
            {
                OnInfoEventHandler(this, new EventInfoResult(false, $"检索异常：{ex.Message}"));
            }
        }

        /// <summary>
        /// 通过DLL检索程序集
        /// </summary>
        /// <param name="lib">库文件</param>
        private void SearchType(string lib)
        {
            try
            {
                //加载程序集
                Assembly assembly = Assembly.LoadFrom(lib);
                //获取所有类
                Type[] types = assembly.GetExportedTypes().Where(c => !c.IsAbstract && c.IsClass).ToArray();
                //过滤器
                TypeFilter typeFilter = new TypeFilter(InterfaceFilter);
                //集合
                List<Type> typesArray = new List<Type>();
                //检索类是否继承接口
                foreach (Type type in types)
                {
                    if (type.FindInterfaces(typeFilter, basics.InterfaceFullName).Count() > 0)
                    {
                        typesArray.Add(type);
                    }
                }
                //添加至集合
                foreach (Type type in typesArray)
                {
                    TypeIoc.TryAdd(type.FullName, type);
                    OnInfoEventHandler(this, new EventInfoResult(true, $"{type.FullName} 程序集添加成功"));
                }
            }
            catch (Exception)
            {
                Task.Delay(1).Wait();
                SearchType(lib);
            }
        }

        /// <summary>
        /// 通过程序集检索配置
        /// </summary>
        /// <param name="type">程序集</param>
        private void TypeSearchConfig(Type type)
        {
            //组织配置文件
            string configFile = string.Format(basics.ConfigFileNameFormat, type.FullName);
            //目录信息
            DirectoryInfo directoryInfo = new DirectoryInfo(basics.LibConfigFolder);
            //文件检索得到文件信息
            List<FileInfo> fieldInfos = directoryInfo.GetFiles(configFile, SearchOption.AllDirectories).ToList();
            //如果文件信息为空则不创建实例
            if (fieldInfos.Count > 0)
            {
                //循环检索配置文件信息
                foreach (FileInfo fi in fieldInfos)
                {
                    ConfigCreateInstance(type, FileHandler.FileToString(fi.FullName));
                }
            }
        }

        /// <summary>
        /// 打开失败的实例对象，用于重新打开使用
        /// </summary>
        private ConcurrentDictionary<string, IMq> OnFailIoc = new ConcurrentDictionary<string, IMq>();

        /// <summary>
        /// 打开失败处理任务的生命周期
        /// </summary>
        private CancellationTokenSource? OnFailToken = null;

        /// <summary>
        /// 打开失败任务处理间隔
        /// </summary>
        private int OnFailTaskHandlerInterval = 1000;

        /// <summary>
        /// 打开失败重新打开的任务
        /// </summary>
        /// <returns></returns>
        private async Task OnFailTaskHandler(CancellationToken token)
        {
            try
            {
                await Task.Run(async () =>
                {
                    //打开成功的SN
                    List<string> OnSucceedSN = new List<string>();
                    while (!token.IsCancellationRequested)
                    {
                        //重新打开
                        foreach (var item in OnFailIoc)
                        {
                            if (item.Value.On().Status)
                            {
                                OnSucceedSN.Add(item.Key);
                            }
                        }
                        //打开成功后从失败的IOC中移除
                        foreach (var item in OnSucceedSN)
                        {
                            OnFailIoc.Remove(item, out _);
                        }
                        //清空打开成功的数组
                        OnSucceedSN.Clear();
                        //休眠时间
                        await Task.Delay(OnFailTaskHandlerInterval);
                    }
                }, token);
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }


        /// <summary>
        /// 通过配置创建实例
        /// </summary>
        /// <param name="type">程序集</param>
        /// <param name="content">内容</param>
        private void ConfigCreateInstance(Type type, string content)
        {
            //获取结构参数
            JObject? jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(content);
            //获取唯一标识符
            string InstanceIocSN = $"{type.FullName}.{jsonObject[basics.LibConfigSNKey]}";
            //获取实例
            IMq? instance = CreateInstance(type, new object[] { jsonObject }) as IMq;
            //实例不为空
            if (instance != null)
            {
                //把这个实例添加到容器中
                InstanceIoc.TryAdd(InstanceIocSN, instance);
                //自动打开
                if (basics.AutoOn)
                {
                    //执行打开方法 
                    OperateResult operateResult = instance.On();
                    //如果打开状态为失败
                    if (!operateResult.Status)
                    {
                        //添加到打开失败重新打开流程中
                        OnFailIoc.AddOrUpdate(InstanceIocSN, instance, (k, v) => instance);
                    }
                    //抛出信息
                    OnInfoEventHandler(this, new EventInfoResult(operateResult.Status, $"{InstanceIocSN} 实例创建成功，自动打开{(operateResult.Status ? "成功" : "失败")}"));
                }
                else
                {
                    //抛出信息
                    OnInfoEventHandler(this, new EventInfoResult(true, $"{InstanceIocSN} 实例创建成功"));
                }
            }
            else
            {
                //抛出信息
                OnInfoEventHandler(this, new EventInfoResult(false, $"{InstanceIocSN} 实例创建失败"));
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
                ConstructorInfo? constructorInfo = NamespaceAndClassNameType?.GetConstructors().Where(c => c.GetParameters().Count() > 0).FirstOrDefault();

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
                                JObject? JsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(Newtonsoft.Json.JsonConvert.SerializeObject(Data[i]));
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
        /// 接口过滤器
        /// </summary>
        private bool InterfaceFilter(Type typeObj, Object criteriaObj)
        {
            if (typeObj.ToString() == criteriaObj.ToString())
                return true;
            else
                return false;
        }

        /// <summary>
        /// 任务处理
        /// </summary>
        /// <returns></returns>
        private async Task TaskHandle(CancellationToken token)
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (await DataQueue.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                    {
                        while (DataQueue.Reader.TryRead(out var queueData))
                        {
                            if (queueData != null)
                            {
                                if (queueData.ISns == null || queueData.ISns.Count <= 0)
                                {
                                    foreach (var item in InstanceIoc)
                                    {
                                        //存在实例，并且已经打开
                                        if (item.Value.GetStatus().Status)
                                        {
                                            OperateResult operateResult = null;
                                            if (queueData.Content is byte[])
                                            {
                                                operateResult = item.Value.Produce(queueData.Topic, queueData.Content.GetSource<byte[]>());
                                            }
                                            else if (queueData.Content is string)
                                            {
                                                operateResult = item.Value.Produce(queueData.Topic, queueData.Content.GetSource<string>());
                                            }
                                            OnInfoEventHandler(this, new EventInfoResult(operateResult.Status, operateResult.Message));
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var ISn in queueData.ISns)
                                    {
                                        //存在实例，并且已经打开
                                        if (InstanceIoc.ContainsKey(ISn) && InstanceIoc[ISn].GetStatus().Status)
                                        {
                                            OperateResult operateResult = null;
                                            if (queueData.Content is byte[])
                                            {
                                                operateResult = InstanceIoc[ISn].Produce(queueData.Topic, queueData.Content.GetSource<byte[]>());
                                            }
                                            else if (queueData.Content is string)
                                            {
                                                operateResult = InstanceIoc[ISn].Produce(queueData.Topic, queueData.Content.GetSource<string>());
                                            }
                                            OnInfoEventHandler(this, new EventInfoResult(operateResult.Status, operateResult.Message));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }, token);
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }
        /// <inheritdoc/>
        public override void Dispose()
        {
            if (OnFailToken != null)
            {
                OnFailToken.Cancel();
                OnFailToken.Dispose();
                OnFailToken = null;
            }

            if (WatcherToken != null)
            {
                WatcherToken.Cancel();
                WatcherToken.Dispose();
                WatcherToken = null;
            }

            if (WatcherQueue != null)
            {
                WatcherQueue.Writer.Complete();
                while (WatcherQueue.Reader.TryRead(out _)) { }
                WatcherQueue = null;
            }

            //任务清空
            if (TaskArray != null)
            {
                foreach (var item in TaskArray)
                {
                    item.Key.Cancel();
                }
                TaskArray.Clear();
                TaskArray = null;
            }

            //容器实例释放
            foreach (var item in InstanceIoc) { item.Value.Dispose(); }

            if (DataQueue != null)
            {
                DataQueue.Writer.Complete();
                while (DataQueue.Reader.TryRead(out _)) { }
                DataQueue = null;
            }
            if (InstanceIoc != null)
            {
                InstanceIoc.Clear();
            }
            if (TypeIoc != null)
            {
                TypeIoc.Clear();
                TypeIoc = null;
            }

            base.Dispose();
        }

        /// <inheritdoc/>
        public override async Task DisposeAsync()
        {
            Dispose();
            await base.DisposeAsync();
        }

        /// <summary>
        /// 释放指定实例
        /// </summary>
        /// <param name="ISn">实例唯一标识符</param>
        /// <returns>统一出参</returns>
        public OperateResult Dispose(string ISn)
        {
            BegOperate();
            try
            {
                if (InstanceIoc.ContainsKey(ISn))
                {
                    OperateResult operateResult = Remove(new List<string> { ISn });
                    return EndOperate(operateResult.Status, operateResult.Message);
                }
                else
                {
                    return EndOperate(false, $"未找到 {ISn} 的实例");
                }
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }


        /// <summary>
        /// 程序集唯一标识符集合
        /// </summary>
        /// <returns>统一出参</returns>
        public List<string>? TypeSns()
        {
            if (TypeIoc != null)
            {
                return TypeIoc.Keys.ToList();
            }
            return null;
        }

        /// <summary>
        /// 实例唯一标识符集合
        /// </summary>
        /// <returns>统一出参</returns>
        public List<string>? InstanceSns()
        {
            if (InstanceIoc != null)
            {
                return InstanceIoc.Keys.ToList();
            }
            return null;
        }

        /// <summary>
        /// 移除指定实例
        /// </summary>
        /// <param name="ISns">实例唯一标识符为空则执行所有</param>
        /// <returns>统一出参</returns>
        public OperateResult Remove(List<string>? ISns = null)
        {
            BegOperate();
            try
            {
                try
                {
                    List<string> FailMessage = new List<string>();
                    if (ISns != null)
                    {
                        foreach (var sn in ISns)
                        {
                            if (InstanceIoc.ContainsKey(sn))
                            {
                                IMq? relay;
                                if (InstanceIoc.Remove(sn, out relay))
                                {
                                    if (relay != null)
                                    {
                                        //直接释放
                                        relay.Dispose();
                                    }
                                }
                                else
                                {
                                    FailMessage.Add($"{sn} 的实例移除失败");
                                }
                            }
                            else
                            {
                                FailMessage.Add($"未找到 {sn} 的实例");
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in InstanceIoc)
                        {
                            item.Value.Dispose();
                        }
                        InstanceIoc.Clear();
                    }
                    if (FailMessage.Count > 0)
                    {
                        return EndOperate(false, $"存在 {FailMessage.Count} 失败信息", FailMessage);
                    }
                    else
                    {
                        return EndOperate(true);
                    }
                }
                catch (Exception ex)
                {
                    return EndOperate(false, ex.Message, exception: ex);
                }
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 打开
        /// </summary>
        /// <param name="ISns">实例唯一标识符为空则执行所有</param>
        /// <returns>统一出参</returns>
        public OperateResult On(List<string>? ISns = null)
        {
            BegOperate();
            try
            {
                List<string> FailMessage = new List<string>();
                if (ISns != null)
                {
                    foreach (var sn in ISns)
                    {
                        if (InstanceIoc.ContainsKey(sn))
                        {
                            OperateResult operateResult = InstanceIoc[sn].On();
                            if (!operateResult.Status)
                            {
                                FailMessage.Add(operateResult.Message);
                            }
                        }
                        else
                        {
                            FailMessage.Add($"未找到 {sn} 的实例");
                        }
                    }
                }
                else
                {
                    foreach (var item in InstanceIoc)
                    {
                        OperateResult operateResult = item.Value.On();
                        if (!operateResult.Status)
                        {
                            FailMessage.Add(operateResult.Message);
                        }
                    }
                }
                if (FailMessage.Count > 0)
                {
                    return EndOperate(false, $"存在 {FailMessage.Count} 失败信息", FailMessage);
                }
                else
                {
                    return EndOperate(true);
                }
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        /// <param name="ISns">实例唯一标识符为空则执行所有</param>
        /// <returns>统一出参</returns>
        public OperateResult Off(List<string>? ISns = null)
        {
            BegOperate();
            try
            {
                List<string> FailMessage = new List<string>();
                if (ISns != null)
                {
                    foreach (var sn in ISns)
                    {
                        if (InstanceIoc.ContainsKey(sn))
                        {
                            OperateResult operateResult = InstanceIoc[sn].Off();
                            if (!operateResult.Status)
                            {
                                FailMessage.Add(operateResult.Message);
                            }
                        }
                        else
                        {
                            FailMessage.Add($"未找到 {sn} 的实例");
                        }
                    }
                }
                else
                {
                    foreach (var item in InstanceIoc)
                    {
                        OperateResult operateResult = item.Value.Off();
                        if (!operateResult.Status)
                        {
                            FailMessage.Add(operateResult.Message);
                        }
                    }
                }

                if (FailMessage.Count > 0)
                {
                    return EndOperate(false, $"存在 {FailMessage.Count} 失败信息", FailMessage);
                }
                else
                {
                    return EndOperate(true);
                }

            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 生产
        /// </summary>
        /// <param name="Topic">主题</param>
        /// <param name="Content">内容</param>
        /// <param name="ISns">实例唯一标识符集合，空则全部发送</param>
        /// <returns>统一出参</returns>
        public OperateResult Produce(string Topic, string Content, List<string>? ISns = null)
        {
            BegOperate();
            try
            {
                //当队列为空，初始化队列
                if (DataQueue == null)
                {
                    DataQueue = Channel.CreateBounded<QueueData>(channelOptions);
                }
                //任务为空创建任务
                if (TaskArray == null)
                {
                    TaskArray = new ConcurrentDictionary<CancellationTokenSource, Task>();
                    //创建任务
                    for (int i = 0; i < basics.TaskNumber; i++)
                    {
                        CancellationTokenSource token = new CancellationTokenSource();
                        TaskArray.TryAdd(token, TaskHandle(token.Token));
                    }
                }
                List<string> FailMessage = new List<string>();

                if (ISns == null || ISns.Count <= 0)
                {
                    //入列
                    DataQueue?.Writer.WriteAsync(new QueueData() { Topic = Topic, Content = Content, ISns = ISns }).ConfigureAwait(false);
                }
                else
                {
                    for (int i = 0; i < ISns.Count; i++)
                    {
                        if (!InstanceIoc.ContainsKey(ISns[i]))
                        {
                            FailMessage.Add($"{ISns[i]} 实例未找到");
                        }
                    }
                    //入列
                    DataQueue?.Writer.WriteAsync(new QueueData() { Topic = Topic, Content = Content, ISns = ISns }).ConfigureAwait(false);
                }
                if (FailMessage.Count > 0)
                {
                    return EndOperate(false, $"存在 {FailMessage.Count} 失败信息，{FailMessage.ToJson()}", FailMessage, consoleOutput: false);
                }
                else
                {
                    return EndOperate(true);
                }
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }


        /// <summary>
        /// 生产
        /// </summary>
        /// <param name="Topic">主题</param>
        /// <param name="Content">内容</param>
        /// <param name="ISns">实例唯一标识符集合，空则全部发送</param>
        /// <returns>统一出参</returns>
        public OperateResult Produce(string Topic, byte[] Content, List<string>? ISns = null)
        {
            BegOperate();
            try
            {
                //当队列为空，初始化队列
                if (DataQueue == null)
                {
                    DataQueue = Channel.CreateBounded<QueueData>(channelOptions);
                }
                //任务为空创建任务
                if (TaskArray == null)
                {
                    TaskArray = new ConcurrentDictionary<CancellationTokenSource, Task>();
                    //创建任务
                    for (int i = 0; i < basics.TaskNumber; i++)
                    {
                        CancellationTokenSource token = new CancellationTokenSource();
                        TaskArray.TryAdd(token, TaskHandle(token.Token));
                    }
                }
                List<string> FailMessage = new List<string>();

                if (ISns == null || ISns.Count <= 0)
                {
                    //入列
                    DataQueue?.Writer.WriteAsync(new QueueData() { Topic = Topic, Content = Content, ISns = ISns }).ConfigureAwait(false);
                }
                else
                {
                    for (int i = 0; i < ISns.Count; i++)
                    {
                        if (!InstanceIoc.ContainsKey(ISns[i]))
                        {
                            FailMessage.Add($"{ISns[i]} 实例未找到");
                        }
                    }

                    //入列
                    DataQueue?.Writer.WriteAsync(new QueueData() { Topic = Topic, Content = Content, ISns = ISns }).ConfigureAwait(false);
                }
                if (FailMessage.Count > 0)
                {
                    return EndOperate(false, $"存在 {FailMessage.Count} 失败信息，{FailMessage.ToJson()}", FailMessage, consoleOutput: false);
                }
                else
                {
                    return EndOperate(true);
                }
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 消费
        /// </summary>
        /// <param name="Topic">主题</param>
        /// <param name="ISns">实例唯一标识符为空则执行所有</param>
        /// <returns>统一结果</returns>
        public OperateResult Consume(string Topic, List<string>? ISns = null)
        {
            BegOperate();
            try
            {
                List<string> FailMessage = new List<string>();
                if (ISns != null)
                {
                    foreach (var sn in ISns)
                    {
                        if (InstanceIoc.ContainsKey(sn))
                        {
                            if (!InstanceIoc[sn].Consume(Topic).GetDetails(out string? message))
                            {
                                FailMessage.Add(message);
                            }
                        }
                        else
                        {
                            FailMessage.Add($"未找到 {sn} 的实例");
                        }
                    }
                }
                else
                {
                    foreach (var item in InstanceIoc)
                    {
                        if (!item.Value.Consume(Topic).GetDetails(out string? message))
                        {
                            FailMessage.Add(message);
                        }
                    }
                }
                if (FailMessage.Count > 0)
                {
                    return EndOperate(false, $"存在 {FailMessage.Count} 失败信息", FailMessage);
                }
                else
                {
                    return EndOperate(true);
                }
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 移除消费
        /// </summary>
        /// <param name="Topic">主题</param>
        /// <param name="ISns">实例唯一标识符为空则执行所有</param>
        /// <returns>统一结果</returns>
        public OperateResult UnConsume(string Topic, List<string>? ISns = null)
        {
            BegOperate();
            try
            {
                List<string> FailMessage = new List<string>();
                if (ISns != null)
                {
                    foreach (var sn in ISns)
                    {
                        if (InstanceIoc.ContainsKey(sn))
                        {
                            if (!InstanceIoc[sn].UnConsume(Topic).GetDetails(out string? message))
                            {
                                FailMessage.Add(message);
                            }
                        }
                        else
                        {
                            FailMessage.Add($"未找到 {sn} 的实例");
                        }
                    }
                }
                else
                {
                    foreach (var item in InstanceIoc)
                    {
                        if (!item.Value.UnConsume(Topic).GetDetails(out string? message))
                        {
                            FailMessage.Add(message);
                        }
                    }
                }
                if (FailMessage.Count > 0)
                {
                    return EndOperate(false, $"存在 {FailMessage.Count} 失败信息", FailMessage);
                }
                else
                {
                    return EndOperate(true);
                }
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }
    }
}
