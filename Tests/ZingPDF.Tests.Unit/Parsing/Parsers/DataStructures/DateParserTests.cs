using FakeItEasy;
using Xunit;
using ZingPDF.Extensions;

namespace ZingPDF.Parsing.Parsers.DataStructures;

public class DateParserTests
{
    [Theory]
    [InlineData("(D:20230922161207+10'00')")]
    [InlineData(" (D:20230922161207+10'00')")]
    [InlineData("(D:20230922161207+10'00') /Resources")]
    [InlineData("(D:202309221612+10'00')")]
    [InlineData("(D:2023092216+10'00')")]
    [InlineData("(D:20230922+10'00')")]
    [InlineData("(D:202309+10'00')")]
    [InlineData("(D:2023+10'00')")]
    [InlineData("(D:20230922161207+10'00)")]
    [InlineData(" (D:20230922161207+10'00)")]
    [InlineData("(D:20230922161207+10'00) /Resources")]
    [InlineData("(D:202309221612+10'00)")]
    [InlineData("(D:2023092216+10'00)")]
    [InlineData("(D:20230922+10'00)")]
    [InlineData("(D:202309+10'00)")]
    [InlineData("(D:2023+10'00)")]
    [InlineData("(D:20230922161207)")]
    [InlineData(" (D:20230922161207)")]
    [InlineData("(D:20230922161207) /Resources")]
    [InlineData("(D:202309221612)")]
    [InlineData("(D:2023092216)")]
    [InlineData("(D:20230922)")]
    [InlineData("(D:202309)")]
    [InlineData("(D:2023)")]
    public async Task ParseAsyncBasic(string dateString)
    {
        var stream = dateString.ToStream();

        await new DateParser()
            .ParseAsync(dateString.ToStream(), ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

        // TODO: test value
    }
}
