using FluentAssertions;
using System.Text;
using Xunit;

namespace ZingPdf.Core.Objects.Filters
{
    public class ASCII85DecodeFilterTests
    {
        [Fact]
        public void DecodeThrowsWhenMissingEODMarker()
        {
            var action = () => new ASCII85DecodeFilter()
                .Decode("F(f,-F(K0(F!,O8@<6*nCi\"/8Df-\\>BOr<-ARQ^&BQ%p&");

            action.Should().Throw<FilterInputFormatException>();
        }

        [Fact]
        public void DecodeThrowsForZInMiddleOf5CharacterSequence()
        {
            var action = () => new ASCII85DecodeFilter().Decode("qjzqo~>");

            action.Should().Throw<FilterInputFormatException>();
        }

        [Fact]
        public void DecodeIgnoresSpacesInInput()
        {
            new ASCII85DecodeFilter()
                .Decode("<+oiaAKYE%ASrl;+EV:.+CoM2Bk29-H#IgQEb-A    0Df9E*DJ()(DfRH~>")
                .Should().BeEquivalentTo(Encoding.ASCII.GetBytes("These aren't the droids you're looking for."));
        }

        [Fact]
        public void DecodeReplacesZWithEmptyBytes()
        {
            new ASCII85DecodeFilter()
                .Decode("9jqo^zBlbD-~>")
                .Should().BeEquivalentTo(Encoding.ASCII.GetBytes("Man \0\0\0\0is d"));
        }

        [Theory]
        [InlineData("F(f,-F(K0(F!,O8@<6*nCi\"/8Df-\\>BOr<-ARQ^&BQ%p&~>", "she sells seashells on the sea shore")]
        [InlineData("<+ohcEHPu*CER),Dg-(AAoDo:C3=B4F!,CEATAo8BOr<&@=!2AA8c*5~>", "The quick brown fox jumps over the lazy dog.")]
        [InlineData("<+oiaAKYE%ASrl;+EV:.+CoM2Bk29-H#IgQEb-A0Df9E*DJ()(DfRH~>", "These aren't the droids you're looking for.")]
        public void DecodeProducesProperOutput(string encoded, string decoded)
        {
            new ASCII85DecodeFilter()
                .Decode(encoded)
                .Should().BeEquivalentTo(Encoding.ASCII.GetBytes(decoded));
        }

        [Theory]
        [InlineData("she sells seashells on the sea shore", "F(f,-F(K0(F!,O8@<6*nCi\"/8Df-\\>BOr<-ARQ^&BQ%p&~>")]
        [InlineData("The quick brown fox jumps over the lazy dog.", "<+ohcEHPu*CER),Dg-(AAoDo:C3=B4F!,CEATAo8BOr<&@=!2AA8c*5~>")]
        [InlineData("These aren't the droids you're looking for.", "<+oiaAKYE%ASrl;+EV:.+CoM2Bk29-H#IgQEb-A0Df9E*DJ()(DfRH~>")]
        public void EncodeProducesProperOutput(string input, string output)
        {
            new ASCII85DecodeFilter()
                .Encode(Encoding.ASCII.GetBytes(input))
                .Should().Be(output);
        }
    }
}
