using FluentAssertions;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Syntax;

namespace ZingPDF.Parsing.Parsers.Objects;

public class NumberParserTests
{
    [Theory]
    [InlineData("0", 0d)]
    [InlineData("0.000000", 0d)]
    [InlineData("1", 1d)]
    [InlineData("595.276000", 595.276000)]
    [InlineData("841.890000", 841.890000)]
    [InlineData("% comment\r\n12", 12d)]
    [InlineData(" \r\n% comment between tokens\r\n27", 27d)]
    public async Task ParseAsyncBasic(string input, double expected)
    {
        var output = await new NumberParser()
            .ParseAsync(input.ToStream(), ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Value.Should().Be(expected);
    }
}
