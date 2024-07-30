using FluentAssertions;
using Xunit;

namespace ZingPDF.Elements.Drawing;

public class CalculationsTests
{
    [Theory]
    [InlineData(0, 255, 0)]
    [InlineData(100, 255, 39)]
    [InlineData(255, 255, 100)]
    public void PercentageOfValueCorrectResult(int value, int maxValue, int expectedResult)
    {
        new Calculations().PercentageOfValue(value, maxValue).Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 0.39)]
    [InlineData(255, 1)]
    public void NormaliseCorrectResult(byte value, double expectedResult)
    {
        new Calculations().Normalise(value).Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(-90)]
    [InlineData(270)]
    [InlineData(-270)]
    public void AngleIsPerpendicular(int angle)
    {
        new Calculations().AngleIsPerpendicular(angle).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(180)]
    [InlineData(-180)]
    public void AngleIsNotPerpendicular(int angle)
    {
        new Calculations().AngleIsPerpendicular(angle).Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(180)]
    [InlineData(-180)]
    public void FindRotationPointCentred(int pageDisplayRotation)
    {
        var arbitaryPageDimension = 100;
        var halfArbitraryDimension = arbitaryPageDimension / 2;

        new Calculations().FindRotationPoint(pageDisplayRotation, arbitaryPageDimension, arbitaryPageDimension)
            .Should().BeEquivalentTo(new Point(halfArbitraryDimension, halfArbitraryDimension));
    }

    [Theory]
    [InlineData(90, 100, 50)]
    [InlineData(90, 50, 100)]
    [InlineData(-270, 100, 50)]
    [InlineData(-270, 50, 100)]
    public void FindRotationPoint90Degrees(int pageDisplayRotation, int pageWidth, int pageHeight)
    {
        var halfWidth = pageWidth / 2;

        new Calculations().FindRotationPoint(pageDisplayRotation, pageWidth, pageHeight)
            .Should().BeEquivalentTo(new Point(halfWidth, halfWidth));
    }

    [Theory]
    [InlineData(-90, 100, 50)]
    [InlineData(-90, 50, 100)]
    [InlineData(270, 100, 50)]
    [InlineData(270, 50, 100)]
    public void FindRotationPointLandscape270Degrees(int pageDisplayRotation, int pageWidth, int pageHeight)
    {
        var halfHeight = pageHeight / 2;

        new Calculations().FindRotationPoint(pageDisplayRotation, pageWidth, pageHeight)
            .Should().BeEquivalentTo(new Point(halfHeight, halfHeight));
    }
}
