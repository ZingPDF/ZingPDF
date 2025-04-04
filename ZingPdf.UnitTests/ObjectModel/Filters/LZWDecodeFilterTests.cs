using FluentAssertions;
using Xunit;

namespace ZingPDF.Syntax.Filters;

public class LZWDecodeFilterTests
{
    [Theory]
    [InlineData(new byte[] { 1, 0, 0, 0, 3, 0, 0, 0 }, new byte[] { 1, 3 })]
    [InlineData(new byte[] { 1, 0, 0, 0, 4, 0, 0, 0 }, new byte[] { 1, 4 })]
    [InlineData(new byte[] { 1, 0, 0, 0, 5, 0, 0, 0 }, new byte[] { 1, 5 })]
    public void DecodeProducesProperOutput(byte[] encoded, byte[] decoded)
    {
        var encodedStream = new MemoryStream(encoded);

        new LZWDecodeFilter(null)
            .Decode(encodedStream)
            .ToArray()
            .Should().BeEquivalentTo(decoded);
    }

    [Theory]
    [InlineData(new byte[] { 1, 3 }, new byte[] { 1, 0, 0, 0, 3, 0, 0, 0 })]
    [InlineData(new byte[] { 1, 4 }, new byte[] { 1, 0, 0, 0, 4, 0, 0, 0 })]
    [InlineData(new byte[] { 1, 5 }, new byte[] { 1, 0, 0, 0, 5, 0, 0, 0 })]
    public void EncodeProducesProperOutput(byte[] input, byte[] encoded)
    {
        using var inputStream = new MemoryStream(input);

        new LZWDecodeFilter(null)
            .Encode(inputStream)
            .ToArray()
            .Should().BeEquivalentTo(encoded);
    }
}
