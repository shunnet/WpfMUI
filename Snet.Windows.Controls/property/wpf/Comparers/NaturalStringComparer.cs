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
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Implements a natural comparer for strings.
    /// </summary>
    public class NaturalStringComparer : IComparer<string?>
    {
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
        public int Compare(string? x, string? y)
        {
            if (x == null)
            {
                return y == null ? 0 : -1;
            }

            if (y == null)
            {
                return 1;
            }

            // Convert to homogeneous string tokens. Keeping numeric tokens as strings avoids
            // mixing Int32 and String when a numeric run exceeds Int32.MaxValue.
            var xitems = SplitToTokens(x.Replace(" ", string.Empty));
            var yitems = SplitToTokens(y.Replace(" ", string.Empty));

            int commonLength = Math.Min(xitems.Length, yitems.Length);
            for (int i = 0; i < commonLength; i++)
            {
                int result = CompareToken(xitems[i], yitems[i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return xitems.Length.CompareTo(yitems.Length);
        }

        /// <summary>
        /// 将字符串切分为数字/文本 token 序列（与原有 Regex.Split + int.TryParse 语义完全一致）。
        /// </summary>
        /// <param name="str">去除空格后的字符串。</param>
        /// <returns>token 数组。</returns>
        private static string[] SplitToTokens(string str)
        {
            return Digits.Split(str);
        }

        /// <summary>
        /// Compares two text or numeric tokens without converting numeric values to a bounded integer type.
        /// </summary>
        private static int CompareToken(string left, string right)
        {
            bool leftIsNumber = IsDigits(left);
            bool rightIsNumber = IsDigits(right);
            if (leftIsNumber && rightIsNumber)
            {
                string normalizedLeft = left.TrimStart('0');
                string normalizedRight = right.TrimStart('0');
                if (normalizedLeft.Length == 0)
                {
                    normalizedLeft = "0";
                }
                if (normalizedRight.Length == 0)
                {
                    normalizedRight = "0";
                }

                int lengthResult = normalizedLeft.Length.CompareTo(normalizedRight.Length);
                if (lengthResult != 0)
                {
                    return lengthResult;
                }

                int numericResult = string.CompareOrdinal(normalizedLeft, normalizedRight);
                if (numericResult != 0)
                {
                    return numericResult;
                }

                // Preserve a total order for values such as "1" and "01".
                return left.Length.CompareTo(right.Length);
            }

            if (leftIsNumber != rightIsNumber)
            {
                return leftIsNumber ? -1 : 1;
            }

            return StringComparer.CurrentCulture.Compare(left, right);
        }

        private static bool IsDigits(string value)
        {
            if (value.Length == 0)
            {
                return false;
            }

            foreach (char character in value)
            {
                if (character is < '0' or > '9')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
