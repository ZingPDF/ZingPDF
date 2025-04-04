using FluentAssertions;
using System.Text;
using Xunit;

namespace ZingPDF.Syntax.Filters;

public class ASCIIHexDecodeFilterTests
{
    [Fact]
    public void DecodeThrowsWhenMissingEODMarker()
    {
        // EOD marker is '>', which is the decimal number 62 in ASCII encoding
        using var encoded = new MemoryStream("she sells seashells on the sea shore"u8.ToArray());

        var action = () => new ASCIIHexDecodeFilter()
            .Decode(encoded);

        action.Should().Throw<FilterInputFormatException>();
    }

    [Fact]
    public void DecodeThrowsForCharactersFollowingEODMarker()
    {
        using var encoded = new MemoryStream([
            115, 104, 101, 32, 115, 101, 108, 108, 115, 32,
            115, 101, 97, 115, 104, 101, 108, 108, 115, 32,
            111, 110, 32, 116, 104, 101, 32, 115, 101, 97,
            32, 115, 104, 111, 114, 101, 62, 20, 101
            // EOD marker _______________|
        ]);

        var action = () => new ASCIIHexDecodeFilter()
            .Decode(encoded);

        action.Should().Throw<FilterInputFormatException>();
    }

    [Fact]
    public void DecodeIgnoresSpacesInEncodedData()
    {
        using var encoded = new MemoryStream("736865 20 73656C6C73207365617368656C6C73206F6E20746865207365612073686F7265>"u8.ToArray());

        new ASCIIHexDecodeFilter()
            .Decode(encoded)
            .ToArray()
            .Should().BeEquivalentTo("she sells seashells on the sea shore"u8.ToArray());
    }

    [Fact]
    public void DecodePadsOddLengthInEncodedData()
    {
        // The odd 2 should be padded to 20. 0x20 is an ASCII space.
        var encodedString = new MemoryStream("7368652073656C6C73207365617368656C6C73206F6E20746865207365612073686F72652>"u8.ToArray());

        new ASCIIHexDecodeFilter()
            .Decode(encodedString)
            .ToArray()
            .Should().BeEquivalentTo("she sells seashells on the sea shore "u8.ToArray());
    }

    [Theory]
    [InlineData(
        new byte[]
        { // Decimal ASCII values for 7368652073656C6C73207365617368656C6C73206F6E20746865207365612073686F7265>
            55, 51, 54, 56, 54, 53, 50, 48, 55, 51,
            54, 53, 54, 67, 54, 67, 55, 51, 50, 48,
            55, 51, 54, 53, 54, 49, 55, 51, 54, 56,
            54, 53, 54, 67, 54, 67, 55, 51, 50, 48,
            54, 70, 54, 69, 50, 48, 55, 52, 54, 56,
            54, 53, 50, 48, 55, 51, 54, 53, 54, 49,
            50, 48, 55, 51, 54, 56, 54, 70, 55, 50,
            54, 53, 62
        },
        new byte[]
        { // Decimal ASCII values for "she sells seashells on the sea shore"
            115, 104, 101, 32, 115, 101, 108, 108, 115, 32,
            115, 101, 97, 115, 104, 101, 108, 108, 115, 32,
            111, 110, 32, 116, 104, 101, 32, 115, 101, 97,
            32, 115, 104, 111, 114, 101
        })]
    [InlineData(
        new byte[]
        { // Decimal ASCII values for 54686520717569636b2062726f776e20666f78206a756d7073206f76657220746865206c617a7920646f672e>
            53, 52, 54, 56, 54, 53, 50, 48, 55, 49,
            55, 53, 54, 57, 54, 51, 54, 98, 50, 48,
            54, 50, 55, 50, 54, 102, 55, 55, 54, 101,
            50, 48, 54, 54, 54, 102, 55, 56, 50, 48,
            54, 97, 55, 53, 54, 100, 55, 48, 55, 51,
            50, 48, 54, 102, 55, 54, 54, 53, 55, 50,
            50, 48, 55, 52, 54, 56, 54, 53, 50, 48,
            54, 99, 54, 49, 55, 97, 55, 57, 50, 48,
            54, 52, 54, 102, 54, 55, 50, 101, 62,
        },
        new byte[]
        { // Decimal ASCII values for "The quick brown fox jumps over the lazy dog."
            84, 104, 101, 32, 113, 117, 105, 99, 107, 32,
            98, 114, 111, 119, 110, 32, 102, 111, 120, 32,
            106, 117, 109, 112, 115, 32, 111, 118, 101, 114,
            32, 116, 104, 101, 32, 108, 97, 122, 121, 32,
            100, 111, 103, 46
        })]
    [InlineData(
        new byte[]
        { // Decimal ASCII values for 5468657365206172656e2774207468652064726f69647320796f75277265206c6f6f6b696e6720666f722e>
            53, 52, 54, 56, 54, 53, 55, 51, 54, 53,
            50, 48, 54, 49, 55, 50, 54, 53, 54, 101,
            50, 55, 55, 52, 50, 48, 55, 52, 54, 56,
            54, 53, 50, 48, 54, 52, 55, 50, 54, 102,
            54, 57, 54, 52, 55, 51, 50, 48, 55, 57,
            54, 102, 55, 53, 50, 55, 55, 50, 54, 53,
            50, 48, 54, 99, 54, 102, 54, 102, 54, 98,
            54, 57, 54, 101, 54, 55, 50, 48, 54, 54,
            54, 102, 55, 50, 50, 101, 62
        },
        new byte[]
        { // Decimal ASCII values for "These aren't the droids you're looking for."
            84, 104, 101, 115, 101, 32, 97, 114, 101, 110,
            39, 116, 32, 116, 104, 101, 32, 100, 114, 111,
            105, 100, 115, 32, 121, 111, 117, 39, 114, 101,
            32, 108, 111, 111, 107, 105, 110, 103, 32, 102,
            111, 114, 46
        })]
    public void DecodeProducesProperBinaryOutput(byte[] encoded, byte[] decoded)
    {
        var encodedStream = new MemoryStream(encoded);

        new ASCIIHexDecodeFilter()
            .Decode(encodedStream)
            .ToArray()
            .Should().BeEquivalentTo(decoded);
    }

