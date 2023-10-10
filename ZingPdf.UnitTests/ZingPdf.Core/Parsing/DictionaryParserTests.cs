using FluentAssertions;
using Xunit;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.UnitTests;

namespace ZingPdf.Core.Parsing
{
    public class DictionaryParserTests
    {
        [Fact]
        public void ParseEmpty()
        {
            Parser.For<Dictionary>()
                .Parse("<< >>")
                .Obj
                .Should()
                .BeEmpty();
        }

        [Fact]
        public void ParseTrailerDictionary()
        {
            var input = "trailer\r\n<< /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>\r\nstartxref\r\n148076\r\n%%EOF\r\n";

            var output = Parser.For<Dictionary>()
                .Parse(input);

            output.Obj.Get<Integer>("Size");
            output.Obj.Get<IndirectObjectReference>("Root");
            output.Obj.Get<IndirectObjectReference>("Info");
            output.Obj.Get<Objects.Primitives.Array>("ID").Should().NotBeNullOrEmpty();

        }
    }
}
