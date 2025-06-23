using FluentAssertions;
using System.Globalization;
using Xunit;
using ZingPDF.Extensions;

namespace ZingPDF.Syntax.CommonDataStructures;

public class DateTests
{
    [Fact]
    internal async Task WriteAsyncProducesCorrectOutput()
    {
        var date = new Date(DateTimeOffset.ParseExact("20230922161207+1000", "yyyyMMddHHmmsszzz", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal));

        using var ms = new MemoryStream();

        await date.WriteAsync(ms);

        ms.Position = 0;
        var output = await ms.GetAsync();

        output.Should().Be("(D:20230922161207+10'00')");
    }
}
