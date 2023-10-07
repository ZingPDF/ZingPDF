using ZingPdf.Core.Objects;

namespace ZingPdf.Core.Extensions
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Splits a string on the provided delimiter characters.
        /// The delimiters are kept as part of the output.
        /// </summary>
        public static IEnumerable<string> SplitAndKeep(this string s, char[] delims)
        {
            int start = 0, index;

            while ((index = s.IndexOfAny(delims, start)) != -1)
            {
                if (index - start > 0)
                    yield return s[start..index];

                yield return s.Substring(index, 1);

                start = index + 1;
            }

            if (start < s.Length)
            {
                yield return s[start..];
            }
        }

        /// <summary>
        /// Given a collection of delimiters, split the input prior to each, keeping the delimiter.
        /// </summary>
        public static IEnumerable<string> SplitBefore(this string s, params char[] delims)
        {
            if (!string.IsNullOrWhiteSpace(s))
            {
                int start = 0, index;

                while ((index = s.IndexOfAny(delims, start)) != -1)
                {
                    if (index - start > 0)
                    {
                        if (start > 0)
                        {
                            start -= 1;
                        }

                        yield return s[start..index];
                    }

                    start = index + 1;
                }

                if (start < s.Length)
                {
                    if (start > 0)
                    {
                        start -= 1;
                    }

                    yield return s[start..];
                }
            }
        }

        public static bool IsInteger(this string input)
        {
            foreach (char c in input)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }
    }
}
