using FluentAssertions;
using Xunit;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    public class NameParserTests
    {
        [Theory]
        [InlineData("/Size ", "Size")]
        [InlineData("/Size 50", "Size")]
        [InlineData("/Type/Catalog", "Type")]
        [InlineData("/Pages 2 0 R", "Pages")]
        [InlineData("<</Type/Catalog/Pages 2 0 R", "Type")]
        [InlineData("/Page\r\n", "Page")]
        [InlineData("/DecodeParms<</Columns", "DecodeParms")]
        [InlineData("/Lang(en)", "Lang")]
        [InlineData("/Lang\n", "Lang")]
        public async Task ParseBasic_CorrectContent(string content, string expected)
        {
            using var input = content.ToStream();

            var output = await new NameParser().ParseAsync(input);

            output.Value.Should().Be(expected);
        }

        [Theory]
        [InlineData("/Size ", 5)]
        [InlineData("/Size 50", 5)]
        [InlineData("/Type/Catalog", 5)]
        [InlineData("/Pages 2 0 R", 6)]
        [InlineData("<</Type/Catalog/Pages 2 0 R", 7)]
        [InlineData("/Page\r\n", 5)]
        [InlineData("/DecodeParms<</Columns", 12)]
        [InlineData("/Lang(en)", 9)]
        [InlineData("/Lang\n", 5)]
        public async Task ParseBasic_CorrectStreamPosition(string content, int expectedPosition)
        {
            using var input = content.ToStream();

            var output = await new NameParser().ParseAsync(input);

            input.Position.Should().Be(expectedPosition);
        }
    }
}
