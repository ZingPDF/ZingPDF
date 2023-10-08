using FluentAssertions;
using Xunit;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing
{
    public class DictionaryParserTests
    {
        [Fact]
        public void ParseEmptyDictionary()
        {
            Parser.For<Dictionary>()
                .Parse("<< >>")
                .Should()
                .BeEmpty();
        }

        [Fact]
        public void ParseTrailerDictionary()
        {
            Parser.For<Dictionary>()
                .Parse("trailer\r\n<< /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>\r\nstartxref\r\n148076\r\n%%EOF\r\n")
                .Should()
                .BeEquivalentTo(new Dictionary
                {
                    { "Size", new Integer(50) },
                    { "Root", new IndirectObjectReference(49, 0) },
                    { "Info", new IndirectObjectReference(47, 0) },
                    //{ "ID", new Objects.Primitives.Array() }
                }, options => options.Excluding(name => name.ByteOffset));
        }
    }
}
