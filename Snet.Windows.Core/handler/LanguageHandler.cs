using Snet.Core.handler;
using Snet.Model.@enum;
using Snet.Utility;
using Snet.Windows.Core.data;
using System.Globalization;
using System.IO;

namespace Snet.Windows.Core.handler
{
    /// <summary>
    /// 多语言环境处理器，用于获取和设置当前语言环境及翻译信息
    /// </summary>
    public class LanguageHandler
    {
        #region 常量与字段

        /// <summary>
        /// 语言配置文件路径
        /// </summary>
        private static string path_language = Path.Combine(WindowHandler.BasePath, "language.json");

        /// <summary>
        /// 语言缓存访问锁，保护语言缓存字段的读写线程安全
        /// </summary>
        private static readonly object _languageCacheLock = new();

        /// <summary>
        /// 缓存的语言类型（null 表示尚未缓存）
        /// </summary>
        private static LanguageType? _cachedLanguage;

        /// <summary>
        /// 缓存时语言文件的最后写入时间（UTC），用于检测文件被外部修改
        /// </summary>
        private static DateTime _cachedLanguageFileTimeUtc;

        /// <summary>
        /// 缓存时语言文件是否存在
        /// </summary>
        private static bool _cachedLanguageFileExisted;

        #endregion

        #region 默认语言模型

        /// <summary>
        /// 获取默认语言模型（使用内置资源）
        /// </summary>
        public static Snet.Model.data.LanguageModel GetDefaultLanguageModel = new Snet.Model.data.LanguageModel("Snet.Windows.Controls", "Language", "Snet.Windows.Controls.dll");

        #endregion

        #region 翻译获取方法

        /// <summary>
        /// 根据关键字获取翻译文本（同步）
        /// </summary>
        /// <param name="key">翻译关键字</param>
        /// <param name="languageModel">语言模型，可选</param>
        /// <returns>翻译内容，如果不存在返回 null</returns>
        public static string? GetLanguageValue(string key, Snet.Model.data.LanguageModel? languageModel = null) => Snet.Core.handler.LanguageHandler.GetLanguageValue(key, languageModel);

        /// <summary>
        /// 根据关键字获取翻译文本（异步）
        /// </summary>
        /// <param name="key">翻译关键字</param>
        /// <param name="languageModel">语言模型，可选</param>
        /// <param name="token">取消令牌</param>
        /// <returns>翻译内容，如果不存在返回 null</returns>
        public static Task<string?> GetLanguageValueAsync(string key, Snet.Model.data.LanguageModel? languageModel = null, CancellationToken token = default) => Snet.Core.handler.LanguageHandler.GetLanguageValueAsync(key, languageModel, token);

        #endregion

        #region 语言配置获取与设置

        /// <summary>
        /// 获取当前系统设置语言（读取配置文件）
        /// </summary>
        /// <returns>语言类型</returns>
        public static LanguageType GetLanguage()
        {
            lock (_languageCacheLock)
            {
                // 缓存命中且文件未被外部修改时直接返回，避免重复读文件 + JSON 反序列化
                if (TryGetCachedLanguageUnsafe(out LanguageType cached))
                {
                    return cached;
                }

                try
                {
                    LanguageType result;
                    if (!File.Exists(path_language))
                    {
                        // 默认保存当前语言（文件不存在的情况同样写入缓存，避免反复读取）
                        Directory.CreateDirectory(WindowHandler.BasePath);
                        var currentLang = Snet.Core.handler.LanguageHandler.GetLanguage();
                        File.WriteAllText(path_language, new UseLanguageModel(currentLang).ToJson());
                        result = currentLang;
                    }
                    else
                    {
                        // 读取并反序列化
                        string str = File.ReadAllText(path_language);
                        result = str.ToJsonEntity<UseLanguageModel>()?.LanguageType ?? LanguageType.zh;
                    }

                    StoreLanguageCacheUnsafe(result);
                    return result;
                }
                catch
                {
                    // 出现异常时返回默认语言
                    return LanguageType.zh;
                }
            }
        }

        /// <summary>
        /// 获取当前使用的语言异步
        /// </summary>
        /// <param name="token">传播应取消操作的通知</param>
        /// <returns>返回语言类型</returns>
        public static async Task<LanguageType> GetLanguageAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            // 快速路径：缓存命中且文件未被外部修改时直接返回
            lock (_languageCacheLock)
            {
                if (TryGetCachedLanguageUnsafe(out LanguageType cached))
                {
                    return cached;
                }
            }

