using FluentAssertions;
using System.Text;
using Xunit;

namespace ZingPDF.ObjectModel.Filters;

public class FlateDecodeFilterTests
{
    [Fact]
    public void DecodeBasic()
    {
        var encoded = new byte[] { 120, 218, 51, 52, 50, 54, 49, 5, 0, 2, 248, 1, 0 };

        var decoded = new FlateDecodeFilter(null).Decode(encoded);

        var output = Encoding.ASCII.GetString(decoded);

        var expectedOutput = "12345";

        output.Should().Be(expectedOutput);
    }

    [Fact]
    public void EncodeBasic()
    {
        var inputString = "12345";
        var input = Encoding.ASCII.GetBytes(inputString);

        var encoded = new FlateDecodeFilter(null).Encode(input);

        var expectedOutput = new byte[] { 120, 218, 51, 52, 50, 54, 49, 5, 0, 2, 248, 1, 0 };

        encoded.Should().BeEquivalentTo(expectedOutput);
    }
}
