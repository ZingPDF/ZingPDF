using FluentAssertions;
using Xunit;

namespace ZingPdf.Core.Parsing
{
    public class ArrayParserTests
    {
        [Fact]
        public void ParseEmpty()
        {
            Parser.For<Objects.Primitives.Array>()
                .Parse("[]")
                .Obj
                .Should().BeEmpty();
        }

        [Theory]
        [InlineData("[ 10 ]", 1)]
        [InlineData("[ 10 20 ]", 2)]
        [InlineData("[ 10 20 30 ]", 3)]
        public void ParseCorrectCounts(string content, int expectedCount)
        {
            Parser.For<Objects.Primitives.Array>()
                .Parse(content)
                .Obj
                .Should().HaveCount(expectedCount);
        }
    }
}
