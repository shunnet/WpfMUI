// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FormattedTimeSpanParser.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   TimeSpan parser that use a format string to evaluate the input string.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

    /// <summary>
    /// TimeSpan parser that use a format string to evaluate the input string.
    /// </summary>
    /// <remarks>Supports the following format codes: D, DD, H, HH, M, MM, S, SS, d, dd, h, hh, m, mm, s, ss, f, ff, fff</remarks>
    public class FormattedTimeSpanParser
    {
        /// <summary>
        /// The special characters.
        /// </summary>
        private const string specialCharacters = @"\?$^.+*|{}[]()";

        /// <summary>
        /// The conversion expression.
        /// </summary>
        private readonly Regex conversionExpression =
            new Regex("D{1,2}|H{1,2}|M{1,2}|S{1,2}|d{1,2}|h{1,2}|m{1,2}|s{1,2}|f{1,7}", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedTimeSpanParser" /> class.
        /// </summary>
        /// <param name="formatString">The format string.</param>
        public FormattedTimeSpanParser(string formatString)
        {
            // escape special characters
            foreach (char specialChar in specialCharacters)
            {
                formatString = formatString.Replace(
                    specialChar.ToString(CultureInfo.InvariantCulture), @"\" + specialChar);
            }

            // replace all format codes by regular expression groups
            // the name of the groups is the same as the format code ("hh", "mm" etc.)
            formatString = this.conversionExpression.Replace(
                formatString,
                m =>
                    {
                        string length = "{1," + m.Value.Length + "}";

                        if (m.Value.Length == 1)
                        {
                            length = "{1,2}";
                        }

                        return @"(?<" + m.Value + @">\d" + length + ")";
                    });

            // escape tabs and whitespaces
            formatString = formatString.Replace("\t", @"\t");
            formatString = formatString.Replace(" ", @"\s");

            // 必须匹配整个输入（允许首尾空白）：没有锚定时只要字符串里"含有"一段匹配就被接受，
            // "123:45:67" 会被读成 23:45:67、"x01:02:03abc" 的尾随内容被丢弃——和"不匹配就静默变成零"是同一类静默错误
            this.Expression = new Regex(@"^\s*" + formatString + @"\s*$", RegexOptions.Compiled);
        }

        /// <summary>
        /// Gets or sets the regular expression.
        /// </summary>
        private Regex Expression { get; set; }

        /// <summary>
        /// Parses the specified time span string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A time span.
        /// </returns>
        /// <exception cref="FormatException">The value does not match the format string (an input that does not match used to be returned as <see cref="TimeSpan.Zero"/> silently).</exception>
        public TimeSpan Parse(string value)
        {
            if (this.TryParse(value, out TimeSpan result))
            {
                return result;
            }

            throw new FormatException(string.Format("Invalid TimeSpan value '{0}'.", value));
        }

        /// <summary>
        /// Tries to parse the specified time span string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="result">The parsed time span.</param>
        /// <returns><c>true</c> if the value matches the format string and fits into a <see cref="TimeSpan"/>; otherwise, <c>false</c>.</returns>
        public bool TryParse(string value, out TimeSpan result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var r = this.Expression.Match(value);
            if (!r.Success)
            {
                return false;
            }

            int days = 0;
            int hours = 0;
            int minutes = 0;
            int seconds = 0;
            int milliseconds = 0;

            for (int groupNumber = 1; groupNumber < r.Groups.Count; groupNumber++)
            {
                var group = r.Groups[groupNumber];
                var name = this.Expression.GroupNameFromNumber(groupNumber);

                if (group.Success)
                {
                    // Console.WriteLine("Match on " + name + ": '" + group.Value + "'");
                    if (!int.TryParse(group.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int v))
                    {
                        return false;
                    }
                    switch (name)
                    {
                        case "DD":
                        case "D":
                        case "dd":
                        case "d":
                            days = v;
                            break;
                        case "HH":
                        case "H":
                        case "hh":
                        case "h":
                            hours = v;
                            break;
                        case "MM":
                        case "M":
                        case "mm":
                        case "m":
                            minutes = v;
                            break;
                        case "SS":
                        case "S":
                        case "ss":
                        case "s":
                            seconds = v;
                            break;
                        case "fff":
                        case "ff":
                        case "f":
                            milliseconds = v;
                            break;
                    }
                }
            }

            try
            {
                result = new TimeSpan(days, hours, minutes, seconds, milliseconds);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                // 天数等超出 TimeSpan 可表示范围
                return false;
            }
        }
    }
}