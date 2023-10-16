using FluentAssertions;
using Xunit;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Parsing;

namespace ZingPdf.UnitTests.ZingPdf.Core.Parsing
{
    public class KeywordParserTests
    {
        [Theory]
        [InlineData("startxref", "startxref")]
        [InlineData("\r\nstartxref", "startxref")]
        public async Task ParseBasicAsync(string content, string expected)
        {
            using var input = content.ToStream();

            Keyword expectedKeyword = expected;

            var output = await Parser.For<Keyword>().ParseAsync(input);

            output.Should().BeEquivalentTo(expectedKeyword);
        }
    }
}
