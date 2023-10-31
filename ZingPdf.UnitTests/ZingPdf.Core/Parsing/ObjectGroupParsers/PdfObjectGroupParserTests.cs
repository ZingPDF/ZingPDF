using FluentAssertions;
using Xunit;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.ObjectGroupParsers
{
    public class PdfObjectGroupParserTests
    {
        [Fact]
        public async Task ParseMultilineNameAndDictionary()
        {
            var contentString = "/Pattern <</P6 6 0 R\r\n" +
                "/P7 7 0 R\r\n" +
                "/P8 8 0 R\r\n" +
                "/P9 9 0 R>>\r\n";

            using var input = contentString.ToStream();

            var output = await new PdfObjectGroupParser().ParseAsync(input);

            output.Objects.Should().HaveCount(2);

            output.Get<Name>(0).Should().NotBeNull();
            output.Get<Dictionary>(1).Should().NotBeNull();
        }

        [Fact]
        public async Task ParseMultipleMultilineNamesAndDictionaries()
        {
            var contentString = "/ExtGState <</G3 3 0 R>>\r\n" +
                "/Pattern <</P6 6 0 R\r\n" +
                "/P7 7 0 R\r\n" +
                "/P8 8 0 R\r\n" +
                "/P9 9 0 R>>\r\n";

            using var input = contentString.ToStream();

            var output = await new PdfObjectGroupParser().ParseAsync(input);

            output.Objects.Should().HaveCount(4);

            output.Get<Name>(0).Should().NotBeNull();
            output.Get<Dictionary>(1).Should().NotBeNull();

            output.Get<Name>(2).Should().NotBeNull();
            output.Get<Dictionary>(3).Should().NotBeNull();
        }
    }
}
