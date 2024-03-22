using FluentAssertions;
using Xunit;

namespace ZingPDF.Objects.Filters;

public class RunLengthDecodeFilterTests
{
    [Theory]
    [InlineData(new byte[] { 1, 1, 1, 2, 3, 3, 4, 4, 4, 4 }, new byte[] { 254, 1, 0, 2, 255, 3, 253, 4, 128 })]
    [InlineData(new byte[] { 1, 2, 3, 4, 5 }, new byte[] { 4, 1, 2, 3, 4, 5, 128 })]
    public void EncodeBasic(byte[] input, byte[] expectedOutput)
    {
        var output = new RunLengthDecodeFilter().Encode(input);

        output.Should().BeEquivalentTo(expectedOutput);
    }

    [Theory]
    [InlineData(new byte[] { 254, 1, 0, 2, 255, 3, 253, 4, 128 }, new byte[] { 1, 1, 1, 2, 3, 3, 4, 4, 4, 4 })]
    [InlineData(new byte[] { 4, 1, 2, 3, 4, 5, 128 }, new byte[] { 1, 2, 3, 4, 5 })]
    public void DecodeBasic(byte[] input, byte[] expectedOutput)
    {
        var output = new RunLengthDecodeFilter().Decode(input);

        output.Should().BeEquivalentTo(expectedOutput);
    }
}
