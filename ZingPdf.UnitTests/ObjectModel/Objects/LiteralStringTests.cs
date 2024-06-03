using FluentAssertions;
using System.ComponentModel;
using Xunit;

namespace ZingPDF.ObjectModel.Objects;

public class LiteralStringTests
{
    [Fact]
    public void ConstructorThrowsForUnsupportedEncoding()
    {
        var act = () => new LiteralString("test", (LiteralStringEncoding)99);

        act.Should().Throw<InvalidEnumArgumentException>().WithParameterName("encodeUsing");
    }

    [Theory]
    [InlineData(LiteralStringEncoding.UTF8)]
    [InlineData(LiteralStringEncoding.UTF16BE)]
    [InlineData(LiteralStringEncoding.PDFDocEncoding)]
    internal async Task WriteAsyncProducesCorrectByteOrderMarks(LiteralStringEncoding encoding)
    {
        var literalString = new LiteralString("test", encoding);

        using var ms = new MemoryStream();

        await literalString.WriteAsync(ms);

        // Skip the opening parenthesis
        ms.Position = 1;

        foreach (var c in literalString.GetEncodingPreamble())
        {
            ms.ReadByte().Should().Be(c);
        }
    }
}
