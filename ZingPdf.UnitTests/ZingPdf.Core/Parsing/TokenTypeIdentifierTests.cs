using FluentAssertions;
using Xunit;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing
{
    public class TokenTypeIdentifierTests
    {
        [Theory]
        [InlineData("/Name", true, typeof(Name))]
        [InlineData(" /Name", true, typeof(Name))]
        [InlineData("<<>>", true, typeof(Dictionary))]
        [InlineData(" <<>>", true, typeof(Dictionary))]
        [InlineData("[]", true, typeof(Objects.Primitives.Array))]
        [InlineData(" []", true, typeof(Objects.Primitives.Array))]
        [InlineData("49 0 R", true, typeof(IndirectObjectReference))]
        [InlineData(" 49 0 R", true, typeof(IndirectObjectReference))]
        [InlineData("123456", true, typeof(Integer))]
        [InlineData(" 123456", true, typeof(Integer))]
        [InlineData(" ", false, null)]
        [InlineData("<4E6F762073686D6F7A206B6120706F702E>", true, typeof(HexadecimalString))]
        public void TryIdentifyBasic(string token, bool identified, Type expectedType)
        {
            TokenTypeIdentifier.TryIdentify(token, out var type).Should().Be(identified);

            type.Should().Be(expectedType);
        }
    }
}
