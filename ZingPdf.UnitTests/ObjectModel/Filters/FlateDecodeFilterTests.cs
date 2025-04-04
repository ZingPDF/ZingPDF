using FluentAssertions;
using System.Text;
using Xunit;
using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Filters;

public class FlateDecodeFilterTests
{
    [Fact]
    public async Task DecodeBasic()
    {
        var encoded = new byte[] { 120, 218, 51, 52, 50, 54, 49, 5, 0, 2, 248, 1, 0 };

        using var ms = new MemoryStream(encoded);

        MemoryStream decoded = new FlateDecodeFilter(null).Decode(ms);

        string output = await decoded.GetAsync();

        var expectedOutput = "12345";

        output.Should().Be(expectedOutput);
    }

    [Fact]
    public void EncodeBasic()
    {
        var inputString = "12345";
        using var input = new MemoryStream(Encoding.ASCII.GetBytes(inputString));

        MemoryStream encoded = new FlateDecodeFilter(null).Encode(input);

        var expectedOutput = new byte[] { 120, 218, 51, 52, 50, 54, 49, 5, 0, 2, 248, 1, 0 };

        encoded.ToArray().Should().BeEquivalentTo(expectedOutput);
    }
}
