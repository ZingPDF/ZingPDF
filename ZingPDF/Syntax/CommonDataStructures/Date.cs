using System.Globalization;
using ZingPDF.Extensions;

namespace ZingPDF.Syntax.CommonDataStructures;

public class Date(DateTimeOffset dateTimeOffset, ObjectOrigin objectOrigin)
    : PdfObject(objectOrigin)
{
    public Date(DateTimeOffset dateTimeOffset)
        : this(dateTimeOffset, ObjectOrigin.UserCreated)
    {
    }

    public DateTimeOffset DateTimeOffset { get; } = dateTimeOffset;

    protected override async Task WriteOutputAsync(Stream stream)
    {
        string formattedDateTime = DateTimeOffset.ToString("yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo);
        string offsetString = $"{(DateTimeOffset.Offset.Hours >= 0 ? "+" : "-")}{Math.Abs(DateTimeOffset.Offset.Hours)}'{DateTimeOffset.Offset.Minutes:00}'";

        await stream.WriteTextAsync($"(D:{formattedDateTime}{offsetString})");
    }

    public override object Clone() => new Date(DateTimeOffset, Origin);
}
