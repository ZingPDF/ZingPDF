using MorseCode.ITask;
using System.Globalization;
using ZingPDF.Extensions;
using ZingPDF.Syntax.CommonDataStructures;

namespace ZingPDF.Parsing.Parsers.DataStructures
{
    internal class DateParser : IParser<Date>
    {
        public async ITask<Date> ParseAsync(Stream stream, ParseContext context)
        {
            await stream.AdvanceBeyondNextAsync("(D:");

            var dateString = await stream.ReadUpToExcludingAsync(')');

            dateString = dateString.Replace("\'", "");

            var date = ParseCustomDateTime(dateString);

            await stream.AdvanceBeyondNextAsync(')');

            return new Date(date, context.Origin);
        }

        private static DateTimeOffset ParseCustomDateTime(string input)
        {
            string[] formats = [
                "yyyyMMddHHmmsszzz",
                "yyyyMMddHHmmzzz",
                "yyyyMMddHHzzz",
                "yyyyMMddzzz",
                "yyyyMMzzz",
                "yyyyzzz",
                "yyyyMMddHHmmss",
                "yyyyMMddHHmm",
                "yyyyMMddHH",
                "yyyyMMdd",
                "yyyyMM",
                "yyyy",
            ];

            // Try parsing with different formats
            if (DateTimeOffset.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset parsedDateTime))
            {
                return parsedDateTime;
            }
            else
            {
                throw new FormatException("Invalid date and time format.");
            }
        }
    }
}
