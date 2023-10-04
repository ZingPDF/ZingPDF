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
    }
}
