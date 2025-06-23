using FakeItEasy;
using FluentAssertions;
using System.Text;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers.Objects;

public class HexadecimalStringParserTests
{
    [Theory]
    [InlineData("<66dbd809c84b6f6bd19bb2f8865b77cc>", "66dbd809c84b6f6bd19bb2f8865b77cc")]
    [InlineData(" <66dbd809c84b6f6bd19bb2f8865b77cc>", "66dbd809c84b6f6bd19bb2f8865b77cc")]
    public async Task ParseBasicAsync(string content, string expected)
    {
        using var input = content.ToStream();

        HexadecimalString expectedHexString = expected;

        var output = await new HexadecimalStringParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.None));

        output.Should().BeEquivalentTo(expectedHexString);
    }

    [Theory]
    [InlineData("<FEFF0044003A00320030003200340031003100310038003000320033003600310033005A>")]
    [InlineData("<FEFF0078006D0070002E006400690064003A00630036003800330035003900300034002D0032006500660035002D0034003400300036002D0061003700310036002D006600640033006100360035006100370065003700310065>")]
    public async Task Parse_CorrectStreamPosition(string content)
    {
        using var input = content.ToStream();

        var output = await new HexadecimalStringParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.None));

        input.Position.Should().Be(Encoding.UTF8.GetByteCount(content));
    }
}
