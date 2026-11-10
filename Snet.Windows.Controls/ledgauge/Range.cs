using Newtonsoft.Json;

namespace Snet.Windows.Controls.ledgauge
{
    /// <summary>
    /// 泛型范围结构体，表示一个有最小值和最大值的闭区间 [Min, Max]。<br/>
    /// 支持范围宽度、中心值计算，以及值的限定（Clamp）和范围比较操作。<br/>
    /// 使用 GenericOperator 实现泛型数学运算，支持任意数值类型。
    /// </summary>
    /// <typeparam name="T">数值类型（必须为值类型且实现 IComparable、IEquatable 等接口）</typeparam>
    [JsonObject(MemberSerialization.OptIn)]
    [DebuggerDisplay("Range Min = {Min}, Max = {Max},  Width = {Width}, Center = {Center}")]
    public readonly struct Range<T> : IComparable<Range<T>>, IEquatable<Range<T>> where T : struct, IComparable, IFormattable, IConvertible, IComparable<T>, IEquatable<T>
    {
        private static readonly Range<T> empty = default(Range<T>);

        private static readonly T two = GenericOperator<T, T, T>.Increment(GenericOperator<T, T, T>.Increment(GenericOperator<T, T, T>.Zero));

        /// <summary>
        /// 区间下界字段（序列化为 Value1）。构造时保证 MinField &lt;= MaxField
        /// </summary>
        [JsonProperty(PropertyName = "Value1")]
        public readonly T MinField;

        /// <summary>
        /// 区间上界字段（序列化为 Value2）。构造时保证 MinField &lt;= MaxField
        /// </summary>
        [JsonProperty(PropertyName = "Value2")]
        public readonly T MaxField;

        /// <summary>
        /// 区间下界
        /// </summary>
        public T Min
        {
            [DebuggerStepThrough]
            get
            {
                return MinField;
            }
        }

        /// <summary>
        /// 区间上界
        /// </summary>
        public T Max
        {
            [DebuggerStepThrough]
            get
            {
                return MaxField;
            }
        }

        /// <summary>
        /// 区间宽度，等于 Max - Min
        /// </summary>
        public T Width
        {
            [DebuggerStepThrough]
            get
            {
                return GenericOperator<T, T, T>.Subtract(MaxField, MinField);
            }
        }

        /// <summary>
        /// 区间中心值，等于 Min + (Max - Min) / 2
        /// </summary>
        public T Center
        {
            [DebuggerStepThrough]
            get
            {
                T arg = GenericOperator<T, T, T>.Subtract(MaxField, MinField);
                T arg2 = GenericOperator<T, T, T>.Divide(arg, two);
                return GenericOperator<T, T, T>.Add(MinField, arg2);
            }
        }

        /// <summary>
        /// 任一端点为 NaN（与自身不相等）时为 true；仅对浮点类型可能为 true
        /// </summary>
        public bool IsNaN
        {
            [DebuggerStepThrough]
            get
            {
                T minField = MinField;
                if (minField.Equals(MinField))
                {
                    minField = MaxField;
                    return !minField.Equals(MaxField);
                }
                return true;
            }
        }

        /// <summary>
        /// 区间宽度为零（两端点相等）时为 true。注意零宽区间与 <see cref="Empty"/> 的行为一致：参与包含、相交判断时被当作空区间
        /// </summary>
        public bool IsEmpty
        {
            [DebuggerStepThrough]
            get
            {
                T minField = MinField;
                return minField.Equals(MaxField);
            }
        }

        /// <summary>
        /// 空区间（默认值，两端点均为该类型的默认值）
        /// </summary>
        public static Range<T> Empty
        {
            [DebuggerStepThrough]
            get
            {
                return empty;
            }
        }

        /// <summary>
        /// 由两个端点创建区间；若 value1 大于 value2 则自动交换，保证下界不大于上界
        /// </summary>
        [JsonConstructor]
        public Range(T value1, T value2)
        {
            if (value1.CompareTo(value2) <= 0)
            {
                MinField = value1;
                MaxField = value2;
            }
            else
            {
                MinField = value2;
                MaxField = value1;
            }
        }

        /// <summary>
        /// 闭区间判断：Start/Min &lt;= value &lt;= End/Max 时返回 true
        /// </summary>
        public bool Contains(T value)
        {
            if (value.CompareTo(MinField) >= 0)
            {
                return value.CompareTo(MaxField) <= 0;
            }
            return false;
        }

        /// <summary>
        /// 开区间判断：下界 &lt; value &lt; 上界 时返回 true
        /// </summary>
        public bool ContainsExclusive(T value)
        {
            if (value.CompareTo(MinField) > 0)
            {
                return value.CompareTo(MaxField) < 0;
            }
            return false;
        }

        /// <summary>
        /// 半开区间判断：下界 &lt;= value &lt; 上界 时返回 true
        /// </summary>
        public bool ContainsInclusiveExclusive(T value)
        {
            if (value.CompareTo(MinField) >= 0)
            {
                return value.CompareTo(MaxField) < 0;
            }
            return false;
        }

        /// <summary>
        /// 是否包含另一个区间（闭区间语义）；任一区间为空区间时返回 false
        /// </summary>
        public bool Contains(Range<T> range)
        {
            if (IsEmpty || range.IsEmpty)
            {
                return false;
            }
            T minField = range.MinField;
            if (minField.CompareTo(MinField) >= 0)
            {
                minField = range.MaxField;
                return minField.CompareTo(MaxField) <= 0;
            }
            return false;
        }

        /// <summary>
        /// 是否严格包含另一个区间（两端都不接触边界）；任一区间为空区间时返回 false
        /// </summary>
        public bool ContainsExclusive(Range<T> range)
        {
            if (IsEmpty || range.IsEmpty)
            {
                return false;
            }
            T minField = range.MinField;
            if (minField.CompareTo(MinField) > 0)
            {
                minField = range.MaxField;
                return minField.CompareTo(MaxField) < 0;
            }
            return false;
        }

        /// <summary>
        /// 把 value 限制在 [Min, Max] 内：小于下界返回下界，大于上界返回上界，否则原样返回
        /// </summary>
        public T Cap(T value)
        {
            if (value.CompareTo(MinField) < 0)
            {
                return MinField;
            }
            if (value.CompareTo(MaxField) > 0)
            {
                return MaxField;
            }
            return value;
        }

        /// <summary>
        /// 两端各向外扩展 extension，返回新区间
        /// </summary>
        public Range<T> ExtendBy(T extension)
        {
            T value = GenericOperator<T, T, T>.Subtract(MinField, extension);
            T value2 = GenericOperator<T, T, T>.Add(MaxField, extension);
            return new Range<T>(value, value2);
        }

        /// <summary>
        /// 下界向下扩展 lowerExtension、上界向上扩展 upperExtension，返回新区间
        /// </summary>
        public Range<T> ExtendBy(T lowerExtension, T upperExtension)
        {
            T value = GenericOperator<T, T, T>.Subtract(MinField, lowerExtension);
            T value2 = GenericOperator<T, T, T>.Add(MaxField, upperExtension);
            return new Range<T>(value, value2);
        }

        /// <summary>
        /// 与 range 有交集时返回两者覆盖范围的并集，没有交集时返回 <see cref="Empty"/>
        /// </summary>
        public Range<T> ExtendOverlapping(Range<T> range)
        {
            if (Intersect(this, range).IsEmpty)
            {
                return Empty;
            }
            T minField = MinField;
            T value = ((minField.CompareTo(range.MinField) > 0) ? range.MinField : MinField);
            minField = MaxField;
            T value2 = ((minField.CompareTo(range.MaxField) < 0) ? range.MaxField : MaxField);
            return new Range<T>(value, value2);
        }

        /// <summary>
        /// 与 range 是否相交（闭区间语义，端点接触也算相交）
        /// </summary>
        public bool IntersectsWith(Range<T> range)
        {
            return !Intersect(this, range).IsEmpty;
        }

        /// <summary>
        /// 与 range 是否相交（开区间语义，仅端点接触不算相交）
        /// </summary>
        public bool IntersectsExclusiveWith(Range<T> range)
        {
            return !IntersectExclusive(this, range).IsEmpty;
        }

        /// <summary>
        /// 返回与 range 的交集（闭区间语义）；不相交时返回 <see cref="Empty"/>
        /// </summary>
        public Range<T> GetIntersectionWith(Range<T> range)
        {
            return Intersect(this, range);
        }

        /// <summary>
        /// 返回与 range 的交集（开区间语义）；不相交时返回 <see cref="Empty"/>
        /// </summary>
        public Range<T> IntersectExclusive(Range<T> range)
        {
            return IntersectExclusive(this, range);
        }

        /// <summary>
        /// 返回覆盖两个区间的最小区间；其中一个为空区间时返回另一个
        /// </summary>
        public Range<T> Merge(Range<T> range)
        {
            return Merge(this, range);
        }

        /// <summary>
        /// 合并两个区间，等同于 <see cref="Merge(Range{T}, Range{T})"/>
        /// </summary>
        public static Range<T> operator +(Range<T> range1, Range<T> range2)
        {
            return Merge(range1, range2);
        }

        /// <summary>
        /// 求两个区间的交集，等同于 <see cref="Intersect(Range{T}, Range{T})"/>
        /// </summary>
        public static Range<T> operator *(Range<T> range1, Range<T> range2)
        {
            return Intersect(range1, range2);
        }

        /// <summary>
        /// 两个区间的上下界是否都相等
        /// </summary>
        public static bool operator ==(Range<T> range1, Range<T> range2)
        {
            return range1.Equals(range2);
        }

        /// <summary>
        /// 两个区间的上界或下界是否不相等
        /// </summary>
        public static bool operator !=(Range<T> range1, Range<T> range2)
        {
            return !range1.Equals(range2);
        }

        /// <summary>
        /// 求两个区间的交集（闭区间语义）；任一为空区间或不相交时返回 <see cref="Empty"/>
        /// </summary>
        public static Range<T> Intersect(Range<T> range1, Range<T> range2)
        {
            if (!range1.IsEmpty && !range2.IsEmpty)
            {
                T minField = range1.MinField;
                if (minField.CompareTo(range2.MaxField) <= 0)
                {
                    minField = range1.MaxField;
                    if (minField.CompareTo(range2.MinField) >= 0)
                    {
                        minField = range1.MinField;
                        T value = ((minField.CompareTo(range2.MinField) > 0) ? range1.MinField : range2.MinField);
                        minField = range1.MaxField;
                        T value2 = ((minField.CompareTo(range2.MaxField) < 0) ? range1.MaxField : range2.MaxField);
                        return new Range<T>(value, value2);
                    }
                }
            }
            return Empty;
        }

        /// <summary>
        /// 求两个区间的交集（开区间语义）；任一为空区间或不相交时返回 <see cref="Empty"/>
        /// </summary>
        public static Range<T> IntersectExclusive(Range<T> range1, Range<T> range2)
        {
            if (!range1.IsEmpty && !range2.IsEmpty)
            {
                T minField = range1.MinField;
                if (minField.CompareTo(range2.MaxField) < 0)
                {
                    minField = range1.MaxField;
                    if (minField.CompareTo(range2.MinField) > 0)
                    {
                        minField = range1.MinField;
                        T value = ((minField.CompareTo(range2.MinField) > 0) ? range1.MinField : range2.MinField);
                        minField = range1.MaxField;
                        T value2 = ((minField.CompareTo(range2.MaxField) < 0) ? range1.MaxField : range2.MaxField);
                        return new Range<T>(value, value2);
                    }
                }
            }
            return Empty;
        }

        /// <summary>
        /// 返回覆盖两个区间的最小区间；其中一个为空区间时返回另一个
        /// </summary>
        public static Range<T> Merge(Range<T> range1, Range<T> range2)
        {
            if (range1.IsEmpty)
            {
                return range2;
            }
            if (range2.IsEmpty)
            {
                return range1;
            }
            T minField = range1.MinField;
            T value = ((minField.CompareTo(range2.MinField) > 0) ? range2.MinField : range1.MinField);
            minField = range1.MaxField;
            T value2 = ((minField.CompareTo(range2.MaxField) < 0) ? range2.MaxField : range1.MaxField);
            return new Range<T>(value, value2);
        }

        /// <summary>
        /// 先比较下界、再比较上界；两者都相等时返回 0
        /// </summary>
        public int CompareTo(Range<T> other)
        {
            T minField = MinField;
            if (minField.Equals(other.MinField))
            {
                minField = MaxField;
                if (minField.Equals(other.MaxField))
                {
                    return 0;
                }
            }
            minField = MinField;
            int num = minField.CompareTo(other.MinField);
            if (num != 0)
            {
                return num;
            }
            minField = MaxField;
            return minField.CompareTo(other.MaxField);
        }

        /// <summary>
        /// 上下界是否都相等
        /// </summary>
        public bool Equals(Range<T> other)
        {
            T minField = MinField;
            if (minField.Equals(other.MinField))
            {
                minField = MaxField;
                return minField.Equals(other.MaxField);
            }
            return false;
        }

        /// <summary>
        /// obj 是同类型区间且上下界都相等时返回 true
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Range<T>)
            {
                return Equals((Range<T>)obj);
            }
            return false;
        }

        /// <summary>
        /// 由上下界组合出的哈希值
        /// </summary>
        public override int GetHashCode()
        {
            T minField = MinField;
            int num = (5993773 + minField.GetHashCode()) * 9973;
            minField = MaxField;
            return num + minField.GetHashCode();
        }
    }

    /// <summary>
    /// 泛型范围结构体，表示从 Start 到 End 的闭区间，端点类型与宽度类型可以不同（例如端点为 DateTime、宽度为 TimeSpan）。<br/>
    /// 提供与 <see cref="Range{T}"/> 对应的包含、相交、合并与比较操作。
    /// </summary>
    /// <typeparam name="T">端点（起点/终点）类型</typeparam>
    /// <typeparam name="U">宽度类型（端点之差的类型）</typeparam>
    [JsonObject(MemberSerialization.OptIn)]
    [DebuggerDisplay("Range Start = {Start}, End = {End},  Width = {Width}")]
    public readonly struct Range<T, U> : IComparable<Range<T, U>>, IEquatable<Range<T, U>> where T : struct, IComparable, IFormattable, IComparable<T>, IEquatable<T> where U : struct, IComparable, IFormattable, IComparable<U>, IEquatable<U>
    {
        private static readonly Range<T, U> empty = default;

        /// <summary>
        /// 区间起点字段（序列化为 Value1）。构造时保证 StartField &lt;= EndField（按 (start, width) 构造且 width 为负时除外）
        /// </summary>
        [JsonProperty(PropertyName = "Value1")]
        public readonly T StartField;

        /// <summary>
        /// 区间终点字段（序列化为 Value2）
        /// </summary>
        [JsonProperty(PropertyName = "Value2")]
        public readonly T EndField;

        /// <summary>
        /// 区间起点
        /// </summary>
        public T Start
        {
            [DebuggerStepThrough]
            get
            {
                return StartField;
            }
        }

        /// <summary>
        /// 区间终点
        /// </summary>
        public T End
        {
            [DebuggerStepThrough]
            get
            {
                return EndField;
            }
        }

        /// <summary>
        /// 区间宽度（U 类型），等于 End - Start
        /// </summary>
        public U Width
        {
            [DebuggerStepThrough]
            get
            {
                return GenericOperator<T, T, U>.Subtract(EndField, StartField);
            }
        }

        /// <summary>
        /// 区间宽度为零（两端点相等）时为 true。注意零宽区间与 <see cref="Empty"/> 的行为一致：参与包含、相交判断时被当作空区间
        /// </summary>
        public bool IsEmpty
        {
            [DebuggerStepThrough]
            get
            {
                T startField = StartField;
                return startField.Equals(EndField);
            }
        }

        /// <summary>
        /// 空区间（默认值，起点与终点均为该类型的默认值）
        /// </summary>
        public static Range<T, U> Empty
        {
            [DebuggerStepThrough]
            get
            {
                return empty;
            }
        }

        /// <summary>
        /// 由两个端点创建区间；若 value1 大于 value2 则自动交换，保证下界不大于上界
        /// </summary>
        [JsonConstructor]
        public Range(T value1, T value2)
        {
            if (value1.CompareTo(value2) <= 0)
            {
                StartField = value1;
                EndField = value2;
            }
            else
            {
                StartField = value2;
                EndField = value1;
            }
        }

        /// <summary>
        /// 由起点与宽度创建区间，终点为 start + width。注意：width 为负数时终点小于起点，不会像 (value1, value2) 构造函数那样自动交换
        /// </summary>
        public Range(T start, U width)
        {
            StartField = start;
            EndField = GenericOperator<T, U, T>.Add(start, width);
        }

        /// <summary>
        /// 闭区间判断：Start/Min &lt;= value &lt;= End/Max 时返回 true
        /// </summary>
        public bool Contains(T value)
        {
            if (value.CompareTo(StartField) >= 0)
            {
                return value.CompareTo(EndField) <= 0;
            }
            return false;
        }

        /// <summary>
        /// 半开区间判断：下界 &lt;= value &lt; 上界 时返回 true
        /// </summary>
        public bool ContainsInclusiveExclusive(T value)
        {
            if (value.CompareTo(StartField) >= 0)
            {
                return value.CompareTo(EndField) < 0;
            }
            return false;
        }

        /// <summary>
        /// 开区间判断：下界 &lt; value &lt; 上界 时返回 true
        /// </summary>
        public bool ContainsExclusive(T value)
        {
            if (value.CompareTo(StartField) > 0)
            {
                return value.CompareTo(EndField) < 0;
            }
            return false;
        }

        /// <summary>
        /// 是否包含另一个区间（闭区间语义）；任一区间为空区间时返回 false
        /// </summary>
        public bool Contains(Range<T, U> range)
        {
            if (IsEmpty || range.IsEmpty)
            {
                return false;
            }
            T startField = range.StartField;
            if (startField.CompareTo(StartField) >= 0)
            {
                startField = range.EndField;
                return startField.CompareTo(EndField) <= 0;
            }
            return false;
        }

        /// <summary>
        /// 是否严格包含另一个区间（两端都不接触边界）；任一区间为空区间时返回 false
        /// </summary>
        public bool ContainsExclusive(Range<T, U> range)
        {
            if (IsEmpty || range.IsEmpty)
            {
                return false;
            }
            T startField = range.StartField;
            if (startField.CompareTo(StartField) > 0)
            {
                startField = range.EndField;
                return startField.CompareTo(EndField) < 0;
            }
            return false;
        }

        /// <summary>
        /// 把 value 限制在 [Min, Max] 内：小于下界返回下界，大于上界返回上界，否则原样返回
        /// </summary>
        public T Cap(T value)
        {
            if (value.CompareTo(StartField) < 0)
            {
                return StartField;
            }
            if (value.CompareTo(EndField) > 0)
            {
                return EndField;
            }
            return value;
        }

        /// <summary>
        /// 两端各向外扩展 extension，返回新区间
        /// </summary>
        public Range<T, U> ExtendBy(U extension)
        {
            T value = GenericOperator<T, U, T>.Subtract(StartField, extension);
            T value2 = GenericOperator<T, U, T>.Add(EndField, extension);
            return new Range<T, U>(value, value2);
        }

        /// <summary>
        /// 起点向前扩展 lowerExtension、终点向后扩展 upperExtension，返回新区间
        /// </summary>
        public Range<T, U> ExtendBy(U lowerExtension, U upperExtension)
        {
            T value = GenericOperator<T, U, T>.Subtract(StartField, lowerExtension);
            T value2 = GenericOperator<T, U, T>.Add(EndField, upperExtension);
            return new Range<T, U>(value, value2);
        }

        /// <summary>
        /// 与 range 有交集时返回两者覆盖范围的并集，没有交集时返回 <see cref="Empty"/>
        /// </summary>
        public Range<T, U> ExtendOverlapping(Range<T, U> range)
        {
            if (Intersect(this, range).IsEmpty)
            {
                return Empty;
            }
            T startField = StartField;
            T value = ((startField.CompareTo(range.StartField) > 0) ? range.StartField : StartField);
            startField = EndField;
            T value2 = ((startField.CompareTo(range.EndField) < 0) ? range.EndField : EndField);
            return new Range<T, U>(value, value2);
        }

        /// <summary>
        /// 与 range 是否相交（闭区间语义，端点接触也算相交）
        /// </summary>
        public bool IntersectsWith(Range<T, U> range)
        {
            return !Intersect(this, range).IsEmpty;
        }

        /// <summary>
        /// 与 range 是否相交（开区间语义，仅端点接触不算相交）
        /// </summary>
        public bool IntersectsExclusiveWith(Range<T, U> range)
        {
            return !IntersectExclusive(this, range).IsEmpty;
        }

        /// <summary>
        /// 返回与 range 的交集（闭区间语义）；不相交时返回 <see cref="Empty"/>
        /// </summary>
        public Range<T, U> GetIntersectionWith(Range<T, U> range)
        {
            return Intersect(this, range);
        }

        /// <summary>
        /// 返回与 range 的交集（开区间语义）；不相交时返回 <see cref="Empty"/>
        /// </summary>
        public Range<T, U> IntersectExclusive(Range<T, U> range)
        {
            return IntersectExclusive(this, range);
        }

        /// <summary>
        /// 返回覆盖两个区间的最小区间；其中一个为空区间时返回另一个
        /// </summary>
        public Range<T, U> Merge(Range<T, U> range)
        {
            return Merge(this, range);
        }

        /// <summary>
        /// 合并两个区间，等同于 <see cref="Merge(Range{T, U}, Range{T, U})"/>
        /// </summary>
        public static Range<T, U> operator +(Range<T, U> range1, Range<T, U> range2)
        {
            return Merge(range1, range2);
        }

        /// <summary>
        /// 求两个区间的交集，等同于 <see cref="Intersect(Range{T, U}, Range{T, U})"/>
        /// </summary>
        public static Range<T, U> operator *(Range<T, U> range1, Range<T, U> range2)
        {
            return Intersect(range1, range2);
        }

        /// <summary>
        /// 两个区间的起点与终点是否都相等
        /// </summary>
        public static bool operator ==(Range<T, U> range1, Range<T, U> range2)
        {
            return range1.Equals(range2);
        }

        /// <summary>
        /// 两个区间的起点或终点是否不相等
        /// </summary>
        public static bool operator !=(Range<T, U> range1, Range<T, U> range2)
        {
            return !range1.Equals(range2);
        }

        /// <summary>
        /// 求两个区间的交集（闭区间语义）；任一为空区间或不相交时返回 <see cref="Empty"/>
        /// </summary>
        public static Range<T, U> Intersect(Range<T, U> range1, Range<T, U> range2)
        {
            if (!range1.IsEmpty && !range2.IsEmpty)
            {
                T startField = range1.StartField;
                if (startField.CompareTo(range2.EndField) <= 0)
                {
                    startField = range1.EndField;
                    if (startField.CompareTo(range2.StartField) >= 0)
                    {
                        startField = range1.StartField;
                        T value = ((startField.CompareTo(range2.StartField) > 0) ? range1.StartField : range2.StartField);
                        startField = range1.EndField;
                        T value2 = ((startField.CompareTo(range2.EndField) < 0) ? range1.EndField : range2.EndField);
                        return new Range<T, U>(value, value2);
                    }
                }
            }
            return Empty;
        }

        /// <summary>
        /// 求两个区间的交集（开区间语义）；任一为空区间或不相交时返回 <see cref="Empty"/>
        /// </summary>
        public static Range<T, U> IntersectExclusive(Range<T, U> range1, Range<T, U> range2)
        {
            if (!range1.IsEmpty && !range2.IsEmpty)
            {
                T startField = range1.StartField;
                if (startField.CompareTo(range2.EndField) < 0)
                {
                    startField = range1.EndField;
                    if (startField.CompareTo(range2.StartField) > 0)
                    {
                        startField = range1.StartField;
                        T value = ((startField.CompareTo(range2.StartField) > 0) ? range1.StartField : range2.StartField);
                        startField = range1.EndField;
                        T value2 = ((startField.CompareTo(range2.EndField) < 0) ? range1.EndField : range2.EndField);
                        return new Range<T, U>(value, value2);
                    }
                }
            }
            return Empty;
        }

        /// <summary>
        /// 返回覆盖两个区间的最小区间；其中一个为空区间时返回另一个
        /// </summary>
        public static Range<T, U> Merge(Range<T, U> range1, Range<T, U> range2)
        {
            if (range1.IsEmpty)
            {
                return range2;
            }
            if (range2.IsEmpty)
            {
                return range1;
            }
            T startField = range1.StartField;
            T value = ((startField.CompareTo(range2.StartField) > 0) ? range2.StartField : range1.StartField);
            startField = range1.EndField;
            T value2 = ((startField.CompareTo(range2.EndField) < 0) ? range2.EndField : range1.EndField);
            return new Range<T, U>(value, value2);
        }

        /// <summary>
        /// 先比较起点、再比较终点；两者都相等时返回 0
        /// </summary>
        public int CompareTo(Range<T, U> other)
        {
            T startField = StartField;
            if (startField.Equals(other.StartField))
            {
                startField = EndField;
                if (startField.Equals(other.EndField))
                {
                    return 0;
                }
            }
            startField = StartField;
            int num = startField.CompareTo(other.StartField);
            if (num != 0)
            {
                return num;
            }
            startField = EndField;
            return startField.CompareTo(other.EndField);
        }

        /// <summary>
        /// 起点与终点是否都相等
        /// </summary>
        public bool Equals(Range<T, U> other)
        {
            T startField = StartField;
            if (startField.Equals(other.StartField))
            {
                startField = EndField;
                return startField.Equals(other.EndField);
            }
            return false;
        }

        /// <summary>
        /// obj 是同类型区间且上下界都相等时返回 true
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Range<T, U>)
            {
                return Equals((Range<T, U>)obj);
            }
            return false;
        }

        /// <summary>
        /// 由上下界组合出的哈希值
        /// </summary>
        public override int GetHashCode()
        {
            T startField = StartField;
            int num = (5993773 + startField.GetHashCode()) * 9973;
            startField = EndField;
            return num + startField.GetHashCode();
        }
    }
}
