using System.Globalization;
using ZingPDF.Extensions;

namespace ZingPDF.Objects.DataStructures
{
    internal class Date : PdfObject
    {
        public Date(DateTimeOffset dateTimeOffset)
        {
            DateTimeOffset = dateTimeOffset;
        }

        public DateTimeOffset DateTimeOffset { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            string formattedDateTime = DateTimeOffset.ToString("yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo);
            string offsetString = $"{(DateTimeOffset.Offset.Hours >= 0 ? "+" : "-")}{Math.Abs(DateTimeOffset.Offset.Hours)}'{DateTimeOffset.Offset.Minutes:00}'";

            await stream.WriteTextAsync($"(D:{formattedDateTime}{offsetString})");
        }
    }
}
