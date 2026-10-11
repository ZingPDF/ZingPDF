using System.Globalization;
using System.Text;
using FluentAssertions;
using Xunit;

namespace ZingPDF.Elements.Drawing.Text.Extraction;

public sealed class PlainTextAcrossContentStreamsTests
{
    [Fact]
    public async Task AppendPlainTextAsync_InsertsLineBreakAcrossContentStreams()
    {
        using var scanner = new LowLevelTextPageExtractor(ResolvedFontResourceSet.Empty);
        var destination = new StringBuilder();
        var collector = scanner.CreatePlainTextCollector(destination);
        using var first = CreateTextStream("First line", 10, 100);
        using var second = CreateTextStream("Second line", 10, 80);

        await scanner.AppendPlainTextAsync(first, collector);
        await scanner.AppendPlainTextAsync(second, collector);

        destination.ToString().Should().Be("First line\nSecond line");
    }

    [Fact]
    public async Task AppendPlainTextAsync_PreservesSameLineSpacingAcrossContentStreams()
    {
        using var measuringScanner = new LowLevelTextPageExtractor(ResolvedFontResourceSet.Empty);
        using var firstForMeasure = CreateTextStream("Alpha", 10, 100);
        var runs = new List<TextRun>();
        await measuringScanner.AppendTextRunsAsync(firstForMeasure, 1, runs);
        var endX = runs.Should().ContainSingle().Which.EndX;

        using var scanner = new LowLevelTextPageExtractor(ResolvedFontResourceSet.Empty);
        var destination = new StringBuilder();
        var collector = scanner.CreatePlainTextCollector(destination);
        using var first = CreateTextStream("Alpha", 10, 100);
        using var second = CreateTextStream("Beta", endX + 10, 100);

        await scanner.AppendPlainTextAsync(first, collector);
        await scanner.AppendPlainTextAsync(second, collector);

        destination.ToString().Should().Be("Alpha Beta");
    }

    [Fact]
    public async Task AppendPlainTextAsync_DoesNotAddSpaceForContiguousSameLineAcrossContentStreams()
    {
        using var measuringScanner = new LowLevelTextPageExtractor(ResolvedFontResourceSet.Empty);
        using var firstForMeasure = CreateTextStream("Alpha", 10, 100);
        var runs = new List<TextRun>();
        await measuringScanner.AppendTextRunsAsync(firstForMeasure, 1, runs);
        var endX = runs.Should().ContainSingle().Which.EndX;

        using var scanner = new LowLevelTextPageExtractor(ResolvedFontResourceSet.Empty);
        var destination = new StringBuilder();
        var collector = scanner.CreatePlainTextCollector(destination);
        using var first = CreateTextStream("Alpha", 10, 100);
        using var second = CreateTextStream("Beta", endX, 100);

        await scanner.AppendPlainTextAsync(first, collector);
        await scanner.AppendPlainTextAsync(second, collector);

        destination.ToString().Should().Be("AlphaBeta");
    }

    private static MemoryStream CreateTextStream(string text, float x, float y)
    {
        var content = FormattableString.Invariant($"BT /F1 12 Tf 1 0 0 1 {x:R} {y:R} Tm ({text}) Tj ET");
        return new MemoryStream(Encoding.ASCII.GetBytes(content));
    }
}
