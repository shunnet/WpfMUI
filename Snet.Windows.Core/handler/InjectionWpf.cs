using Microsoft.Extensions.DependencyInjection;
using Snet.Core;
using System.Collections.Concurrent;

namespace Snet.Windows.Core.handler
{
    /// <summary>
    /// WPF MVVM 依赖注入容器，支持 Window、UserControl、Page 的视图-视图模型自动绑定。<br/>
    /// 使用 ConcurrentDictionary 缓存已创建的实例（按 Type.GUID 索引），<br/>
    /// 支持缓存模式（Singleton）和瞬态模式（Transient）两种注入方式。<br/>
    /// 请确保项目文件中已设置 &lt;UseWPF&gt;true&lt;/UseWPF&gt;。
    /// </summary>
    public class InjectionWpf : InjectionHandler
    {
        /// <summary>
        /// 窗口缓存（使用 Type 作为 Key，引用比较 O(1)，比 GUID 更高效）
        /// </summary>
        private static readonly ConcurrentDictionary<Type, System.Windows.Window> WindowCache = new();

        /// <summary>
        /// 页面缓存（使用 Type 作为 Key，引用比较 O(1)，比 GUID 更高效）
        /// </summary>
        private static readonly ConcurrentDictionary<Type, System.Windows.Controls.Page> PageCache = new();

        /// <summary>
        /// 用户控件缓存（使用 Type 作为 Key，引用比较 O(1)，比 GUID 更高效）
        /// </summary>
        private static readonly ConcurrentDictionary<Type, System.Windows.Controls.UserControl> UserControlCache = new();

