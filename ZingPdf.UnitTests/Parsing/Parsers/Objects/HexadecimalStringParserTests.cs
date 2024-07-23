using FluentAssertions;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects;

public class HexadecimalStringParserTests
{
    [Theory]
    [InlineData("<66dbd809c84b6f6bd19bb2f8865b77cc>", "66dbd809c84b6f6bd19bb2f8865b77cc")]
    [InlineData(" <66dbd809c84b6f6bd19bb2f8865b77cc>", "66dbd809c84b6f6bd19bb2f8865b77cc")]
    public async Task ParseBasicAsync(string content, string expected)
    {
        using var input = content.ToStream();

        HexadecimalString expectedHexString = expected;

        var output = await new HexadecimalStringParser().ParseAsync(input);

        output.Should().BeEquivalentTo(expectedHexString);
    }
}
