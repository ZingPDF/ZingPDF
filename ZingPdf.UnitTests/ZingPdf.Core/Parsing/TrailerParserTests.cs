using FluentAssertions;
using Xunit;
using ZingPdf.Core.Parsing.ObjectParsers;

namespace ZingPdf.Core.Parsing
{
    public class TrailerParserTests
    {
        [Fact]
        public void ParseBasic()
        {
            var input = "trailer\r\n<< /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>\r\nstartxref\r\n148076\r\n%%EOF\r\n";
            var output = new TrailerParser().Parse(input);

            output.Obj.ObjectCount.Should().Be(50);
            output.Obj.XrefTableByteOffset.Should().Be(148076);
        }
    }
}
