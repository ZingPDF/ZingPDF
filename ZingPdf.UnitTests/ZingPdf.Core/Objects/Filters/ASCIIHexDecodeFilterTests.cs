using FluentAssertions;
using System.Text;
using Xunit;

namespace ZingPdf.Core.Objects.Filters
{
    public class ASCIIHexDecodeFilterTests
    {
        [Fact]
        public void DecodeThrowsWhenMissingEODMarker()
        {
            var action = () => new ASCIIHexDecodeFilter()
                .Decode("7368652073656C6C73207365617368656C6C73206F6E20746865207365612073686F7265");

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void DecodeThrowsForCharactersFollowingEODMarker()
        {
            var action = () => new ASCIIHexDecodeFilter()
                .Decode("7368652073656C6C73207365617368656C6C73206F6E20746865207365612073686F7265> blah");

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void DecodeIgnoresSpacesInEncodedData()
        {
            new ASCIIHexDecodeFilter()
                .Decode("736865 20 73656C6C73207365617368656C6C73206F6E20746865207365612073686F7265>")
                .Should().BeEquivalentTo(Encoding.ASCII.GetBytes("she sells seashells on the sea shore"));
        }

        [Fact]
        public void DecodePadsOddLengthInEncodedData()
        {
            new ASCIIHexDecodeFilter()
                .Decode("7368652073656C6C73207365617368656C6C73206F6E20746865207365612073686F72652>")
                .Should().BeEquivalentTo(Encoding.ASCII.GetBytes("she sells seashells on the sea shore "));
        }

        [Theory]
        [InlineData("7368652073656C6C73207365617368656C6C73206F6E20746865207365612073686F7265>", "she sells seashells on the sea shore")]
        [InlineData("54686520717569636b2062726f776e20666f78206a756d7073206f76657220746865206c617a7920646f672e>", "The quick brown fox jumps over the lazy dog.")]
        [InlineData("5468657365206172656e2774207468652064726f69647320796f75277265206c6f6f6b696e6720666f722e>", "These aren't the droids you're looking for.")]
        public void DecodeProducesProperBinaryOutput(string encoded, string decoded)
        {
            new ASCIIHexDecodeFilter()
                .Decode(encoded)
                .Should().BeEquivalentTo(Encoding.ASCII.GetBytes(decoded));
        }

        [Theory]
        [InlineData("she sells seashells on the sea shore", "7368652073656C6C73207365617368656C6C73206F6E20746865207365612073686F7265>")]
        [InlineData("The quick brown fox jumps over the lazy dog.", "54686520717569636b2062726f776e20666f78206a756d7073206f76657220746865206c617a7920646f672e>")]
        [InlineData("These aren't the droids you're looking for.", "5468657365206172656e2774207468652064726f69647320796f75277265206c6f6f6b696e6720666f722e>")]
        public void EncodeProducesProperHexOutput(string input, string encoded)
        {
            new ASCIIHexDecodeFilter()
                .Encode(Encoding.ASCII.GetBytes(input))
                .Should().BeEquivalentTo(encoded);
        }
    }
}
