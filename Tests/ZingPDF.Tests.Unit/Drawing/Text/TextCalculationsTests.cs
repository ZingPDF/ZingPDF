using FakeItEasy;
using FluentAssertions;
using Xunit;
using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.Syntax.CommonDataStructures;

namespace ZingPDF.Elements.Drawing.Text;

public class TextCalculationsTests
{
    [Theory]
    [InlineData("Helvetica", 209.821, 22, "t1", 14.82)] // /Tx BMC q 1 1 207.821 20 re W n BT /Helv 14.82 Tf 0 g 2 5.5017 Td (t1) Tj ET Q EMC
    [InlineData("Helvetica", 209.821, 22, "mmmmmmmmmmmmmmmmm", 14.512)] //14.82 - 0.308
    [InlineData("Helvetica", 209.821, 22, "mmmmmmmmmmmmmmmmlll", 14.686)] //14.82 - 0.134
    [InlineData("Helvetica", 232.44, 14.04, "t2", 8.921)] // /Tx BMC BT /Helv 10.3945 Tf /Helv 8.921 Tf 0 g 2 3.7103 Td (t2) Tj ET EMC
    [InlineData("Helvetica", 232.44, 14.04, "mmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm", 4)]
    [InlineData("Helvetica", 232.44, 14.04, "mmmmmmmmmmmmmmmmmmmmmmmmmmm", 8.921)]
    [InlineData("Helvetica", 209.821, 22, "finnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn", 4)]
    public void CalculateFontSize(string fontName, double width, double height, string textValue, double expectedFontSize)
    {
        var fontProvider = new PDFStandardFontMetricsProvider();

        var textFit = new TextCalculations([fontProvider])
            .CalculateTextFit(fontName, Rectangle.FromDimensions(width, height), textValue);

        textFit.FontSize.Should().BeApproximately(expectedFontSize, 0.02);
    }

    [Theory]
    [InlineData("Helvetica", 209.821, 22, "t1", 5.5017)] // /Tx BMC q 1 1 207.821 20 re W n BT /Helv 14.82 Tf 0 g 2 5.5017 Td (t1) Tj ET Q EMC
    [InlineData("Helvetica", 209.821, 22, "mmmmmmmmmmmmmmmmm", 5.616)]
    [InlineData("Helvetica", 232.44, 14.04, "t2", 3.7103)] // /Tx BMC BT /Helv 10.3945 Tf /Helv 8.921 Tf 0 g 2 3.7103 Td (t2) Tj ET EMC
    [InlineData("Helvetica", 232.44, 14.04, "mmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm", 5.536)]
    [InlineData("Helvetica", 232.44, 14.04, "mmmmmmmmmmmmmmmmmmmmmmmmmmm", 3.7103)]
    [InlineData("Helvetica", 209.821, 22, "finnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn", 9.516)]
    public void CalculateTextOrigin(string fontName, double width, double height, string textValue, double expectedBaselineOffset)
    {
        var fontProvider = new PDFStandardFontMetricsProvider();

        var textFit = new TextCalculations([fontProvider])
            .CalculateTextFit(fontName, Rectangle.FromDimensions(width, height), textValue);

        textFit.TextOrigin.X.Should().Be(2); // TODO: incorporate quadding
        textFit.TextOrigin.Y.Should().BeApproximately(expectedBaselineOffset, 0.02);
    }

    [Fact]
    public void CalculateTextFit_WithExplicitFontSize_PreservesRequestedSizeForVerticalPlacement()
    {
        var fontProvider = CreateFontMetricsProvider(
            "Montserrat-Regular",
            new FontMetrics
            {
                Name = "Montserrat-Regular",
                Ascent = 968,
                Descent = -251,
                CapHeight = 729,
                XHeight = 566
            },
            measuredWidth: 16);

        var textFit = new TextCalculations([fontProvider])
            .CalculateTextFit(
                "Montserrat-Regular",
                Rectangle.FromDimensions(285.717, 13.3132),
                "Zip",
                new TextFitOptions
                {
                    RequestedFontSize = 10
                });

        textFit.FontSize.Should().Be(10);
        textFit.TextOrigin.X.Should().Be(2);
        textFit.TextOrigin.Y.Should().BeApproximately(2.6415, 0.02);
    }

    [Theory]
    [InlineData(1, 30)]
    [InlineData(2, 58)]
    public void CalculateTextFit_HonoursQuadding(int quadding, double expectedX)
    {
        var fontProvider = CreateFontMetricsProvider(
            "Helvetica",
            new FontMetrics
            {
                Name = "Helvetica",
                Ascent = 718,
                Descent = -207,
                CapHeight = 718,
                XHeight = 523
            },
            measuredWidth: 40);

        var textFit = new TextCalculations([fontProvider])
            .CalculateTextFit(
                "Helvetica",
                Rectangle.FromDimensions(100, 20),
                "test",
                new TextFitOptions
                {
                    RequestedFontSize = 10,
                    Quadding = quadding
                });

        textFit.TextOrigin.X.Should().BeApproximately(expectedX, 0.001);
    }