        /// <summary>
        /// 注入窗口
        /// </summary>
        /// <typeparam name="T">
        /// 对象类型<br/>
        /// System.Windows.Window
        /// </typeparam>
        /// <typeparam name="M">视图</typeparam>
        /// <param name="cache">
        /// 缓存<br/>
        /// 注入的对象是否需要设置缓存，如果不缓存则返回新的实例
        /// </param>
        /// <returns>对应的实例</returns>
        public static T Window<T, M>(bool cache = false) where T : System.Windows.Window where M : class
        {
            return WindowAsync<T, M>(cache).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 注入窗口
        /// </summary>
        /// <typeparam name="T">
        /// 对象类型<br/>
        /// System.Windows.Window
        /// </typeparam>
        /// <typeparam name="M">视图</typeparam>
        /// <param name="cache">
        /// 缓存<br/>
        /// 注入的对象是否需要设置缓存，如果不缓存则返回新的实例
        /// </param>
        /// <param name="token">取消通知</param>
        /// <returns>对应的实例</returns>
        public static async Task<T> WindowAsync<T, M>(bool cache = false, CancellationToken token = default) where T : System.Windows.Window where M : class
        {
            try
            {
                //注入两个对象，已注入则不重复注入，当未注入时在注入
                if (cache)
                {
                    if (!ExistService<T>())
                    {
                        await AddServiceAsync(s => s.AddSingleton<T>(), token);
                    }
                    if (!ExistService<M>())
                    {
                        await AddServiceAsync(s => s.AddSingleton<M>(), token);
                    }
                }
                else
                {
                    if (!ExistService<T>())
                    {
                        await AddServiceAsync(s => s.AddTransient<T>(), token);
                    }
                    if (!ExistService<M>())
                    {
                        await AddServiceAsync(s => s.AddTransient<M>(), token);
                    }
                }

                //从缓存中获取
                if (cache && WindowCache.TryGetValue(typeof(T), out var cacheData))
                {
                    return (T)cacheData;
                }

                ServiceProvider? serviceProvider = GetProvider();
                if (serviceProvider == null)
                {
                    throw new Exception("注入失败，服务提供器为空");
                }

                // 获取视图模型
                var viewModel = ActivatorUtilities.CreateInstance<M>(serviceProvider);

                // 使用依赖注入创建实例
                var instance = ActivatorUtilities.CreateInstance<T>(serviceProvider);
                instance.DataContext = viewModel;

                // 说明是第一次缓存，则设置缓存
                if (cache)
                {
                    // 设置缓存
                    WindowCache[typeof(T)] = instance;
                    //覆盖之前的注入
                    await AddServiceAsync(s => s.AddSingleton<T>(instance), token);
                    await AddServiceAsync(s => s.AddSingleton<M>(viewModel), token);
                }

                //返回新的实例
                return instance;
            }
            catch (Exception ex)
            {
                throw new Exception($"注入窗口异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 注入用户控件
        /// </summary>
        /// <typeparam name="T">
        /// 对象类型<br/>
        /// System.Windows.Controls.UserControl
        /// </typeparam>
        /// <typeparam name="M">视图</typeparam>
        /// <param name="cache">
        /// 缓存<br/>
        /// 注入的对象是否需要设置缓存，如果不缓存则返回新的实例
        /// </param>
        /// <returns>对应的实例</returns>
        public static T UserControl<T, M>(bool cache = false) where T : System.Windows.Controls.UserControl where M : class
        {
            return UserControlAsync<T, M>(cache).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 注入用户控件
        /// </summary>
        /// <typeparam name="T">
        /// 对象类型<br/>
        /// System.Windows.Controls.UserControl
        /// </typeparam>
        /// <typeparam name="M">视图</typeparam>
        /// <param name="cache">
        /// 缓存<br/>
        /// 注入的对象是否需要设置缓存，如果不缓存则返回新的实例
        /// </param>
        /// <param name="token">取消通知</param>
        /// <returns>对应的实例</returns>
        public static async Task<T> UserControlAsync<T, M>(bool cache = false, CancellationToken token = default) where T : System.Windows.Controls.UserControl where M : class
        {
            try
            {
                //注入两个对象，已注入则不重复注入，当未注入时在注入
                if (cache)
                {
                    if (!ExistService<T>())
                    {
                        await AddServiceAsync(s => s.AddSingleton<T>(), token);
                    }
                    if (!ExistService<M>())
                    {
                        await AddServiceAsync(s => s.AddSingleton<M>(), token);
                    }
                }
                else
                {
                    if (!ExistService<T>())
                    {
                        await AddServiceAsync(s => s.AddTransient<T>(), token);
                    }
                    if (!ExistService<M>())
                    {
                        await AddServiceAsync(s => s.AddTransient<M>(), token);
                    }
                }

                //从缓存中获取
                if (cache && UserControlCache.TryGetValue(typeof(T), out var cacheData))
                {
                    return (T)cacheData;
                }

                ServiceProvider? serviceProvider = GetProvider();
                if (serviceProvider == null)
                {
                    throw new Exception("注入失败，服务提供器为空");
                }

                // 获取视图模型
                var viewModel = ActivatorUtilities.CreateInstance<M>(serviceProvider);

                // 使用依赖注入创建实例
                var instance = ActivatorUtilities.CreateInstance<T>(serviceProvider);
                instance.DataContext = viewModel;

                // 说明是第一次缓存，则设置缓存
                if (cache)
                {
                    // 设置缓存
                    UserControlCache[typeof(T)] = instance;
                    //覆盖之前的注入
                    await AddServiceAsync(s => s.AddSingleton<T>(instance), token);
                    await AddServiceAsync(s => s.AddSingleton<M>(viewModel), token);
                }

                //返回新的实例
                return instance;
            }
            catch (Exception ex)
            {
                throw new Exception($"注入用户控件异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 注入页面
        /// </summary>
        /// <typeparam name="T">
        /// 对象类型<br/>
        /// System.Windows.Controls.Page
        /// </typeparam>
        /// <typeparam name="M">视图</typeparam>
        /// <param name="cache">
        /// 缓存<br/>
        /// 注入的对象是否需要设置缓存，如果不缓存则返回新的实例
        /// </param>
        /// <returns>对应的实例</returns>
        public static T Page<T, M>(bool cache = true) where T : System.Windows.Controls.Page where M : class
        {
            return PageAsync<T, M>(cache).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 注入页面
        /// </summary>
        /// <typeparam name="T">
        /// 对象类型<br/>
        /// System.Windows.Controls.Page
        /// </typeparam>
        /// <typeparam name="M">视图</typeparam>
        /// <param name="cache">
        /// 缓存<br/>
        /// 注入的对象是否需要设置缓存，如果不缓存则返回新的实例
        /// </param>
        /// <param name="token">取消通知</param>
        /// <returns>对应的实例</returns>
        public static async Task<T> PageAsync<T, M>(bool cache = true, CancellationToken token = default) where T : System.Windows.Controls.Page where M : class
        {
            try
            {
                //注入两个对象，已注入则不重复注入，当未注入时在注入
                if (cache)
                {
                    if (!ExistService<T>())
                    {
                        await AddServiceAsync(s => s.AddSingleton<T>(), token);
                    }
                    if (!ExistService<M>())
                    {
                        await AddServiceAsync(s => s.AddSingleton<M>(), token);
                    }
                }
                else
                {
                    if (!ExistService<T>())
                    {
                        await AddServiceAsync(s => s.AddTransient<T>(), token);
                    }
                    if (!ExistService<M>())
                    {
                        await AddServiceAsync(s => s.AddTransient<M>(), token);
                    }
                }

                //从缓存中获取
                if (cache && PageCache.TryGetValue(typeof(T), out var cacheData))
                {
                    return (T)cacheData;
                }

                ServiceProvider? serviceProvider = GetProvider();
                if (serviceProvider == null)
                {
                    throw new Exception("注入失败，服务提供器为空");
                }

                // 获取视图模型
                var viewModel = ActivatorUtilities.CreateInstance<M>(serviceProvider);

                // 使用依赖注入创建实例
                var instance = ActivatorUtilities.CreateInstance<T>(serviceProvider);
                instance.DataContext = viewModel;

                // 说明是第一次缓存，则设置缓存
                if (cache)
                {
                    // 设置缓存
                    PageCache[typeof(T)] = instance;
                    //覆盖之前的注入
                    await AddServiceAsync(s => s.AddSingleton<T>(instance), token);
                    await AddServiceAsync(s => s.AddSingleton<M>(viewModel), token);
                }

                //返回新的实例
                return instance;
            }
            catch (Exception ex)
            {
                throw new Exception($"注入页面异常：{ex.Message}", ex);
            }
        }
    }
}