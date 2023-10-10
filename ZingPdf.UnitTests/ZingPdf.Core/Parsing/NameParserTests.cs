using FluentAssertions;
using Xunit;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing
{
    public class NameParserTests
    {
        [Theory]
        [InlineData("/Size", "Size", "")]
        [InlineData("/Size 50", "Size", " 50")]
        [InlineData("/Type/Catalog", "Type", "/Catalog")]
        [InlineData("/Pages 2 0 R", "Pages", " 2 0 R")]
        [InlineData("<</Type/Catalog/Pages 2 0 R", "Type", "/Catalog/Pages 2 0 R")]
        [InlineData("2 0 obj\r\n<</Type/Pages/Count 3", "Type", "/Pages/Count 3")]
        public void ParseBasic(string content, string expected, string remainingContent)
        {
            ParseResult<Name> expectedName = new(expected, remainingContent);

            Parser.For<Name>()
                .Parse(content)
                .Should()
                .BeEquivalentTo(expectedName, options => options.Excluding(name => name.Obj.ByteOffset));
        }
    }
}
