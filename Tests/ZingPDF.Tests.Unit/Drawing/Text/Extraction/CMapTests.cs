using FluentAssertions;
using System.Text;
using Xunit;
using ZingPDF.Elements.Drawing.Text.Extraction.CmapParsing;

namespace ZingPDF.Elements.Drawing.Text.Extraction;

public class CMapTests
{
    [Fact]
    public void TryReadMatch_PrefersLongestMatchingCode()
    {
        var cmap = new CMap();
        cmap.AddMapping([0x01], "A");
        cmap.AddMapping([0x01, 0x02], "B");

        var matched = cmap.TryReadMatch([0x01, 0x02], out var mapped, out var bytesConsumed);

        matched.Should().BeTrue();
        mapped.Should().Be("B");
        bytesConsumed.Should().Be(2);
    }

    [Fact]
    public void Parse_RegistersCodeSpaceLengthsForFallbackDecoding()
    {
        const string cmapSource = """
            1 begincodespacerange
            <0000> <FFFF>
            1 beginbfchar
            <0041> <0042>
            """;

        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(cmapSource));

        var cmap = CMapParser.Parse(stream);

        cmap.GetFallbackCodeLength(4).Should().Be(2);
    }

    [Fact]
    public void Parse_ExpandsSequentialBfRangeMappings()
    {
        const string cmapSource = """
            1 beginbfrange
            <0001> <0003> <0041>
            """;

        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(cmapSource));

        var cmap = CMapParser.Parse(stream);

        cmap.Map([0x00, 0x01]).Should().Be("A");
        cmap.Map([0x00, 0x02]).Should().Be("B");
        cmap.Map([0x00, 0x03]).Should().Be("C");
    }

    [Fact]
    public void Parse_ExpandsBracketedBfRangeMappings()
    {
        const string cmapSource = """
            1 beginbfrange
            <0001> <0003> [
            <0041> <0042> <0043>
            ]
            """;

        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(cmapSource));

        var cmap = CMapParser.Parse(stream);

        cmap.Map([0x00, 0x01]).Should().Be("A");
        cmap.Map([0x00, 0x02]).Should().Be("B");
        cmap.Map([0x00, 0x03]).Should().Be("C");
    }
}
