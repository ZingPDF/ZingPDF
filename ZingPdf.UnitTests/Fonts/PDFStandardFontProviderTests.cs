using FluentAssertions;
using Xunit;
using ZingPDF.Fonts.FontProviders;

namespace ZingPDF.Fonts;

public class PDFStandardFontProviderTests
{
    [Fact]
    public void GetFontMetrics_ReturnsExpectedMetrics()
    {
        var metrics = new PDFStandardFontMetricsProvider().GetFontMetrics(PDFStandardFontMetricsProvider.FontNames.Helvetica);

        metrics.Name.Should().Be(PDFStandardFontMetricsProvider.FontNames.Helvetica);
        metrics.Ascent.Should().Be(718);
        metrics.Descent.Should().Be(-207);
        metrics.CapHeight.Should().Be(718);
        metrics.XHeight.Should().Be(523);
        metrics.ItalicAngle.Should().Be(0);
        metrics.IsFixedPitch.Should().BeFalse();
        metrics.UnderlinePosition.Should().Be(-100);
        metrics.UnderlineThickness.Should().Be(50);

        metrics.Widths.Should().HaveCount(149);
        metrics.KerningPairs.Should().HaveCount(138);
    }

    [Fact]
    public void GetFontMetrics_ThrowsForInvalidFont()
    {
        Assert.Throws<FontNotFoundException>(() => new PDFStandardFontMetricsProvider().GetFontMetrics("SpacePants"));
    }
}