    [Fact]
    public void CalculateTextFit_FallsBackToCapHeightWhenXHeightIsMissing()
    {
        var fontProvider = CreateFontMetricsProvider(
            "CustomFont",
            new FontMetrics
            {
                Name = "CustomFont",
                Ascent = 900,
                Descent = -200,
                CapHeight = 700,
                XHeight = 0
            },
            measuredWidth: 40);

        var textFit = new TextCalculations([fontProvider])
            .CalculateTextFit(
                "CustomFont",
                Rectangle.FromDimensions(100, 20),
                "test",
                new TextFitOptions
                {
                    RequestedFontSize = 10
                });

        textFit.TextOrigin.Y.Should().BeApproximately(6.2775, 0.02);
    }

    [Fact]
    public void CalculateTextLayout_WithMultiline_WrapsTextAndRespectsBoundingBoxOrigin()
    {
        var fontProvider = CreateMonospaceFontMetricsProvider(
            "Mono",
            defaultWidth: 500,
            spaceWidth: 500,
            ascent: 800,
            descent: -200,
            capHeight: 700,
            xHeight: 500);

        var layout = new TextCalculations([fontProvider])
            .CalculateTextLayout(
                "Mono",
                Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(10, 20), new ZingPDF.Elements.Drawing.Coordinate(50, 80)),
                "alpha beta gamma",
                new TextFitOptions
                {
                    RequestedFontSize = 10,
                    IsMultiline = true
                });

