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