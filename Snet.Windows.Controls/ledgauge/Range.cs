using Newtonsoft.Json;
using System.Diagnostics;

namespace Snet.Windows.Controls.ledgauge
{
    [JsonObject(MemberSerialization.OptIn)]
    [DebuggerDisplay("Range Min = {Min}, Max = {Max},  Width = {Width}, Center = {Center}")]
    public readonly struct Range<T> : IComparable<Range<T>>, IEquatable<Range<T>> where T : struct, IComparable, IFormattable, IConvertible, IComparable<T>, IEquatable<T>
    {
        private static readonly Range<T> empty = default(Range<T>);

        private static readonly T two = GenericOperator<T, T, T>.Increment(GenericOperator<T, T, T>.Increment(GenericOperator<T, T, T>.Zero));

        [JsonProperty(PropertyName = "Value1")]
        public readonly T MinField;

        [JsonProperty(PropertyName = "Value2")]
        public readonly T MaxField;

        public T Min
        {
            [DebuggerStepThrough]
            get
            {
                return MinField;
            }
        }

        public T Max
        {
            [DebuggerStepThrough]
            get
            {
                return MaxField;
            }
        }

        public T Width
        {
            [DebuggerStepThrough]
            get
            {
                return GenericOperator<T, T, T>.Subtract(MaxField, MinField);
            }
        }

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

        public bool IsEmpty
        {
            [DebuggerStepThrough]
            get
            {
                T minField = MinField;
                return minField.Equals(MaxField);
            }
        }

        public static Range<T> Empty
        {
            [DebuggerStepThrough]
            get
            {
                return empty;
            }
        }

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

        public bool Contains(T value)
        {
            if (value.CompareTo(MinField) >= 0)
            {
                return value.CompareTo(MaxField) <= 0;
            }
            return false;
        }

        public bool ContainsExclusive(T value)
        {
            if (value.CompareTo(MinField) > 0)
            {
                return value.CompareTo(MaxField) < 0;
            }
            return false;
        }

        public bool ContainsInclusiveExclusive(T value)
        {
            if (value.CompareTo(MinField) >= 0)
            {
                return value.CompareTo(MaxField) < 0;
            }
            return false;
        }

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

        public Range<T> ExtendBy(T extension)
        {
            T value = GenericOperator<T, T, T>.Subtract(MinField, extension);
            T value2 = GenericOperator<T, T, T>.Add(MaxField, extension);
            return new Range<T>(value, value2);
        }

        public Range<T> ExtendBy(T lowerExtension, T upperExtension)
        {
            T value = GenericOperator<T, T, T>.Subtract(MinField, lowerExtension);
            T value2 = GenericOperator<T, T, T>.Add(MaxField, upperExtension);
            return new Range<T>(value, value2);
        }

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

        public bool IntersectsWith(Range<T> range)
        {
            return !Intersect(this, range).IsEmpty;
        }

        public bool IntersectsExclusiveWith(Range<T> range)
        {
            return !IntersectExclusive(this, range).IsEmpty;
        }

        public Range<T> GetIntersectionWith(Range<T> range)
        {
            return Intersect(this, range);
        }

        public Range<T> IntersectExclusive(Range<T> range)
        {
            return IntersectExclusive(this, range);
        }

        public Range<T> Merge(Range<T> range)
        {
            return Merge(this, range);
        }

        public static Range<T> operator +(Range<T> range1, Range<T> range2)
        {
            return Merge(range1, range2);
        }

        public static Range<T> operator *(Range<T> range1, Range<T> range2)
        {
            return Intersect(range1, range2);
        }

        public static bool operator ==(Range<T> range1, Range<T> range2)
        {
            return range1.Equals(range2);
        }

        public static bool operator !=(Range<T> range1, Range<T> range2)
        {
            return !range1.Equals(range2);
        }

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

        public override bool Equals(object obj)
        {
            if (obj is Range<T>)
            {
                return Equals((Range<T>)obj);
            }
            return false;
        }

        public override int GetHashCode()
        {
            T minField = MinField;
            int num = (5993773 + minField.GetHashCode()) * 9973;
            minField = MaxField;
            return num + minField.GetHashCode();
        }
    }
    [JsonObject(MemberSerialization.OptIn)]
    [DebuggerDisplay("Range Start = {Start}, End = {End},  Width = {Width}")]
    public readonly struct Range<T, U> : IComparable<Range<T, U>>, IEquatable<Range<T, U>> where T : struct, IComparable, IFormattable, IComparable<T>, IEquatable<T> where U : struct, IComparable, IFormattable, IComparable<U>, IEquatable<U>
    {
        private static readonly Range<T, U> empty;

        [JsonProperty(PropertyName = "Value1")]
        public readonly T StartField;

        [JsonProperty(PropertyName = "Value2")]
        public readonly T EndField;

        public T Start
        {
            [DebuggerStepThrough]
            get
            {
                return StartField;
            }
        }

        public T End
        {
            [DebuggerStepThrough]
            get
            {
                return EndField;
            }
        }

        public U Width
        {
            [DebuggerStepThrough]
            get
            {
                return GenericOperator<T, T, U>.Subtract(EndField, StartField);
            }
        }

        public bool IsEmpty
        {
            [DebuggerStepThrough]
            get
            {
                T startField = StartField;
                return startField.Equals(EndField);
            }
        }

        public static Range<T, U> Empty
        {
            [DebuggerStepThrough]
            get
            {
                return empty;
            }
        }

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

        public Range(T start, U width)
        {
            StartField = start;
            EndField = GenericOperator<T, U, T>.Add(start, width);
        }

        public bool Contains(T value)
        {
            if (value.CompareTo(StartField) >= 0)
            {
                return value.CompareTo(EndField) <= 0;
            }
            return false;
        }

        public bool ContainsInclusiveExclusive(T value)
        {
            if (value.CompareTo(StartField) >= 0)
            {
                return value.CompareTo(EndField) < 0;
            }
            return false;
        }

        public bool ContainsExclusive(T value)
        {
            if (value.CompareTo(StartField) > 0)
            {
                return value.CompareTo(EndField) < 0;
            }
            return false;
        }

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

        public Range<T, U> ExtendBy(U extension)
        {
            T value = GenericOperator<T, U, T>.Subtract(StartField, extension);
            T value2 = GenericOperator<T, U, T>.Add(EndField, extension);
            return new Range<T, U>(value, value2);
        }

        public Range<T, U> ExtendBy(U lowerExtension, U upperExtension)
        {
            T value = GenericOperator<T, U, T>.Subtract(StartField, lowerExtension);
            T value2 = GenericOperator<T, U, T>.Add(EndField, upperExtension);
            return new Range<T, U>(value, value2);
        }

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

        public bool IntersectsWith(Range<T, U> range)
        {
            return !Intersect(this, range).IsEmpty;
        }

        public bool IntersectsExclusiveWith(Range<T, U> range)
        {
            return !IntersectExclusive(this, range).IsEmpty;
        }

        public Range<T, U> GetIntersectionWith(Range<T, U> range)
        {
            return Intersect(this, range);
        }

        public Range<T, U> IntersectExclusive(Range<T, U> range)
        {
            return IntersectExclusive(this, range);
        }

        public Range<T, U> Merge(Range<T, U> range)
        {
            return Merge(this, range);
        }

        public static Range<T, U> operator +(Range<T, U> range1, Range<T, U> range2)
        {
            return Merge(range1, range2);
        }

        public static Range<T, U> operator *(Range<T, U> range1, Range<T, U> range2)
        {
            return Intersect(range1, range2);
        }

        public static bool operator ==(Range<T, U> range1, Range<T, U> range2)
        {
            return range1.Equals(range2);
        }

        public static bool operator !=(Range<T, U> range1, Range<T, U> range2)
        {
            return !range1.Equals(range2);
        }

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

        public override bool Equals(object obj)
        {
            if (obj is Range<T, U>)
            {
                return Equals((Range<T, U>)obj);
            }
            return false;
        }

        public override int GetHashCode()
        {
            T startField = StartField;
            int num = (5993773 + startField.GetHashCode()) * 9973;
            startField = EndField;
            return num + startField.GetHashCode();
        }
    }
}
