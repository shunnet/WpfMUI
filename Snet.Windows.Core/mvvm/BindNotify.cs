using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Snet.Windows.Core.mvvm
{
    /// <summary>
    /// 绑定通知对象基类，基于 ObservableObject<br/>
    /// 提供基于表达式的动态属性存储机制，无需手动定义字段。
    /// </summary>
    public class BindNotify : ObservableObject
    {
        #region 字典属性包

        private Dictionary<string, object> _propertyBag;

        /// <summary>
        /// 延迟初始化的属性包，用于动态存储所有属性值。
        /// </summary>
        private Dictionary<string, object> PropertyBag => _propertyBag ??= new Dictionary<string, object>();

        #endregion

        #region 属性访问核心方法

        /// <summary>
        /// 获取指定属性的值。
        /// </summary>
        private T GetPropertyCore<T>(string propertyName)
        {
            if (PropertyBag.TryGetValue(propertyName, out object? val))
            {
                return (T)val;
            }
            return default!;
        }

        /// <summary>
        /// 设置属性值，并通知属性变更。
        /// </summary>
        protected virtual bool SetPropertyCore<T>(string propertyName, T value, out T oldValue)
        {
            VerifyAccess();

            oldValue = default!;
            if (PropertyBag.TryGetValue(propertyName, out object? val))
            {
                oldValue = (T)val;
            }

            if (EqualityComparer<T>.Default.Equals(oldValue, value))
                return false;

            lock (PropertyBag)
            {
                PropertyBag[propertyName] = value!;
            }

            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 带回调的 SetPropertyCore（无参数）。
        /// </summary>
        private bool SetPropertyCore<T>(string propertyName, T value, Action changedCallback)
        {
            var result = SetPropertyCore(propertyName, value, out _);
            if (result)
                changedCallback?.Invoke();
            return result;
        }

        /// <summary>
        /// 带回调的 SetPropertyCore（旧值作为参数）。
        /// </summary>
        private bool SetPropertyCore<T>(string propertyName, T value, Action<T> changedCallback)
        {
            var result = SetPropertyCore(propertyName, value, out T oldValue);
            if (result)
                changedCallback?.Invoke(oldValue);
            return result;
        }

        /// <summary>
        /// 可重写的访问验证逻辑（默认无操作）。
        /// </summary>
        protected virtual void VerifyAccess() { }

        #endregion

        #region 表达式支持

        /// <summary>
        /// 获取表达式表示的属性名称。
        /// </summary>
        private static string GetPropertyName<T>(Expression<Func<T>> expression)
        {
            return GetPropertyNameFast(expression);
        }

        /// <summary>
        /// 从 Lambda 表达式中提取属性名（内部快速实现）。
        /// </summary>
        private static string GetPropertyNameFast(LambdaExpression expression)
        {
            if (expression.Body is not MemberExpression memberExpression)
            {
                throw new ArgumentException("表达式体应为成员访问表达式", nameof(expression));
            }

            MemberInfo member = memberExpression.Member;
            const string VbLocalPrefix = "$VB$Local_";

            // 修正 VB.NET 编译器局部变量前缀
            if (member.MemberType == MemberTypes.Field &&
                member.Name != null &&
                member.Name.StartsWith(VbLocalPrefix))
            {
                return member.Name.Substring(VbLocalPrefix.Length);
            }

            return member.Name;
        }

        #endregion

        #region 封装的属性访问方法（推荐调用方式）

        /// <summary>
        /// 获取属性值。
        /// </summary>
        protected T GetProperty<T>(Expression<Func<T>> expression)
        {
            return GetPropertyCore<T>(GetPropertyName(expression));
        }
        /// <summary>
        /// 设置属性值，仅触发属性更改通知。
        /// </summary>
        protected bool SetProperty<T>(Expression<Func<T>> expression, T value)
        {
            return SetProperty(expression, value, changedCallback: (Action?)null);
        }

        /// <summary>
        /// 设置属性值，带旧值回调。
        /// </summary>
        protected bool SetProperty<T>(Expression<Func<T>> expression, T value, Action<T> changedCallback)
        {
            return SetPropertyCore(GetPropertyName(expression), value, changedCallback);
        }

        /// <summary>
        /// 设置属性值，带回调。
        /// </summary>
        protected bool SetProperty<T>(Expression<Func<T>> expression, T value, Action changedCallback)
        {
            return SetPropertyCore(GetPropertyName(expression), value, changedCallback);
        }
        #endregion
    }
}