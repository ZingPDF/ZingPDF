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
        [InlineData("[<2B551D2AFE52654494F9720283CFF1C4><3CDA8BB6D5834E41A5E2AA16C35E4C47>]/Index", 2)]
        public async Task ParseCorrectCountsAsync(string content, int expectedCount)
        {
            using var input = content.ToStream();

            var output = await Parser.For<ArrayObject>()
                .ParseAsync(input);

            output.Should().HaveCount(expectedCount);
        }

        [Fact]
        public async Task ParseArrayOfIntegersMultiline()
        {
            // During parsing, the TokenTypeIdentifier must not mistake the line ending
            // and the tokens preceding it for a cross reference section index.
            var contentString = "[ 1 52\r\n" +
                " 1 54 1 56 1 58 1 60 1 62 1 64 1 66 1 69 1 71 ]";

            using var input = contentString.ToStream();

            var output = await Parser.For<ArrayObject>()
                .ParseAsync(input);

            output.All(x => x is Integer).Should().BeTrue();
        }
    }
}
