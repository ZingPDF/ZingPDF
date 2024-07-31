using FakeItEasy;
using FluentAssertions;
using Xunit;
using ZingPDF.Syntax.CommonDataStructures;

namespace ZingPDF.Elements.Drawing;

public class CoordinateTranslatorTests
{
    [Fact]
    public void FlipImageCoordinatesDoesNothingForBottomUp()
    {
        var coordinate = new Coordinate(10, 10);

        new CoordinateTranslator(A.Fake<ICalculations>())
            .FlipImageCoordinatesIfRequired(0, 100, 100, CoordinateSystem.BottomUp, coordinate, 100).Should().BeEquivalentTo(coordinate);
    }

    [Fact]
    public void FlipTextCoordinatesDoesNothingForBottomUp()
    {
        var lowerLeft = new Coordinate(10, 10);
        var upperRight = new Coordinate(100, 100);

        var boundingBox = new Rectangle(lowerLeft, upperRight);

        new CoordinateTranslator(A.Fake<ICalculations>())
            .FlipTextCoordinatesIfRequired(0, 100, 100, CoordinateSystem.BottomUp, boundingBox).Should().BeEquivalentTo(boundingBox);
    }

    [Fact]
    public void FlipPathCoordinatesDoesNothingForBottomUp()
    {
        var coordinates = new[] { new Coordinate(10, 10) };

        new CoordinateTranslator(A.Fake<ICalculations>())
            .FlipPathCoordinatesIfRequired(0, 100, 100, CoordinateSystem.BottomUp, coordinates).Should().BeEquivalentTo(coordinates);
    }

    [Theory]
    [InlineData(0, 100, 100)]
    [InlineData(10, 100, 90)]
    [InlineData(100, 100, 0)]
    public void FlipPathCoordinates(int y, int pageHeight, int expectedYValue)
    {
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.AngleIsPerpendicular(A<int>.Ignored)).Returns(false);

        var arbitraryXValue = 10;
        var arbitraryPageWidth = 100;
        var expectedTranslatedYValue = pageHeight - y;

