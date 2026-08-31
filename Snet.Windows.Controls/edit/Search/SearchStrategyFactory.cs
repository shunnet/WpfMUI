// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Snet.Windows.Controls.edit.Search
{
    /// <summary>
    /// Provides factory methods for ISearchStrategies.
    /// </summary>
    public static class SearchStrategyFactory
    {
        // Compiled regex construction is expensive (JIT + parse), and SearchPanel.Create is called on
        // every search-pattern change / option toggle. Cache the compiled regexes with a small LRU so
        // repeated searches with the same (pattern, ignoreCase) combination reuse the instance.
        static readonly object regexCacheLock = new object();
        static readonly Dictionary<CacheKey, Regex> regexCache = new Dictionary<CacheKey, Regex>();
        static readonly List<CacheKey> regexCacheOrder = new List<CacheKey>();
        const int MaxRegexCacheSize = 20;

        struct CacheKey : IEquatable<CacheKey>
        {
            public readonly string Pattern;
            public readonly bool IgnoreCase;

            public CacheKey(string pattern, bool ignoreCase)
            {
                this.Pattern = pattern;
                this.IgnoreCase = ignoreCase;
            }

            public bool Equals(CacheKey other)
            {
                return IgnoreCase == other.IgnoreCase
                    && string.Equals(Pattern, other.Pattern, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is CacheKey && Equals((CacheKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = IgnoreCase.GetHashCode();
                    if (Pattern != null)
                        hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Pattern);
                    return hashCode;
                }
            }
        }

        static Regex GetOrCreateRegex(string regexPattern, bool ignoreCase)
        {
            var key = new CacheKey(regexPattern, ignoreCase);
            lock (regexCacheLock)
            {
                Regex regex;
                if (regexCache.TryGetValue(key, out regex))
                {
                    // move to most-recently-used position
                    int index = regexCacheOrder.IndexOf(key);
                    if (index > 0)
                    {
                        regexCacheOrder.RemoveAt(index);
                        regexCacheOrder.Insert(0, key);
                    }
                    return regex;
                }
                RegexOptions options = RegexOptions.Compiled | RegexOptions.Multiline;
                if (ignoreCase)
                    options |= RegexOptions.IgnoreCase;
                regex = new Regex(regexPattern, options);
                regexCache.Add(key, regex);
                regexCacheOrder.Insert(0, key);
                if (regexCacheOrder.Count > MaxRegexCacheSize)
                {
                    // evict least-recently-used entry (at the end of the order list)
                    CacheKey lruKey = regexCacheOrder[regexCacheOrder.Count - 1];
                    regexCacheOrder.RemoveAt(regexCacheOrder.Count - 1);
                    regexCache.Remove(lruKey);
                }
                return regex;
            }
        }

        /// <summary>
        /// Creates a default ISearchStrategy with the given parameters.
        /// </summary>
        public static ISearchStrategy Create(string searchPattern, bool ignoreCase, bool matchWholeWords, SearchMode mode)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");

            switch (mode)
            {
                case SearchMode.Normal:
                    // plain-text search: keep the raw pattern so RegexSearchStrategy can use a fast
                    // string.IndexOf loop instead of running the regex engine over the whole document.
                    try
                    {
                        return new RegexSearchStrategy(searchPattern, matchWholeWords, ignoreCase, isLiteralSearch: true);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new SearchPatternException(ex.Message, ex);
                    }
                case SearchMode.Wildcard:
                    searchPattern = ConvertWildcardsToRegex(searchPattern);
                    break;
            }
            try
            {
                Regex pattern = GetOrCreateRegex(searchPattern, ignoreCase);
                return new RegexSearchStrategy(pattern, matchWholeWords);
            }
            catch (ArgumentException ex)
            {
                throw new SearchPatternException(ex.Message, ex);
            }
        }

        static string ConvertWildcardsToRegex(string searchPattern)
        {
            if (string.IsNullOrEmpty(searchPattern))
                return "";

            StringBuilder builder = new StringBuilder();

            foreach (char ch in searchPattern)
            {
                switch (ch)
                {
                    case '?':
                        builder.Append(".");
                        break;
                    case '*':
                        builder.Append(".*");
                        break;
                    default:
                        builder.Append(Regex.Escape(ch.ToString()));
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
