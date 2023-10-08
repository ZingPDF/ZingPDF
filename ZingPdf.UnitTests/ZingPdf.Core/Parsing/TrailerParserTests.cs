using FluentAssertions;
using Xunit;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Parsing.ObjectParsers;

namespace ZingPdf.Core.Parsing
{
    public class TrailerParserTests
    {
        [Fact]
        public void ParseBasic()
        {
            new TrailerParser()
                .Parse("trailer\r\n<< /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>\r\nstartxref\r\n148076\r\n%%EOF\r\n")
                .Should()
                .Be(new Trailer(new IndirectObjectReference(49, 0), 148076, new Objects.Primitives.Integer(50)));
        }
    }
}
