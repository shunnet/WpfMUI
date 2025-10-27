using System.Diagnostics;
using System.Linq.Expressions;

namespace Snet.Windows.Controls.ledgauge
{
    public static class GenericOperator<T, U, V>
    {
        private static readonly T zero;

        private static readonly Func<T, T> increment;

        private static readonly Func<T, T> decrement;

        private static readonly Func<T, U, V> add;

        private static readonly Func<T, U, V> subtract;

        private static readonly Func<T, U, V> multiply;

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
