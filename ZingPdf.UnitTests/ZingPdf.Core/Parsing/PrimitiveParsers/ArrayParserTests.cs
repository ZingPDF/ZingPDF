using FluentAssertions;
using Xunit;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    public class ArrayParserTests
    {
        [Fact]
        public async Task ParseEmptyAsync()
        {
            using var input = "[]".ToStream();

            var output = await Parser.For<ArrayObject>()
                .ParseAsync(input);

            output.Should().BeEmpty();
        }

        [Theory]
        [InlineData("[ 10 ]", 1)]
        [InlineData("[ 10 20 ]", 2)]
        [InlineData("[ 10 20 30 ]", 3)]
        public async Task ParseCorrectCountsAsync(string content, int expectedCount)
        {
            using var input = content.ToStream();

            var output = await Parser.For<ArrayObject>()
                .ParseAsync(input);

            output.Should().HaveCount(expectedCount);
        }
    }
}
