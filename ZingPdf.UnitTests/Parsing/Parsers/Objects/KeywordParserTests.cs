using FluentAssertions;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.Parsing.Parsers.Objects;

public class KeywordParserTests
{
    [Theory]
    [InlineData("startxref", "startxref")]
    [InlineData("\r\nstartxref", "startxref")]
    public async Task ParseBasicAsync(string content, string expected)
    {
        using var input = content.ToStream();

        Keyword expectedKeyword = expected;

        var output = await new KeywordParser().ParseAsync(input);

        output.Should().BeEquivalentTo(expectedKeyword);
    }
}
