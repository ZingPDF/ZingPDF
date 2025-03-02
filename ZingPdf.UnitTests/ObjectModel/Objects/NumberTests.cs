using FluentAssertions;
using Xunit;
using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects;

public class NumberTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(1.1, "1.1")]
    [InlineData(2.22, "2.22")]
    [InlineData(3.333, "3.333")]
    [InlineData(4.4444, "4.4444")]
    [InlineData(5.55555, "5.55555")]
    [InlineData(6.666666, "6.666666")]
    public async Task Render(double input, string expectedOutput)
    {
        using var ms = new MemoryStream();

        await new Number(input).WriteAsync(ms);

        ms.Position = 0;

        var output = await ms.GetAsync();

        output.Should().Be(expectedOutput);
    }
}
