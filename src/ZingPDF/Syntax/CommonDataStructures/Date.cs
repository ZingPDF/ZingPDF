using System.Globalization;
using ZingPDF.Extensions;

namespace ZingPDF.Syntax.CommonDataStructures;

public class Date(DateTimeOffset dateTimeOffset, ObjectContext context)
    : PdfObject(context)
{
    public Date(DateTimeOffset dateTimeOffset)
        : this(dateTimeOffset, ObjectContext.UserCreated)
    {
    }

    public DateTimeOffset DateTimeOffset { get; } = dateTimeOffset;

    protected override async Task WriteOutputAsync(Stream stream)
    {
        string formattedDateTime = DateTimeOffset.ToString("yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo);
        var totalOffsetMinutes = (int)Math.Abs(DateTimeOffset.Offset.TotalMinutes);
        var offsetHours = totalOffsetMinutes / 60;
        var offsetMinutes = totalOffsetMinutes % 60;
        string offsetString = $"{(DateTimeOffset.Offset >= TimeSpan.Zero ? "+" : "-")}{offsetHours:00}'{offsetMinutes:00}'";

        await stream.WriteTextAsync($"(D:{formattedDateTime}{offsetString})");
    }

    public override object Clone() => new Date(DateTimeOffset, Context);
}
