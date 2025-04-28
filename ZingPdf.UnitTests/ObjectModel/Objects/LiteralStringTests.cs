using FluentAssertions;
using System.ComponentModel;
using System.Text;
using Xunit;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text.Encoding.PDFDocEncoding;

namespace ZingPDF.Syntax.Objects;

public class LiteralStringTests
{
    static LiteralStringTests()
    {
        Encoding.RegisterProvider(PDFDocEncodingProvider.Instance);
    }

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
