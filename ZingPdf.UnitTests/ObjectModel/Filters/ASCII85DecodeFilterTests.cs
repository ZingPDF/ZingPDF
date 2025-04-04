using FluentAssertions;
using System.Text;
using Xunit;
using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Filters;

public class ASCII85DecodeFilterTests
{
    [Fact]
    public void DecodeThrowsForNullData()
    {
        var action = () => new ASCII85DecodeFilter()
            .Decode(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DecodeThrowsWhenMissingEODMarker()
    {
        var action = () => new ASCII85DecodeFilter()
            .Decode("F(f,-F(K0(F!,O8@<6*nCi\"/8Df-\\>BOr<-ARQ^&BQ%p&".ToStream());

        action.Should().Throw<FilterInputFormatException>();
    }

    [Fact]
    public void DecodeThrowsForZInMiddleOf5CharacterSequence()
    {
        var action = () => new ASCII85DecodeFilter().Decode("qjzqo~>".ToStream());

        action.Should().Throw<FilterInputFormatException>();
    }

    [Fact]
    public void DecodeIgnoresSpacesInInput()
    {
        var input = "<+oiaAKYE%ASrl;+EV:.+CoM2Bk29-H#IgQEb-A    0Df9E*DJ()(DfRH~>".ToStream();
        var expected = Encoding.ASCII.GetBytes("These aren't the droids you're looking for.");

        var output = new ASCII85DecodeFilter().Decode(input);

        using var ms = new MemoryStream();
        output.CopyTo(ms);

        ms.ToArray().Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void DecodeReplacesZWithEmptyBytes()
    {
        var input = "9jqo^zBlbD-~>".ToStream();
        var expected = Encoding.ASCII.GetBytes("Man \0\0\0\0is d");

        var output = new ASCII85DecodeFilter().Decode(input);

        using var ms = new MemoryStream();
        output.CopyTo(ms);

        ms.ToArray().Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("F(f,-F(K0(F!,O8@<6*nCi\"/8Df-\\>BOr<-ARQ^&BQ%p&~>", "she sells seashells on the sea shore")]
    [InlineData("<+ohcEHPu*CER),Dg-(AAoDo:C3=B4F!,CEATAo8BOr<&@=!2AA8c*5~>", "The quick brown fox jumps over the lazy dog.")]
    [InlineData("<+oiaAKYE%ASrl;+EV:.+CoM2Bk29-H#IgQEb-A0Df9E*DJ()(DfRH~>", "These aren't the droids you're looking for.")]
    public void DecodeProducesProperOutput(string encoded, string decoded)
    {
        var input = encoded.ToStream();
        var expected = Encoding.ASCII.GetBytes(decoded);

        var output = new ASCII85DecodeFilter().Decode(input);

        using var ms = new MemoryStream();
        output.CopyTo(ms);

        ms.ToArray().Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("she sells seashells on the sea shore", "F(f,-F(K0(F!,O8@<6*nCi\"/8Df-\\>BOr<-ARQ^&BQ%p&~>")]
    [InlineData("The quick brown fox jumps over the lazy dog.", "<+ohcEHPu*CER),Dg-(AAoDo:C3=B4F!,CEATAo8BOr<&@=!2AA8c*5~>")]
    [InlineData("These aren't the droids you're looking for.", "<+oiaAKYE%ASrl;+EV:.+CoM2Bk29-H#IgQEb-A0Df9E*DJ()(DfRH~>")]
    public void EncodeProducesProperOutput(string unencoded, string encoded)
    {
        var input = unencoded.ToStream();
        var expected = Encoding.ASCII.GetBytes(encoded);

        var output = new ASCII85DecodeFilter().Encode(input);

        using var ms = new MemoryStream();
        output.CopyTo(ms);

        ms.ToArray().Should().BeEquivalentTo(expected);
    }
}
