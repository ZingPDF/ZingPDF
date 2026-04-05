using FluentAssertions;
using Xunit;
using ZingPDF.Graphics;

namespace ZingPDF.Elements.Drawing;

public class PathTests
{
    [Fact]
    public void LinearPath_RequiresAtLeastTwoPoints()
    {
        var act = () => new Path(
            new StrokeOptions(RGBColour.PrimaryRed, 1),
            null,
            PathType.Linear,
            [new Coordinate(0, 0)]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*at least 2 points*");
    }

    [Fact]
    public void BezierPath_RequiresValidPointGrouping()
    {
        var act = () => new Path(
            null,
            new FillOptions(RGBColour.PrimaryBlue),
            PathType.Bezier,
            [
                new Coordinate(0, 0),
                new Coordinate(10, 10),
                new Coordinate(20, 10),
                new Coordinate(30, 0),
                new Coordinate(40, 10)
            ]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*additional segment*");
    }
}
