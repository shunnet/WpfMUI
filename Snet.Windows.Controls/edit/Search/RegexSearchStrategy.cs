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

using Snet.Windows.Controls.edit.Document;
using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace Snet.Windows.Controls.edit.Search
{
    class RegexSearchStrategy : ISearchStrategy
    {
        readonly Regex searchPattern;
        readonly bool matchWholeWords;

        // Literal (plain text, Normal mode) searches avoid the regex engine entirely:
        // a string.IndexOf loop over the document text is significantly faster and avoids
        // allocating Match objects. searchPattern is null in this mode.
        readonly bool isLiteralSearch;
        readonly string literalSearchPattern;
        readonly StringComparison comparison;

        public RegexSearchStrategy(Regex searchPattern, bool matchWholeWords)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            this.searchPattern = searchPattern;
            this.matchWholeWords = matchWholeWords;
            this.isLiteralSearch = false;
        }

        /// <summary>
        /// Creates a strategy for plain-text (non-regex) search.
        /// </summary>
        public RegexSearchStrategy(string literalSearchPattern, bool matchWholeWords, bool ignoreCase, bool isLiteralSearch)
        {
            if (isLiteralSearch && literalSearchPattern == null)
                throw new ArgumentNullException("literalSearchPattern");
            this.literalSearchPattern = literalSearchPattern;
            this.matchWholeWords = matchWholeWords;
            this.isLiteralSearch = isLiteralSearch;
            this.comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        }

        public IEnumerable<ISearchResult> FindAll(ITextSource document, int offset, int length)
        {
            if (isLiteralSearch)
                return FindAllLiteral(document, offset, length);
            return FindAllRegex(document, offset, length);
        }

        IEnumerable<ISearchResult> FindAllRegex(ITextSource document, int offset, int length)
        {
            int endOffset = offset + length;
            foreach (Match result in searchPattern.Matches(document.Text))
            {
                int resultEndOffset = result.Length + result.Index;
                if (offset > result.Index || endOffset < resultEndOffset)
                    continue;
                if (matchWholeWords && (!IsWordBorder(document, result.Index) || !IsWordBorder(document, resultEndOffset)))
                    continue;
                yield return new SearchResult { StartOffset = result.Index, Length = result.Length, Data = result };
            }
        }

        IEnumerable<ISearchResult> FindAllLiteral(ITextSource document, int offset, int length)
        {
            if (string.IsNullOrEmpty(literalSearchPattern))
                yield break;
            int endOffset = offset + length;
            string text = document.Text;
            int searchOffset = 0;
            while (searchOffset <= text.Length)
            {
                int index = text.IndexOf(literalSearchPattern, searchOffset, comparison);
                if (index < 0)
                    break;
                int resultEndOffset = index + literalSearchPattern.Length;
                if (offset <= index && endOffset >= resultEndOffset)
                {
                    if (!matchWholeWords || (IsWordBorder(document, index) && IsWordBorder(document, resultEndOffset)))
                        yield return new SearchResult { StartOffset = index, Length = literalSearchPattern.Length, Data = null };
                }
                searchOffset = index + 1;
            }
        }

        static bool IsWordBorder(ITextSource document, int offset)
        {
            return TextUtilities.GetNextCaretPosition(document, offset - 1, LogicalDirection.Forward, CaretPositioningMode.WordBorder) == offset;
        }

        public ISearchResult FindNext(ITextSource document, int offset, int length)
        {
            return FindAll(document, offset, length).FirstOrDefault();
        }

        public bool Equals(ISearchStrategy other)
        {
            var strategy = other as RegexSearchStrategy;
            if (strategy == null)
                return false;
            if (this.isLiteralSearch != strategy.isLiteralSearch)
                return false;
            if (this.isLiteralSearch)
                return string.Equals(this.literalSearchPattern, strategy.literalSearchPattern, StringComparison.Ordinal)
                    && this.comparison == strategy.comparison;
            return strategy.searchPattern.ToString() == searchPattern.ToString() &&
                strategy.searchPattern.Options == searchPattern.Options &&
                strategy.searchPattern.RightToLeft == searchPattern.RightToLeft;
        }
    }

    class SearchResult : TextSegment, ISearchResult
    {
        public Match Data { get; set; }

        public string ReplaceWith(string replacement)
        {
            return Data.Result(replacement);
        }
    }
}
