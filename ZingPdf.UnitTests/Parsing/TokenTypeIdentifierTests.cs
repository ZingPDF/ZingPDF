using FluentAssertions;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Objects.Primitives;
using ZingPDF.Objects.Primitives.IndirectObjects;

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
    public async Task TryIdentifyBasicAsync(string token, Type expectedType)
    {
        using var input = token.ToStream();

        var output = await TokenTypeIdentifier.TryIdentifyAsync(input);

        output.Should().Be(expectedType);
    }
}
