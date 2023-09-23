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

            action.Should().Throw<ArgumentException>();
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
