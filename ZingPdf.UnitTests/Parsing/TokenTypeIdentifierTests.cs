using FluentAssertions;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Parsing;

public class TokenTypeIdentifierTests
{
    [Theory]
    [InlineData("/Name", typeof(Name))]
    [InlineData(" /Name", typeof(Name))]
    [InlineData("<<>>", typeof(Dictionary))]
    [InlineData(" <<>>", typeof(Dictionary))]
    [InlineData("[]", typeof(ArrayObject))]
    [InlineData(" []", typeof(ArrayObject))]
    [InlineData("49 0 R", typeof(IndirectObjectReference))]
    [InlineData(" 49 0 R", typeof(IndirectObjectReference))]
    [InlineData("123456", typeof(Integer))]
    [InlineData(" 123456", typeof(Integer))]
    [InlineData("123.456", typeof(RealNumber))]
    [InlineData(" 123.456", typeof(RealNumber))]
    [InlineData("0.000000", typeof(RealNumber))]
    [InlineData(" ", null)]
    [InlineData("<4E6F762073686D6F7A206B6120706F702E>", typeof(HexadecimalString))]
    [InlineData("<</ArtBox[0.0 0.0 841.89 595.276]/BleedBox[0.0 0.0 841.89 595.276]/", typeof(Dictionary))]
    public async Task TryIdentify_Basic(string token, Type expectedType)
    {
        using var input = token.ToStream();

        var output = await TokenTypeIdentifier.TryIdentifyAsync(input);

        output.Should().Be(expectedType);

        input.Position.Should().Be(0);
    }

    [Theory]
    [InlineData("\rendobj", typeof(Keyword))]
    [InlineData("\r\nendobj", typeof(Keyword))]
    public async Task TryIdentify_LeadingWhitespace(string token, Type expectedType)
    {
        using var input = token.ToStream();

        var output = await TokenTypeIdentifier.TryIdentifyAsync(input);

        output.Should().Be(expectedType);

        input.Position.Should().Be(0);
    }

    [Theory]
    [InlineData("endobj\r", typeof(Keyword))]
    [InlineData("endobj ", typeof(Keyword))]
    [InlineData("\rendobj\r", typeof(Keyword))]
    [InlineData("\r\nendobj\r", typeof(Keyword))]
    [InlineData("endobj\r\n", typeof(Keyword))]
    [InlineData("\rendobj\r\n", typeof(Keyword))]
    [InlineData("\r\nendobj\r\n", typeof(Keyword))]
    public async Task TryIdentify_TrailingWhitespace(string token, Type expectedType)
    {
        using var input = token.ToStream();

        var output = await TokenTypeIdentifier.TryIdentifyAsync(input);

        output.Should().Be(expectedType);

        input.Position.Should().Be(0);
    }
}
