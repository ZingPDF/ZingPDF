using FakeItEasy;
using FluentAssertions;
using Xunit;
using ZingPDF.Extensions;

namespace ZingPDF.Parsing.Parsers.Objects;

public class RealNumberParserTests
{
    [Theory]
    [InlineData("0.000000", 0d)]
    [InlineData("595.276000", 595.276000)]
    [InlineData("841.890000", 841.890000)]
    public async Task ParseAsyncBasic(string input, double expected)
    {
        var output = await new RealNumberParser().ParseAsync(input.ToStream());

        output.Value.Should().Be(expected);
    }
}