        new CoordinateTranslator(calculations)
            .FlipPathCoordinatesIfRequired(0, arbitraryPageWidth, pageHeight, CoordinateSystem.TopDown, new[] { new Coordinate(arbitraryXValue, y) })
            .Should().BeEquivalentTo(new[] { new Coordinate(arbitraryXValue, expectedYValue) });
    }

    [Theory]
    [InlineData(0, 100, 100, 0)]
    [InlineData(10, 100, 10, 80)]
    [InlineData(10, 100, 200, -110)]
    public void FlipImageCoordinates(int y, int pageHeight, int imageHeight, int expectedYValue)
    {
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.AngleIsPerpendicular(A<int>.Ignored)).Returns(false);

        var arbitraryXValue = 10;
        var arbitraryPageWidth = 100;

        new CoordinateTranslator(calculations)
            .FlipImageCoordinatesIfRequired(0, arbitraryPageWidth, pageHeight, CoordinateSystem.TopDown, new Coordinate(arbitraryXValue, y), imageHeight)
            .Should().BeEquivalentTo(new Coordinate(arbitraryXValue, expectedYValue));
    }

    [Theory]
    [InlineData(0, 100, 100, 0)]
    [InlineData(10, 100, 10, 80)]
    [InlineData(10, 100, 200, -110)]
    public void FlipTextCoordinates(int y, int pageHeight, int boundingBoxHeight, int expectedYValue)
    {
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.AngleIsPerpendicular(A<int>.Ignored)).Returns(false);

        var arbitraryXValue = 10;
        var arbitraryBoxWidth = 100;
        var arbitraryPageWidth = 100;

        var boundingBox = new Rectangle(new Coordinate(arbitraryXValue, y), new Coordinate(arbitraryBoxWidth, boundingBoxHeight));
        var translatedBoundingBox = new Rectangle(new Coordinate(arbitraryXValue, expectedYValue), new Coordinate(arbitraryBoxWidth, boundingBoxHeight));

        new CoordinateTranslator(calculations)
            .FlipTextCoordinatesIfRequired(0, arbitraryPageWidth, pageHeight, CoordinateSystem.TopDown, boundingBox)
            .Should().BeEquivalentTo(translatedBoundingBox);
    }

    [Theory]
    [InlineData(1190, 841, 267, 1016, 174, 267)]
    [InlineData(1190, 841, 267, 951, 239, 267)]
    [InlineData(1190, 841, 224, 951, 239, 224)]
    [InlineData(1190, 841, 225, 907, 283, 225)]
    [InlineData(1190, 841, 189, 908, 282, 189)]
    [InlineData(1190, 841, 188, 1018, 172, 188)]
    public void RotateCoordinates90DegreesLandscapePage(int pageWidth, int pageHeight, int x, int y, int expectedX, int expectedY)
    {
        const int pageDisplayRotation = 90;
        var horizontalCentre = pageWidth / 2;
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.FindRotationPoint(A<int>.Ignored, A<double>.Ignored, A<double>.Ignored))
            .Returns(new Coordinate(horizontalCentre, horizontalCentre));

        new CoordinateTranslator(calculations).RotateCoordinates(pageDisplayRotation, pageWidth, pageHeight, [new Coordinate(x, y)])
            .Should().BeEquivalentTo(new[] { new Coordinate(expectedX, expectedY) });
    }

    [Theory]
    [InlineData(612, 792, 267, 1016, -404, 267)]
    [InlineData(612, 792, 267, 951, -339, 267)]
    [InlineData(612, 792, 224, 951, -339, 224)]
    [InlineData(612, 792, 225, 907, -295, 225)]
    [InlineData(612, 792, 189, 908, -296, 189)]
    [InlineData(612, 792, 188, 1018, -406, 188)]
    public void RotateCoordinates90DegreesPortraitPage(int pageWidth, int pageHeight, int x, int y, int expectedX, int expectedY)
    {
        const int pageDisplayRotation = 90;
        var horizontalCentre = pageWidth / 2;
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.FindRotationPoint(A<int>.Ignored, A<double>.Ignored, A<double>.Ignored))
            .Returns(new Coordinate(horizontalCentre, horizontalCentre));

        new CoordinateTranslator(calculations).RotateCoordinates(pageDisplayRotation, pageWidth, pageHeight, [new Coordinate(x, y)])
            .Should().BeEquivalentTo(new[] { new Coordinate(expectedX, expectedY) });
    }

    [Theory]
    [InlineData(841.68, 595.2, 100, 100, 100, 494)]
    [InlineData(841.68, 595.2, 100, 300, 300, 494)]
    [InlineData(841.68, 595.2, 400, 300, 300, 194)]
    [InlineData(841.68, 595.2, 400, 100, 100, 194)]
    public void RotateCoordinatesMinus90DegreesLandscapePage(double pageWidth, double pageHeight, int x, int y, int expectedX, int expectedY)
    {
        const int pageDisplayRotation = -90;
        var verticalCentre = (int)pageHeight / 2;
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.FindRotationPoint(A<int>.Ignored, A<double>.Ignored, A<double>.Ignored))
            .Returns(new Coordinate(verticalCentre, verticalCentre));

        new CoordinateTranslator(calculations).RotateCoordinates(pageDisplayRotation, pageWidth, pageHeight, [new Coordinate(x, y)])
            .Should().BeEquivalentTo(new[] { new Coordinate(expectedX, expectedY) });
    }

    [Theory]
    [InlineData(612, 792, 100, 100, 100, 692)]
    [InlineData(612, 792, 100, 300, 300, 692)]
    [InlineData(612, 792, 400, 300, 300, 392)]
    [InlineData(612, 792, 400, 100, 100, 392)]
    public void RotateCoordinatesMinus90DegreesPortraitPage(double pageWidth, double pageHeight, int x, int y, int expectedX, int expectedY)
    {
        const int pageDisplayRotation = -90;
        var verticalCentre = (int)pageHeight / 2;
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.FindRotationPoint(A<int>.Ignored, A<double>.Ignored, A<double>.Ignored))
            .Returns(new Coordinate(verticalCentre, verticalCentre));

        new CoordinateTranslator(calculations).RotateCoordinates(pageDisplayRotation, pageWidth, pageHeight, [new Coordinate(x, y)])
            .Should().BeEquivalentTo(new[] { new Coordinate(expectedX, expectedY) });
    }

    [Theory]
    [InlineData(1190, 841, 267, 1016, 923, -176)]
    [InlineData(1190, 841, 267, 951, 923, -111)]
    [InlineData(1190, 841, 224, 951, 966, -111)]
    [InlineData(1190, 841, 225, 907, 965, -67)]
    [InlineData(1190, 841, 189, 908, 1001, -68)]
    [InlineData(1190, 841, 188, 1018, 1002, -178)]
    public void RotateCoordinates180DegreesLandscapePage(int pageWidth, int pageHeight, int x, int y, int expectedX, int expectedY)
    {
        const int pageDisplayRotation = 180;
        var horizontalCentre = pageWidth / 2;
        var verticalCentre = pageHeight / 2;
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.FindRotationPoint(A<int>.Ignored, A<double>.Ignored, A<double>.Ignored))
            .Returns(new Coordinate(horizontalCentre, verticalCentre));

        new CoordinateTranslator(calculations).RotateCoordinates(pageDisplayRotation, pageWidth, pageHeight, [new Coordinate(x, y)])
            .Should().BeEquivalentTo(new[] { new Coordinate(expectedX, expectedY) });
    }

    [Theory]
    [InlineData(1190, 841, 267, 1016, 923, -176)]
    [InlineData(1190, 841, 267, 951, 923, -111)]
    [InlineData(1190, 841, 224, 951, 966, -111)]
    [InlineData(1190, 841, 225, 907, 965, -66)]
    [InlineData(1190, 841, 189, 908, 1001, -67)]
    [InlineData(1190, 841, 188, 1018, 1002, -178)]
    public void RotateCoordinatesMinus180DegreesLandscapePage(int pageWidth, int pageHeight, int x, int y, int expectedX, int expectedY)
    {
        const int pageDisplayRotation = -180;
        var horizontalCentre = pageWidth / 2;
        var verticalCentre = pageHeight / 2;
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.FindRotationPoint(A<int>.Ignored, A<double>.Ignored, A<double>.Ignored))
            .Returns(new Coordinate(horizontalCentre, verticalCentre));

        new CoordinateTranslator(calculations).RotateCoordinates(pageDisplayRotation, pageWidth, pageHeight, [new Coordinate(x, y)])
            .Should().BeEquivalentTo(new[] { new Coordinate(expectedX, expectedY) });
    }

    [Theory]
    [InlineData(612, 792, 267, 1016, 344, -224)]
    [InlineData(612, 792, 267, 951, 344, -159)]
    [InlineData(612, 792, 224, 951, 387, -159)]
    [InlineData(612, 792, 225, 907, 386, -115)]
    [InlineData(612, 792, 189, 908, 422, -116)]
    [InlineData(612, 792, 188, 1018, 423, -226)]
    public void RotateCoordinates180DegreesPortraitPage(int pageWidth, int pageHeight, int x, int y, int expectedX, int expectedY)
    {
        const int pageDisplayRotation = 180;
        var horizontalCentre = pageWidth / 2;
        var verticalCentre = pageHeight / 2;
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.FindRotationPoint(A<int>.Ignored, A<double>.Ignored, A<double>.Ignored))
            .Returns(new Coordinate(horizontalCentre, verticalCentre));

        new CoordinateTranslator(calculations).RotateCoordinates(pageDisplayRotation, pageWidth, pageHeight, [new Coordinate(x, y)])
            .Should().BeEquivalentTo(new[] { new Coordinate(expectedX, expectedY) });
    }

    [Theory]
    [InlineData(612, 792, 267, 1016, 345, -224)]
    [InlineData(612, 792, 267, 951, 345, -159)]
    [InlineData(612, 792, 224, 951, 388, -159)]
    [InlineData(612, 792, 225, 907, 387, -115)]
    [InlineData(612, 792, 189, 908, 423, -116)]
    [InlineData(612, 792, 188, 1018, 424, -226)]
    public void RotateCoordinatesMinus180DegreesPortraitPage(int pageWidth, int pageHeight, int x, int y, int expectedX, int expectedY)
    {
        const int pageDisplayRotation = -180;
        var horizontalCentre = pageWidth / 2;
        var verticalCentre = pageHeight / 2;
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.FindRotationPoint(A<int>.Ignored, A<double>.Ignored, A<double>.Ignored))
            .Returns(new Coordinate(horizontalCentre, verticalCentre));

        new CoordinateTranslator(calculations).RotateCoordinates(pageDisplayRotation, pageWidth, pageHeight, [new Coordinate(x, y)])
            .Should().BeEquivalentTo(new[] { new Coordinate(expectedX, expectedY) });
    }

    [Theory]
    [InlineData(841.68, 595.2, 100, 100, 100, 494)]
    [InlineData(841.68, 595.2, 100, 300, 300, 494)]
    [InlineData(841.68, 595.2, 400, 300, 300, 194)]
    [InlineData(841.68, 595.2, 400, 100, 99, 194)]
    public void RotateCoordinates270DegreesLandscapePage(double pageWidth, double pageHeight, int x, int y, int expectedX, int expectedY)
    {
        const int pageDisplayRotation = 270;
        var verticalCentre = (int)pageHeight / 2;
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.FindRotationPoint(A<int>.Ignored, A<double>.Ignored, A<double>.Ignored))
            .Returns(new Coordinate(verticalCentre, verticalCentre));

        new CoordinateTranslator(calculations).RotateCoordinates(pageDisplayRotation, pageWidth, pageHeight, [new Coordinate(x, y)])
            .Should().BeEquivalentTo(new[] { new Coordinate(expectedX, expectedY) });
    }

    [Theory]
    [InlineData(612, 792, 100, 100, 100, 692)]
    [InlineData(612, 792, 100, 300, 300, 692)]
    [InlineData(612, 792, 400, 300, 300, 392)]
    [InlineData(612, 792, 400, 100, 100, 392)]
    public void RotateCoordinates270DegreesPortraitPage(double pageWidth, double pageHeight, int x, int y, int expectedX, int expectedY)
    {
        const int pageDisplayRotation = 270;
        var verticalCentre = (int)pageHeight / 2;
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.FindRotationPoint(A<int>.Ignored, A<double>.Ignored, A<double>.Ignored))
            .Returns(new Coordinate(verticalCentre, verticalCentre));

        new CoordinateTranslator(calculations).RotateCoordinates(pageDisplayRotation, pageWidth, pageHeight, [new Coordinate(x, y)])
            .Should().BeEquivalentTo(new[] { new Coordinate(expectedX, expectedY) });
    }

    [Theory]
    [InlineData(1190, 841, 267, 1016, 174, 266)]
    [InlineData(1190, 841, 267, 951, 239, 266)]
    [InlineData(1190, 841, 224, 951, 239, 223)]
    [InlineData(1190, 841, 225, 907, 283, 224)]
    [InlineData(1190, 841, 189, 908, 282, 188)]
    [InlineData(1190, 841, 188, 1018, 172, 187)]
    public void RotateCoordinatesMinus270DegreesLandscapePage(int pageWidth, int pageHeight, int x, int y, int expectedX, int expectedY)
    {
        const int pageDisplayRotation = -270;
        var horizontalCentre = pageWidth / 2;
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.FindRotationPoint(A<int>.Ignored, A<double>.Ignored, A<double>.Ignored))
            .Returns(new Coordinate(horizontalCentre, horizontalCentre));

        new CoordinateTranslator(calculations).RotateCoordinates(pageDisplayRotation, pageWidth, pageHeight, [new Coordinate(x, y)])
            .Should().BeEquivalentTo(new[] { new Coordinate(expectedX, expectedY) });
    }

    [Theory]
    [InlineData(612, 792, 267, 1016, -404, 266)]
    [InlineData(612, 792, 267, 951, -339, 266)]
    [InlineData(612, 792, 224, 951, -339, 223)]
    [InlineData(612, 792, 225, 907, -295, 224)]
    [InlineData(612, 792, 189, 908, -296, 188)]
    [InlineData(612, 792, 188, 1018, -406, 187)]
    public void RotateCoordinatesMinus270DegreesPortraitPage(int pageWidth, int pageHeight, int x, int y, int expectedX, int expectedY)
    {
        const int pageDisplayRotation = -270;
        var horizontalCentre = pageWidth / 2;
        var calculations = A.Fake<ICalculations>();

        A.CallTo(() => calculations.FindRotationPoint(A<int>.Ignored, A<double>.Ignored, A<double>.Ignored))
            .Returns(new Coordinate(horizontalCentre, horizontalCentre));

        new CoordinateTranslator(calculations).RotateCoordinates(pageDisplayRotation, pageWidth, pageHeight, [new Coordinate(x, y)])
            .Should().BeEquivalentTo(new[] { new Coordinate(expectedX, expectedY) });
    }
}
