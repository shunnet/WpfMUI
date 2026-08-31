// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NaturalStringComparer.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Implements a natural comparer for strings.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Implements a natural comparer for strings.
    /// </summary>
    public class NaturalStringComparer : IComparer<string>
    {
        /// <summary>
        /// The comparer for sequences of objects.
        /// </summary>
        private static readonly EnumerableComparer<object> EnumerableOfObjectComparer = new EnumerableComparer<object>();

        /// <summary>
        /// The regular expression used to split numbers and text.
        /// </summary>
        private static readonly Regex Digits = new Regex("([0-9]+)", RegexOptions.Compiled);

        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>
        /// A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.Value Meaning Less than zero<paramref name="x" /> is less than <paramref name="y" />.Zero<paramref name="x" /> equals <paramref name="y" />.Greater than zero<paramref name="x" /> is greater than <paramref name="y" />.
        /// </returns>
        public int Compare(string x, string y)
        {
            if (x == null)
            {
                return y == null ? 0 : -1;
            }

            if (y == null)
            {
                return 1;
            }

            // convert to sequences of int/string（循环转换，避免每次比较都产生 LINQ 迭代器与闭包开销）
            var xitems = SplitToTokens(x.Replace(" ", string.Empty));
            var yitems = SplitToTokens(y.Replace(" ", string.Empty));

            // compare the sequences
            return EnumerableOfObjectComparer.Compare(xitems, yitems);
        }

        /// <summary>
        /// 将字符串切分为数字/文本 token 序列（与原有 Regex.Split + int.TryParse 语义完全一致）。
        /// </summary>
        /// <param name="str">去除空格后的字符串。</param>
        /// <returns>token 数组。</returns>
        private static object[] SplitToTokens(string str)
        {
            var parts = Digits.Split(str);
            var tokens = new object[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                int result;
                if (int.TryParse(parts[i], out result))
                {
                    tokens[i] = result;
                }
                else
                {
                    tokens[i] = parts[i];
                }
            }

            return tokens;
        }
    }
}