        layout.FontSize.Should().Be(10);
        layout.Segments.Select(x => x.Text).Should().Equal("alpha", "beta", "gamma");
        layout.Segments[0].Origin.X.Should().BeApproximately(12, 0.001);
        layout.Segments[0].Origin.Y.Should().BeApproximately(70, 0.001);
        layout.Segments[1].Origin.Y.Should().BeApproximately(58, 0.001);
        layout.Segments[2].Origin.Y.Should().BeApproximately(46, 0.001);
    }

    [Fact]
    public void CalculateTextLayout_WithMultilineAutoSize_ShrinksToFitAvailableHeight()
    {
        var fontProvider = CreateMonospaceFontMetricsProvider(
            "Mono",
            defaultWidth: 500,
            spaceWidth: 500,
            ascent: 800,
            descent: -200,
            capHeight: 700,
            xHeight: 500);

        var boundingBox = Rectangle.FromDimensions(60, 30);
        var initialAutoFontSize = Math.Max(4, ((double)boundingBox.Height - 2) * 0.741);

        var layout = new TextCalculations([fontProvider])
            .CalculateTextLayout(
                "Mono",
                boundingBox,
                "alpha beta gamma delta epsilon",
                new TextFitOptions
                {
                    IsMultiline = true
                });

        layout.FontSize.Should().BeLessThan(initialAutoFontSize);
        layout.Segments.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void CalculateTextLayout_WithComb_CentresGlyphsWithinEachCell()
    {
        var fontProvider = CreateMonospaceFontMetricsProvider(
            "Mono",
            defaultWidth: 500,
            spaceWidth: 500,
            ascent: 800,
            descent: -200,
            capHeight: 700,
            xHeight: 523);

        var layout = new TextCalculations([fontProvider])
            .CalculateTextLayout(
                "Mono",
                Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(10, 20), new ZingPDF.Elements.Drawing.Coordinate(110, 40)),
                "AB",
                new TextFitOptions
                {
                    RequestedFontSize = 10,
                    IsComb = true,
                    MaxLength = 4
                });

        layout.FontSize.Should().Be(10);
        layout.Segments.Select(x => x.Text).Should().Equal("A", "B");
        layout.Segments[0].Origin.X.Should().BeApproximately(20.75, 0.001);
        layout.Segments[1].Origin.X.Should().BeApproximately(45.25, 0.001);
        layout.Segments[0].Origin.Y.Should().BeApproximately(26.2919, 0.02);
        layout.Segments[1].Origin.Y.Should().BeApproximately(26.2919, 0.02);
    }

    private static IFontMetricsProvider CreateFontMetricsProvider(string fontName, FontMetrics fontMetrics, double measuredWidth)
    {
        var fontProvider = A.Fake<IFontMetricsProvider>();
        A.CallTo(() => fontProvider.IsSupported(fontName)).Returns(true);
        A.CallTo(() => fontProvider.GetFontMetrics(fontName)).Returns(fontMetrics);
        A.CallTo(() => fontProvider.MeasureText(A<string>.Ignored, fontName, A<double>.Ignored)).Returns(measuredWidth);
        return fontProvider;
    }

    private static IFontMetricsProvider CreateMonospaceFontMetricsProvider(
        string fontName,
        int defaultWidth,
        int spaceWidth,
        int ascent,
        int descent,
        int capHeight,
        int xHeight)
    {
        var widths = Enumerable.Range(32, 95)
            .ToDictionary(codePoint => (char)codePoint, _ => defaultWidth);
        widths[' '] = spaceWidth;
        widths['J'] = defaultWidth;

        return new TestFontMetricsProvider(new FontMetrics
        {
            Name = fontName,
            Ascent = ascent,
            Descent = descent,
            CapHeight = capHeight,
            XHeight = xHeight,
            Widths = widths
        });
    }

    private sealed class TestFontMetricsProvider(FontMetrics metrics) : IFontMetricsProvider
    {
        public FontMetrics GetFontMetrics(string fontName) => metrics;

        public bool IsSupported(string fontName) => metrics.Name == fontName;

        public double MeasureText(string text, string fontName, double fontSize)
            => metrics.CalculateStringWidth(text, fontSize);
    }

    //    [Theory]
    //    [InlineData("Hello", 12, 60)] // Assuming each character width is 10 units
    //    [InlineData("World", 10, 50)] // Assuming each character width is 10 units
    //    [InlineData("Test", 8, 32)]   // Assuming each character width is 8 units
    //    [InlineData("PDF", 15, 45)]   // Assuming each character width is 15 units
    //    public void MeasureTextWidth_ShouldReturnCorrectWidth(string text, float fontSize, float expectedWidth)
    //    {
    //        // Arrange
    //        var fontMetrics = new FontMetrics
    //        {
    //            Name = "TestFont",
    //            Ascent = 800,
    //            Descent = 200,
    //            Leading = 0,
    //            CapHeight = 700,
    //            XHeight = 500,
    //            AvgWidth = 500,
    //            MaxWidth = 1000,
    //            DefaultWidth = 1000,
    //            Widths = new Dictionary<char, int>
    //            {
    //                { 'H', 1000 },
    //                { 'e', 1000 },
    //                { 'l', 1000 },
    //                { 'o', 1000 },
    //                { 'W', 1000 },
    //                { 'r', 1000 },
    //                { 'd', 1000 },
    //                { 'T', 1000 },
    //                { 's', 1000 },
    //                { 'P', 1000 },
    //                { 'D', 1000 },
    //                { 'F', 1000 }
    //            }
    //        };

    //        new TextCalculations()
    //            .MeasureTextWidth(fontMetrics, text, fontSize)
    //            .Should().Be(expectedWidth);
    //    }

    //    [Theory]
    //    [InlineData("Helvetica", "Hello", 12, 27.336)]
    //    [InlineData("Helvetica", "World", 10, 26.11)]
    //    [InlineData("Helvetica", "Test", 8, 15.56)]
    //    public void StandardFontMeasurements(string fontName, string text, float fontSize, float expectedWidth)
    //    {
    //        //var fontMetrics = StandardPdfFonts.Metrics[fontName];

    //        new TextCalculations()
    //            .MeasureTextWidth(fontName, text, fontSize)
    //            .Should().Be(expectedWidth);
    //    }

    //private static class TestFontMetrics
    //{
    //    public static Dictionary<string, FontMetrics> Metrics = new()
    //    {
    //        ["Helvetica"] = Helvetica,
    //    };

    //    private static FontMetrics Helvetica => new()
    //    {
    //        Name = "Helvetica",
    //        Ascent = 718,
    //        Descent = -207,
    //        CapHeight = 718,
    //        XHeight = 523,
    //        Widths = new Dictionary<char, int>
    //        {
    //            [' '] = 278, ['A'] = 667, ['B'] = 667, ['C'] = 722,
    //            ['D'] = 722, ['E'] = 667, ['F'] = 611, ['G'] = 778,
    //            ['H'] = 722, ['I'] = 278, ['J'] = 500, ['K'] = 667,
    //            ['L'] = 556, ['M'] = 833, ['N'] = 722, ['O'] = 778,
    //            ['P'] = 667, ['Q'] = 778, ['R'] = 722, ['S'] = 667,
    //            ['T'] = 611, ['U'] = 722, ['V'] = 667, ['W'] = 944,
    //            ['X'] = 667, ['Y'] = 667, ['Z'] = 611, ['a'] = 556,
    //            ['b'] = 556, ['c'] = 500, ['d'] = 556, ['e'] = 556,
    //            ['f'] = 278, ['g'] = 556, ['h'] = 556, ['i'] = 222,
    //            ['j'] = 222, ['k'] = 500, ['l'] = 222, ['m'] = 833,
    //            ['n'] = 556, ['o'] = 556, ['p'] = 556, ['q'] = 556,
    //            ['r'] = 333, ['s'] = 500, ['t'] = 278, ['u'] = 556,
    //            ['v'] = 500, ['w'] = 722, ['x'] = 500, ['y'] = 500,
    //            ['z'] = 500
    //        },
    //    };
    //}
}
