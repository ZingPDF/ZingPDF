using System.Text;

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

        public static bool IsInteger(this string input) => input.All(c => c.IsInteger());

        /// <summary>
        /// Removes the next EOL marker. This could be a single carriage return (\r), a single line feed (\n), or both together. 
        /// </summary>
        public static string RemoveNextEndOfLineMarker(this string input, out char[] removedChars)
        {
            var index = input.IndexOf($"{Constants.CarriageReturn}{Constants.LineFeed}");
            if (index != -1)
            {
                removedChars = new[] { Constants.CarriageReturn, Constants.LineFeed };
                return input.Remove(index, 2);
            }

            index = input.IndexOf(Constants.LineFeed);
            if (index != -1)
            {
                removedChars = new[] { Constants.LineFeed };
                return input.Remove(index + 1, 1);
            }

            index = input.IndexOf(Constants.CarriageReturn);
            if (index != -1)
            {
                removedChars = new[] { Constants.CarriageReturn };
                return input.Remove(index + 1, 1);
            }

            throw new InvalidOperationException();
        }

        public static char ToCharFromOctal(this string input)
        {
            return (char)Convert.ToInt32(input, 8);
        }

        /// <summary>
        /// Encodes text using UTF8 and returns as a MemoryStream.
        /// </summary>
        public static Stream ToStream(this string input) => new MemoryStream(Encoding.UTF8.GetBytes(input));
    }
}
