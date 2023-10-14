using FluentAssertions;
using Xunit;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing
{
    public class NameParserTests
    {
        [Theory]
        [InlineData("/Size", "Size")]
        [InlineData("/Size 50", "Size")]
        [InlineData("/Type/Catalog", "Type")]
        [InlineData("/Pages 2 0 R", "Pages")]
        [InlineData("<</Type/Catalog/Pages 2 0 R", "Type")]
        [InlineData("2 0 obj\r\n<</Type/Pages/Count 3", "Type")]
        public async Task ParseBasicAsync(string content, string expected)
        {
            using var input = content.ToStream();

            Name expectedName = expected;

            var output = await Parser.For<Name>().ParseAsync(input);

            output.Should().BeEquivalentTo(expectedName);
        }
    }
}