    [Theory]
    [InlineData(
        new byte[]
        { // Decimal ASCII values for "she sells seashells on the sea shore"
            115, 104, 101, 32, 115, 101, 108, 108, 115, 32,
            115, 101, 97, 115, 104, 101, 108, 108, 115, 32,
            111, 110, 32, 116, 104, 101, 32, 115, 101, 97,
            32, 115, 104, 111, 114, 101
        },
        new byte[]
        { // Decimal ASCII values for 7368652073656C6C73207365617368656C6C73206F6E20746865207365612073686F7265>
            55, 51, 54, 56, 54, 53, 50, 48, 55, 51,
            54, 53, 54, 67, 54, 67, 55, 51, 50, 48,
            55, 51, 54, 53, 54, 49, 55, 51, 54, 56,
            54, 53, 54, 67, 54, 67, 55, 51, 50, 48,
            54, 70, 54, 69, 50, 48, 55, 52, 54, 56,
            54, 53, 50, 48, 55, 51, 54, 53, 54, 49,
            50, 48, 55, 51, 54, 56, 54, 70, 55, 50,
            54, 53, 62
        })]
    [InlineData(
        new byte[]
        { // Decimal ASCII values for "The quick brown fox jumps over the lazy dog."
            84, 104, 101, 32, 113, 117, 105, 99, 107, 32,
            98, 114, 111, 119, 110, 32, 102, 111, 120, 32,
            106, 117, 109, 112, 115, 32, 111, 118, 101, 114,
            32, 116, 104, 101, 32, 108, 97, 122, 121, 32,
            100, 111, 103, 46
        },
        new byte[]
        { // Decimal ASCII values for 54686520717569636B2062726F776E20666F78206A756D7073206F76657220746865206C617A7920646F672E>
            53, 52, 54, 56, 54, 53, 50, 48, 55, 49,
            55, 53, 54, 57, 54, 51, 54, 66, 50, 48,
            54, 50, 55, 50, 54, 70, 55, 55, 54, 69,
            50, 48, 54, 54, 54, 70, 55, 56, 50, 48,
            54, 65, 55, 53, 54, 68, 55, 48, 55, 51,
            50, 48, 54, 70, 55, 54, 54, 53, 55, 50,
            50, 48, 55, 52, 54, 56, 54, 53, 50, 48,
            54, 67, 54, 49, 55, 65, 55, 57, 50, 48,
            54, 52, 54, 70, 54, 55, 50, 69, 62,
        })]
    [InlineData(
        new byte[]
        { // Decimal ASCII values for "These aren't the droids you're looking for."
            84, 104, 101, 115, 101, 32, 97, 114, 101, 110,
            39, 116, 32, 116, 104, 101, 32, 100, 114, 111,
            105, 100, 115, 32, 121, 111, 117, 39, 114, 101,
            32, 108, 111, 111, 107, 105, 110, 103, 32, 102,
            111, 114, 46
        },
        new byte[]
        { // Decimal ASCII values for 5468657365206172656E2774207468652064726f69647320796F75277265206C6F6F6B696E6720666F722E>
            53, 52, 54, 56, 54, 53, 55, 51, 54, 53,
            50, 48, 54, 49, 55, 50, 54, 53, 54, 69,
            50, 55, 55, 52, 50, 48, 55, 52, 54, 56,
            54, 53, 50, 48, 54, 52, 55, 50, 54, 70,
            54, 57, 54, 52, 55, 51, 50, 48, 55, 57,
            54, 70, 55, 53, 50, 55, 55, 50, 54, 53,
            50, 48, 54, 67, 54, 70, 54, 70, 54, 66,
            54, 57, 54, 69, 54, 55, 50, 48, 54, 54,
            54, 70, 55, 50, 50, 69, 62
        })]
    public void EncodeProducesProperHexOutput(byte[] input, byte[] encoded)
    {
        using var inputBuffer = new MemoryStream(input);

        new ASCIIHexDecodeFilter()
            .Encode(inputBuffer)
            .ToArray()
            .Should().BeEquivalentTo(encoded);
    }
}
