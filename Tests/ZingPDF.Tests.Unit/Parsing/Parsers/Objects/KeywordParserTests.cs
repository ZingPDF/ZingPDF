using FluentAssertions;
using System.Text;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Syntax;
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

        Keyword expectedKeyword = new (expected, ObjectContext.None);

        var output = await new KeywordParser()
            .ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Should().BeEquivalentTo(expectedKeyword);

        input.Position.Should().Be(Encoding.ASCII.GetByteCount(content));
    }
}
