using Snet.Model.data;
using Snet.Model.@enum;
using Snet.Unility;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Snet.Core.handler
{
    /// <summary>
    /// 语言处理
    /// </summary>
    public static class LanguageHandler
    {
        /// <summary>
        /// 语言传递事件
        /// </summary>
        public static event EventHandler<EventLanguageResult> OnLanguageEvent;
        /// <summary>
        /// 语言传递事件异步
        /// </summary>
        public static event EventHandlerAsync<EventLanguageResult> OnLanguageEventAsync
        {
            add => OnLanguageEventWrapperAsync.AddHandler(value);
            remove => OnLanguageEventWrapperAsync.RemoveHandler(value);
        }
        /// <summary>
        /// 包装器异步
        /// </summary>
        private static EventingWrapperAsync<EventLanguageResult> OnLanguageEventWrapperAsync;
        /// <summary>
        /// 语言切换传递
        /// </summary>
        /// <param name="sender">自身对象</param>
        /// <param name="e">事件结果</param>
        private static void OnLanguageEventHandler(object? sender, EventLanguageResult e)
        {
            OnLanguageEvent?.Invoke(sender, e);
            OnLanguageEventWrapperAsync.InvokeAsync(sender, e);
        }

        /// <summary>
        /// 语言信息<br/>
        /// 默认中文
        /// </summary>
        private static CultureInfo cultureInfo = new CultureInfo("zh");

        /// <summary>
        /// 资源管理<br/>
        /// 存放已经实例的资源<br/>
        /// 提升速度,避免每次都要实例化
        /// </summary>
        private static ConcurrentDictionary<string, ResourceManager>? resourceManager;
        /// <summary>
        /// 语言管理<br/>
        /// 存放已经实例的语言<br/>
        /// 提升速度,避免每次都要实例化
        /// </summary>
        private static ConcurrentDictionary<string, CultureInfo>? languageManager;

        /// <summary>
        /// 内部默认语言模型
        /// </summary>
        /// <returns>语言模型</returns>
        private static LanguageModel internalLanguageModel { get; set; } = new("Snet.Core", "Language", "Snet.Core.dll");

        /// <summary>
        /// 根据关键字获取当前语言环境下的对应的键值信息
        /// </summary>
        /// <param name="key">关键字</param>
        /// <param name="languageModel">
        /// 语言模型<br/>
        /// 存放基础数据，指向对应资源<br/>
        /// 如果外部自定义了语言资源<br/>
        /// 此属性"必传"<br/>
        /// 否则获取对应的键值<br/>
        /// 也可使用"LanguageOperate"属性来进行根据关键字获取当前语言环境下的对应的键值信息<br/>
        /// 为空将认定为是内部操作
        /// </param>
        /// <returns>对应语言的值</returns>
        public static string? GetLanguageValue(this string key, LanguageModel? languageModel = null)
        {
            //当语言模型为空则使用默认的语言模型
            languageModel ??= internalLanguageModel;

            string baseName = $"{languageModel.Source}.{languageModel.Dictionary}";
            // 构建资源管理器
            ResourceManager resource;
            if (resourceManager == null)
            {
                resourceManager = new ConcurrentDictionary<string, ResourceManager>();
            }
            if (resourceManager.TryGetValue(baseName, out ResourceManager? manager))
            {
                resource = manager;
            }
            else
            {
                Assembly assembly;
                if (!languageModel.AssemblyFile.IsNullOrWhiteSpace())
                {
                    if (File.Exists(languageModel.AssemblyFile))
                    {
                        assembly = Assembly.LoadFrom(languageModel.AssemblyFile);
                    }
                    else
                    {
                        assembly = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, languageModel.AssemblyFile));
                    }
                }
                else
                {
                    assembly = Assembly.GetCallingAssembly();
                }
                resource = new ResourceManager(baseName, assembly);
                resourceManager.TryAdd(baseName, resource);
            }
            // 从资源管理器中获取指定关键字的值
            return resource.GetString(key, cultureInfo);
        }
        /// <summary>
        /// 获取当前使用的语言
        /// </summary>
        /// <returns>返回语言类型</returns>
        public static LanguageType GetLanguage() => cultureInfo.TwoLetterISOLanguageName == "zh" ? LanguageType.zh : LanguageType.en;
        /// <summary>
        /// 设置语言
        /// </summary>
        /// <param name="language">语言类型</param>
        /// <returns>成功与失败</returns>
        public static bool SetLanguage(this LanguageType language)
        {
            //事件语言结果
            EventLanguageResult? result = null;
            try
            {
                if (languageManager == null)
                {
                    languageManager = new ConcurrentDictionary<string, CultureInfo>();
                }
                if (languageManager.TryGetValue(language.ToString(), out CultureInfo? culture))
                {
                    cultureInfo = culture;
                }
                else
                {
                    cultureInfo = new CultureInfo(language.ToString());
                    languageManager.TryAdd(language.ToString(), cultureInfo);
                }

                // 设置默认文化信息（影响整个 AppDomain 中新线程）
                CultureInfo.DefaultThreadCurrentCulture = cultureInfo;         // 影响数字、日期、货币等格式
                CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;       // 影响资源文件（.resx）语言加载

                // 设置当前线程文化信息（影响当前线程立即生效）
                Thread.CurrentThread.CurrentCulture = cultureInfo;             // 当前线程的数字/日期格式
                Thread.CurrentThread.CurrentUICulture = cultureInfo;           // 当前线程的语言资源加载

                // 显式设置全局静态 CurrentCulture（某些框架兼容性考虑）
                CultureInfo.CurrentCulture = cultureInfo;
                CultureInfo.CurrentUICulture = cultureInfo;

                //只要设置了语言就告诉外部
                result = EventLanguageResult.CreateSuccessResult(GetLanguageValue("语言设置成功"), language);
            }
            catch (CultureNotFoundException ex)
            {
                result = EventLanguageResult.CreateFailureResult($"{GetLanguageValue("找不到指定语言")}: {ex.Message}", language);
            }
            catch (MissingManifestResourceException ex)
            {
                result = EventLanguageResult.CreateFailureResult($"{GetLanguageValue("找不到资源文件")}: {ex.Message}", language);
            }
            catch (Exception ex)
            {
                result = EventLanguageResult.CreateFailureResult($"{GetLanguageValue("语言设置异常")}: {ex.Message}", language);
            }
            OnLanguageEventHandler(null, result);
            return result.Status;
        }

        /// <summary>
        /// 根据关键字获取当前语言环境下的对应的键值信息异步
        /// </summary>
        /// <param name="key">关键字</param>
        /// <param name="languageModel">
        /// 语言模型<br/>
        /// 存放基础数据，指向对应资源<br/>
        /// 如果外部自定义了语言资源<br/>
        /// 此属性"必传"<br/>
        /// 否则获取对应的键值<br/>
        /// 也可使用"LanguageOperate"属性来进行根据关键字获取当前语言环境下的对应的键值信息<br/>
        /// 为空将认定为是内部操作
        /// </param>
        /// <param name="token">传播应取消操作的通知</param>
        /// <returns>对应语言的值</returns>
        public static async Task<string?> GetLanguageValueAsync(this string key, LanguageModel? languageModel = null, CancellationToken token = default)
            => await Task.Run(() => GetLanguageValue(key, languageModel), token);
        /// <summary>
        /// 获取当前使用的语言异步
        /// </summary>
        /// <param name="token">传播应取消操作的通知</param>
        /// <returns>返回语言类型</returns>
        public static async Task<LanguageType> GetLanguageAsync(CancellationToken token = default)
            => await Task.Run(() => GetLanguage(), token);
        /// <summary>
        /// 设置语言异步
        /// </summary>
        /// <param name="language">语言类型</param>
        /// <param name="token">传播应取消操作的通知</param>
        /// <returns>成功与失败</returns>
        public static async Task<bool> SetLanguageAsync(this LanguageType language, CancellationToken token = default)
            => await Task.Run(() => SetLanguage(language), token);

        /// <summary>
        /// 根据关键字获取当前语言环境下的提示信息
        /// </summary>
        /// <param name="model">语言模型<br/>存放基础数据，指向指定资源</param>
        /// <param name="key">关键字</param>
        /// <returns>对应语言的值</returns>
        public static string? GetLanguageValue<T>(this T model, string key) where T : LanguageModel, new()
            => GetLanguageValue(key, model);

        /// <summary>
        /// 根据关键字获取当前语言环境下的提示信息异步
        /// </summary>
        /// <param name="model">语言模型<br/>存放基础数据，指向指定资源</param>
        /// <param name="key">关键字</param>
        /// <param name="token">传播应取消操作的通知</param>
        /// <returns>对应语言的值</returns>
        public static async Task<string?> GetLanguageValueAsync<T>(this T model, string key, CancellationToken token = default) where T : LanguageModel, new()
            => await GetLanguageValueAsync(key, model, token);
    }
}
