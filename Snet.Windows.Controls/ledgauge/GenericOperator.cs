using System.Linq.Expressions;

namespace Snet.Windows.Controls.ledgauge
{
    /// <summary>
    /// 泛型运算符辅助类，通过表达式树在编译时生成泛型数学运算委托。<br/>
    /// 支持加、减、乘、除、自增、自减等运算，适用于无法直接对泛型类型使用运算符的场景。<br/>
    /// 所有委托在静态构造函数中编译一次，后续调用无反射开销。
    /// </summary>
    /// <typeparam name="T">第一个操作数类型</typeparam>
    /// <typeparam name="U">第二个操作数类型</typeparam>
    /// <typeparam name="V">运算结果类型</typeparam>
    public static class GenericOperator<T, U, V>
    {
        /// <summary>类型 T 的零值（对于可空类型返回内部类型的默认值）</summary>
        private static readonly T zero;

        /// <summary>自增运算委托 (x => x + 1)</summary>
        private static readonly Func<T, T> increment;

        /// <summary>自减运算委托 (x => x - 1)</summary>
        private static readonly Func<T, T> decrement;

        /// <summary>加法运算委托 ((x, y) => x + y)</summary>
        private static readonly Func<T, U, V> add;

        /// <summary>减法运算委托 ((x, y) => x - y)</summary>
        private static readonly Func<T, U, V> subtract;

        /// <summary>乘法运算委托 ((x, y) => x * y)</summary>
        private static readonly Func<T, U, V> multiply;

        /// <summary>除法运算委托 ((x, y) => x / y)</summary>
        private static readonly Func<T, U, V> divide;

        public static T Zero
        {
            [DebuggerStepThrough]
            get
            {
                return zero;
            }
        }

        public static Func<T, U, V> Add
        {
            [DebuggerStepThrough]
            get
            {
                return add;
            }
        }

        public static Func<T, U, V> Subtract
        {
            [DebuggerStepThrough]
            get
            {
                return subtract;
            }
        }

        public static Func<T, U, V> Multiply
        {
            [DebuggerStepThrough]
            get
            {
                return multiply;
            }
        }

        public static Func<T, U, V> Divide
        {
            [DebuggerStepThrough]
            get
            {
                return divide;
            }
        }

        public static Func<T, T> Increment
        {
            [DebuggerStepThrough]
            get
            {
                return GenericOperator<T, T, T>.increment;
            }
        }

        public static Func<T, T> Decrement
        {
            [DebuggerStepThrough]
            get
            {
                return GenericOperator<T, T, T>.decrement;
            }
        }

        /// <summary>
        /// 静态构造函数，通过表达式树编译生成所有运算委托。<br/>
        /// 对于可空值类型，零值通过 Activator 创建内部类型的默认实例。
        /// </summary>
        static GenericOperator()
        {
            increment = MakeFunction<T>(Expression.Increment);
            decrement = MakeFunction<T>(Expression.Decrement);
            add = MakeFunction<T, U, V>(Expression.Add);
            subtract = MakeFunction<T, U, V>(Expression.Subtract);
            multiply = MakeFunction<T, U, V>(Expression.Multiply);
            divide = MakeFunction<T, U, V>(Expression.Divide);
            Type typeFromHandle = typeof(T);
            if (typeFromHandle.IsValueType && typeFromHandle.IsGenericType && typeFromHandle.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                zero = (T)Activator.CreateInstance(typeFromHandle.GetGenericArguments()[0]);
            }
            else
            {
                zero = default(T);
            }
        }

        /// <summary>
        /// 通过表达式树生成二元运算委托。<br/>
        /// 若类型不支持该运算，则返回一个在调用时抛出 InvalidOperationException 的委托。
        /// </summary>
        private static Func<X, Y, Z> MakeFunction<X, Y, Z>(Func<Expression, Expression, BinaryExpression> operation)
        {
            ParameterExpression parameterExpression = Expression.Parameter(typeof(X), "arg1");
            ParameterExpression parameterExpression2 = Expression.Parameter(typeof(Y), "arg2");
            try
            {
                return Expression.Lambda<Func<X, Y, Z>>(operation(parameterExpression, parameterExpression2), new ParameterExpression[2] { parameterExpression, parameterExpression2 }).Compile();
            }
            catch (Exception ex2)
            {
                Exception ex = ex2;
                return delegate
                {
                    throw new InvalidOperationException(ex.Message, ex);
                };
            }
        }

        /// <summary>
        /// 通过表达式树生成一元运算委托。<br/>
        /// 若类型不支持该运算，则返回一个在调用时抛出 InvalidOperationException 的委托。
        /// </summary>
        private static Func<X, X> MakeFunction<X>(Func<Expression, UnaryExpression> operation)
        {
            ParameterExpression parameterExpression = Expression.Parameter(typeof(X), "arg");
            try
            {
                return Expression.Lambda<Func<X, X>>(operation(parameterExpression), new ParameterExpression[1] { parameterExpression }).Compile();
            }
            catch (Exception ex2)
            {
                Exception ex = ex2;
                return delegate
                {
                    throw new InvalidOperationException(ex.Message, ex);
                };
            }
        }
    }
}
