using FluentAssertions;
using Xunit;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.IndirectObjects;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing
{
    public class DictionaryParserTests
    {
        [Fact]
        public async Task ParseEmptyAsync()
        {
            using var input = "<< >>".ToStream();

            var output = await Parser.For<Dictionary>().ParseAsync(input);

            output.Should().BeEmpty();
        }

        [Fact]
        public async Task ParseTrailerDictionaryAsync()
        {
            var contentString = "trailer\r\n<< /Size 50 /Root 49 0 R /Info 47 0 R " +
                "/ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>\r\n" +
                "startxref\r\n148076\r\n%%EOF\r\n";

            using var input = contentString.ToStream();

            var output = await Parser.For<Dictionary>().ParseAsync(input);

            output.Get<Integer>("Size");
            output.Get<IndirectObjectReference>("Root");
            output.Get<IndirectObjectReference>("Info");
            output.Get<ArrayObject>("ID").Should().NotBeNullOrEmpty();

        }
    }
}
