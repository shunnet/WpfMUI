//using Microsoft.Extensions.DependencyInjection;
//using Snet.Utility;
//using System.Collections.Concurrent;

//namespace Snet.Windows.Core.handler
//{
//    /// <summary>
//    /// 注入 Winform<br/>
//    /// 请设置 《UseWindowsForms》true《/UseWindowsForms》
//    /// </summary>
//    public class InjectionWinform : InjectionHandler
//    {
//        /// <summary>
//        /// 窗口缓存
//        /// </summary>
//        private static readonly ConcurrentDictionary<Type, Form> WindowCache = new();

//        /// <summary>
//        /// 用户控件缓存
//        /// </summary>
//        private static readonly ConcurrentDictionary<Type, UserControl> UserControlCache = new();

//        /// <summary>
//        /// 注入窗口
//        /// </summary>
//        /// <typeparam name="T">
//        /// 对象类型<br/>
//        /// System.Windows.Forms.Form
//        /// </typeparam>
//        /// <param name="cache">
//        /// 缓存<br/>
//        /// 注入的对象是否需要设置缓存，如果不缓存则返回新的实例
//        /// </param>
//        /// <returns>对应的实例</returns>
//        public static T Window<T>(bool cache = false)
//            where T : Form
//        {
//            try
//            {
//                //注入，已注入则不重复注入，当未注入时在注入
//                if (cache)
//                {
//                    if (!ExistService<T>())
//                    {
//                        AddService(s => s.AddSingleton<T>());
//                    }
//                }
//                else
//                {
//                    if (!ExistService<T>())
//                    {
//                        AddService(s => s.AddTransient<T>());
//                    }
//                }

//                //从缓存中获取
//                if (cache && WindowCache.TryGetValue(typeof(T), out var cacheData))
//                {
//                    return (T)cacheData;
//                }

//                // 使用依赖注入创建实例
//                var instance = ActivatorUtilities.CreateInstance<T>(GetProvider());

//                // 说明是第一次缓存，则设置缓存
//                if (cache)
//                {
//                    // 设置缓存
//                    WindowCache[typeof(T)] = instance;
//                }

//                //返回新的实例
//                return instance;
//            }
//            catch (Exception ex)
//            {
//                throw new Exception($"注入窗口异常：{ex.Message}", ex);
//            }
//        }

//        /// <summary>
//        /// 注入用户控件
//        /// </summary>
//        /// <typeparam name="T">
//        /// 对象类型<br/>
//        /// System.Windows.Forms.UserControl
//        /// </typeparam>
//        /// <param name="cache">
//        /// 缓存<br/>
//        /// 注入的对象是否需要设置缓存，如果不缓存则返回新的实例
//        /// </param>
//        /// <returns>对应的实例</returns>
//        public static T UserControl<T>(bool cache = false)
//            where T : UserControl
//        {
//            try
//            {
//                //注入两个对象，已注入则不重复注入，当未注入时在注入
//                if (cache)
//                {
//                    if (!ExistService<T>())
//                    {
//                        AddService(s => s.AddSingleton<T>());
//                    }
//                }
//                else
//                {
//                    if (!ExistService<T>())
//                    {
//                        AddService(s => s.AddTransient<T>());
//                    }
//                }

//                //从缓存中获取
//                if (cache && UserControlCache.TryGetValue(typeof(T), out var cacheData))
//                {
//                    return (T)cacheData;
//                }

//                // 使用依赖注入创建实例
//                var instance = ActivatorUtilities.CreateInstance<T>(GetProvider());

//                // 说明是第一次缓存，则设置缓存
//                if (cache)
//                {
//                    // 设置缓存
//                    UserControlCache[typeof(T)] = instance;
//                }

//                //返回新的实例
//                return instance;
//            }
//            catch (Exception ex)
//            {
//                throw new Exception($"注入用户控件异常：{ex.Message}", ex);
//            }
//        }
//    }
//}