            try
            {
                LanguageType result;
                if (!File.Exists(path_language))
                {
                    // 默认保存当前语言
                    Directory.CreateDirectory(WindowHandler.BasePath);
                    var currentLang = await Snet.Core.handler.LanguageHandler.GetLanguageAsync(token);
                    await File.WriteAllTextAsync(path_language, new UseLanguageModel(currentLang).ToJson(), token);
                    result = currentLang;
                }
                else
                {
                    // 读取并反序列化
                    string str = await File.ReadAllTextAsync(path_language, token);
                    result = str.ToJsonEntity<UseLanguageModel>()?.LanguageType ?? LanguageType.zh;
                }

                UpdateLanguageCache(result);
                return result;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (IOException)
            {
                return LanguageType.zh;
            }
            catch (UnauthorizedAccessException)
            {
                return LanguageType.zh;
            }
            catch (System.Text.Json.JsonException)
            {
                // 出现异常时返回默认语言
                return LanguageType.zh;
            }
        }


        /// <summary>
        /// 设置当前系统语言（并保存配置文件）
        /// </summary>
        /// <param name="languageType">语言类型</param>
        public static void SetLanguage(LanguageType languageType)
        {
            // 设置当前线程文化
            Snet.Windows.Core.localize.wpf.Engine.LocalizeDictionary.Instance.Culture = CultureInfo.GetCultureInfo(languageType.ToString());

            // 确保路径存在
            if (!Directory.Exists(WindowHandler.BasePath))
            {
                Directory.CreateDirectory(WindowHandler.BasePath);
            }

            // 保存配置到本地文件（必须在语言切换事件之前：事件订阅者通过 GetLanguage() 读取语言文件，
            // 若文件最后写入会读到旧语言，导致属性框等界面显示滞后/错乱）
            UseLanguageModel model = new UseLanguageModel(languageType);
            File.WriteAllText(path_language, model.ToJson());

            // 写文件后同步更新缓存，避免下次 GetLanguage 重复读文件
            UpdateLanguageCache(languageType);

            // 最后通知核心语言模块更新语言（触发所有语言事件订阅者，此时文件与内存均已是新语言）
            languageType.SetLanguage();
        }

        /// <summary>
        /// 设置语言异步
        /// </summary>
        /// <param name="languageType">语言类型</param>
        /// <param name="token">传播应取消操作的通知</param>
        /// <returns>成功与失败</returns>
        public static async Task SetLanguageAsync(LanguageType languageType, CancellationToken token = default)
        {
            // 设置当前线程文化
            Snet.Windows.Core.localize.wpf.Engine.LocalizeDictionary.Instance.Culture = CultureInfo.GetCultureInfo(languageType.ToString());

            // 确保路径存在
            if (!Directory.Exists(WindowHandler.BasePath))
            {
                Directory.CreateDirectory(WindowHandler.BasePath);
            }

            // 保存配置到本地文件（必须在语言切换事件之前：事件订阅者通过 GetLanguage() 读取语言文件，
            // 若文件最后写入会读到旧语言，导致属性框等界面显示滞后/错乱）
            UseLanguageModel model = new UseLanguageModel(languageType);
            await File.WriteAllTextAsync(path_language, model.ToJson(), token);

            // 写文件后同步更新缓存，避免下次 GetLanguage 重复读文件
            UpdateLanguageCache(languageType);

            // 最后通知核心语言模块更新语言（触发所有语言事件订阅者，此时文件与内存均已是新语言）
            await languageType.SetLanguageAsync(token);
        }

        #region 语言缓存辅助方法

        /// <summary>
        /// 校验并读取缓存的语言类型（调用方需持有 _languageCacheLock）。<br/>
        /// 当缓存未初始化、或语言文件被外部修改/删除时返回 false，触发重新读取。
        /// </summary>
        private static bool TryGetCachedLanguageUnsafe(out LanguageType language)
        {
            language = default;
            if (_cachedLanguage is null)
            {
                return false;
            }

            bool fileExists = File.Exists(path_language);
            if (fileExists != _cachedLanguageFileExisted)
            {
                return false;
            }

            if (fileExists && File.GetLastWriteTimeUtc(path_language) != _cachedLanguageFileTimeUtc)
            {
                return false;
            }

            language = _cachedLanguage.Value;
            return true;
        }

        /// <summary>
        /// 将语言类型写入缓存并记录当前文件状态（调用方需持有 _languageCacheLock）
        /// </summary>
        private static void StoreLanguageCacheUnsafe(LanguageType languageType)
        {
            _cachedLanguage = languageType;
            _cachedLanguageFileExisted = File.Exists(path_language);
            _cachedLanguageFileTimeUtc = _cachedLanguageFileExisted ? File.GetLastWriteTimeUtc(path_language) : default;
        }

        /// <summary>
        /// 更新语言缓存（SetLanguage/SetLanguageAsync 写文件后调用，使缓存与文件保持一致）
        /// </summary>
        private static void UpdateLanguageCache(LanguageType languageType)
        {
            lock (_languageCacheLock)
            {
                StoreLanguageCacheUnsafe(languageType);
            }
        }

        #endregion

        #endregion
    }
}
