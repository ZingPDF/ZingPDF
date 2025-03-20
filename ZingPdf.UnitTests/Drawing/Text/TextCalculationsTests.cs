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
    [InlineData("Helvetica", 1000, 10, "TEST", 7.20720768, 1.66666651)]
    public void CalculateFontSize_NoOverflow(string fontName, double width, double height, string textValue, float expectedFontSize, float expectedBaselineOffset)
    {
        var fontProvider = new PDFStandardFontProvider();

        new TextCalculations([fontProvider])
            .CalculateTextFit(fontName, Rectangle.FromDimensions(width, height), textValue)
            .Should()
            .BeEquivalentTo(new TextFit { FontSize = expectedFontSize, Baseline = expectedBaselineOffset });
    }

    [Theory]
    [InlineData("Helvetica", 232.44000000000003, 14.04000000000002, "mmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm", 4, 5.536)]
    [InlineData("Helvetica", 232.44000000000003, 14.039999999999964, "mmmmmmmmmmmmmmmmmmmmmmmmmmm", 8.921, 3.7103)]
    [InlineData("Helvetica", 209.821, 22, "finnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn", 4, 9.516)]
    public void CalculateFontSize_WithOverflow(string fontName, double width, double height, string textValue, float expectedFontSize, float expectedBaselineOffset)
    {
        var fontProvider = new PDFStandardFontProvider();

        new TextCalculations([fontProvider])
            .CalculateTextFit(fontName, Rectangle.FromDimensions(width, height), textValue)
            .Should()
            .BeEquivalentTo(new TextFit { FontSize = expectedFontSize, Baseline = expectedBaselineOffset });
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