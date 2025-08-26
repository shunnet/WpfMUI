using Snet.Core.handler;
using Snet.Model.@enum;
using Snet.Unility;
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
        /// <summary>
        /// 静态构造函数<br/>
        /// 实例化时自动获取当前语言并设置<br/>
        /// 为了让底层知道当前使用的什么语言
        /// </summary>
        static LanguageHandler()
        {
            SetLanguage(GetLanguage());
        }

        #region 常量与字段

        /// <summary>
        /// 语言配置文件路径
        /// </summary>
        private static string path_language = Path.Combine(WindowHandler.BasePath, "language.json");

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
        public static string? GetLanguageValue(string key, Snet.Model.data.LanguageModel? languageModel = null)
            => Snet.Core.handler.LanguageHandler.GetLanguageValue(key, languageModel);

        /// <summary>
        /// 根据关键字获取翻译文本（异步）
        /// </summary>
        /// <param name="key">翻译关键字</param>
        /// <param name="languageModel">语言模型，可选</param>
        /// <param name="token">取消令牌</param>
        /// <returns>翻译内容，如果不存在返回 null</returns>
        public static Task<string?> GetLanguageValueAsync(string key, Snet.Model.data.LanguageModel? languageModel = null, CancellationToken token = default)
            => Snet.Core.handler.LanguageHandler.GetLanguageValueAsync(key, languageModel, token);

        #endregion

        #region 语言配置获取与设置

        /// <summary>
        /// 获取当前系统设置语言（读取配置文件）
        /// </summary>
        /// <returns>语言类型</returns>
        public static LanguageType GetLanguage()
        {
            try
            {
                if (!File.Exists(path_language))
                {
                    // 默认保存当前语言
                    var currentLang = Snet.Core.handler.LanguageHandler.GetLanguage();
                    File.WriteAllText(path_language, new UseLanguageModel(currentLang).ToJson());
                    return currentLang;
                }

                // 读取并反序列化
                return File.ReadAllText(path_language).ToJsonEntity<UseLanguageModel>().LanguageType;
            }
            catch
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

            // 通知核心语言模块更新语言
            languageType.SetLanguage();

            // 确保路径存在
            if (!Directory.Exists(WindowHandler.BasePath))
            {
                Directory.CreateDirectory(WindowHandler.BasePath);
            }

            // 保存配置到本地文件
            File.WriteAllText(path_language, new UseLanguageModel(languageType).ToJson());
        }

        #endregion
    }
}
