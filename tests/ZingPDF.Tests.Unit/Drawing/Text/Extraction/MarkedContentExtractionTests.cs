using System.Text;
using FluentAssertions;
using Xunit;

namespace ZingPDF.Elements.Drawing.Text.Extraction;

public class MarkedContentExtractionTests
{
    [Theory]
    [InlineData("/P <</MCID 0>> BDC")]
    [InlineData("/P << /MCID 0 /Lang (en) /Nested << /Name /Value >> /Values [/Name (ignored) <0041>] >> BDC")]
    [InlineData("/P << /MCID 0 /Odd ] >> BDC")]
    [InlineData("/P << /MCID 0 >> BDC ] > )")]
    public async Task NamedMarkedContentProperties_DoNotHangOrHideFollowingText(string properties)
    {
        var content = Encoding.ASCII.GetBytes($"{properties} BT /F1 12 Tf 1 0 0 1 10 10 Tm (Invoice marker) Tj ET EMC");
        // A synchronous scanner regression must fail promptly rather than block the test thread forever.
        await Task.Run(async () =>
        {
            using var scanner = new LowLevelTextPageExtractor(ResolvedFontResourceSet.Empty);
            using var source = new MemoryStream(content);
            var runs = new List<TextRun>();
            await scanner.AppendTextRunsAsync(source, 1, runs);
            string.Concat(runs.Select(run => run.Text)).Should().Be("Invoice marker");
            var fonts = new HashSet<string>();
            await scanner.AppendUsedFontResourceNamesAsync(source, fonts);
            fonts.Should().ContainSingle().Which.Should().Be("F1");
            var plain = new StringBuilder();
            await scanner.AppendPlainTextAsync(source, plain);
            plain.ToString().Should().Be("Invoice marker");
        }).WaitAsync(TimeSpan.FromSeconds(5));
    }
}
