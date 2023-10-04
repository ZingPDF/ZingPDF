using FluentAssertions;
using Xunit;

namespace ZingPdf.Core.Parsing
{
    public class DictionaryParserTests
    {
        [Fact]
        public void ParseEmptyDictionary()
        {
            new DictionaryParser()
                .Parse(new[] { "<<",  ">>" })
                .Should()
                .BeEmpty();
        }
    }
}
