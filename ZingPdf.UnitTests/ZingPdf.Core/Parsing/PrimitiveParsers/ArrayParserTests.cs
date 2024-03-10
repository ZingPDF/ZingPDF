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

            var output = await new ArrayParser()
                .ParseAsync(input);

            output.Should().BeEmpty();
            input.Position.Should().Be(input.Length);
        }

        [Fact]
        public async Task ParseNestedArray()
        {
            using var input = "[[(2020-12-03_ISO_32000-2-final.pdf)90827 0 R]]".ToStream();

            var output = await new ArrayParser()
                .ParseAsync(input);

            output.Should().HaveCount(1);
            output.First().Should().BeOfType<ArrayObject>();
            output.First().As<ArrayObject>().Should().HaveCount(2);

            input.Position.Should().Be(input.Length);
        }

        [Theory]
        [InlineData("[ 10 ]", 1)]
        [InlineData("[ 10 20 ]", 2)]
        [InlineData("[ 10 20 30 ]", 3)]
        [InlineData("[90793 1014]", 2)]
        [InlineData("[0 0 594.95996 841.91998]", 4)]
        [InlineData("[<2B551D2AFE52654494F9720283CFF1C4><3CDA8BB6D5834E41A5E2AA16C35E4C47>]/Index", 2)]
        public async Task ParseCorrectCountsAsync(string content, int expectedCount)
        {
            using var input = content.ToStream();

            var output = await new ArrayParser()
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

            var output = await new ArrayParser()
                .ParseAsync(input);

            output.All(x => x is Integer).Should().BeTrue();
        }

        [Fact]
        public async Task ParseArrayOfHexValues()
        {
            var contentString = "[<81b14aafa313db63dbd6f981e49f94f4>\r\n" +
                "<81b14aafa313db63dbd6f981e49f94f4>\r\n" +
                "]\r\n";

            using var input = contentString.ToStream();

            var output = await new ArrayParser()
                .ParseAsync(input);

            output.Count().Should().Be(2);
            output.All(x => x is HexadecimalString).Should().BeTrue();
            
            input.Position.Should().Be(74, because: "the parser should move the stream past the string-end delimiter");
        }
    }
}
