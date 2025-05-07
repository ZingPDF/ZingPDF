using FakeItEasy;
using FluentAssertions;
using System.Text;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects;

public class KeywordParserTests
{
    [Theory]
    [InlineData("startxref", "startxref")]
    [InlineData("\r\nstartxref", "startxref")]
    [InlineData("\rendobj", "endobj")]
    public async Task ParseBasicAsync(string content, string expected)
    {
        using var input = content.ToStream();

        Keyword expectedKeyword = expected;

        var output = await new KeywordParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.None));

        output.Should().BeEquivalentTo(expectedKeyword);

        input.Position.Should().Be(Encoding.ASCII.GetByteCount(content));
    }
}
