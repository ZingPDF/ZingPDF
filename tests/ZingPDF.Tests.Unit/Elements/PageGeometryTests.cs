using FluentAssertions;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using Xunit;

namespace ZingPDF.Tests.Unit.Elements;

public class PageGeometryTests
{
    [Fact]
    public async Task GetGeometryAsync_MediaBoxOnly_UsesMediaBoxForVisibleBounds()
    {
        using var pdf = Pdf.Create(options => options.MediaBox = Rectangle.FromDimensions(240, 360));
        var page = await pdf.GetPageAsync(1);

        var geometry = await page.GetGeometryAsync();

        geometry.PageNumber.Should().Be(1);
        geometry.MediaBox.Width.Value.Should().Be(240);
        geometry.MediaBox.Height.Value.Should().Be(360);
        geometry.CropBox.Should().BeEquivalentTo(geometry.MediaBox);
        geometry.VisibleBox.Should().BeEquivalentTo(geometry.MediaBox);
        geometry.RotationDegrees.Should().Be(0);
        geometry.DisplayWidth.Should().Be(240);
        geometry.DisplayHeight.Should().Be(360);
    }

    [Fact]
    public async Task GetGeometryAsync_CropBoxExtendingOutsideMediaBox_ReturnsDisplayedIntersection()
    {
        using var pdf = Pdf.Create(options => options.MediaBox = Rectangle.FromDimensions(200, 300));
        var page = await pdf.GetPageAsync(1);
        page.Dictionary.Set(
            Constants.DictionaryKeys.PageTree.CropBox,
            Rectangle.FromCoordinates(new Coordinate(20, -10), new Coordinate(240, 250)));

        var geometry = await page.GetGeometryAsync();

        geometry.CropBox.LowerLeft.Should().BeEquivalentTo(new Coordinate(20, -10));
        geometry.CropBox.UpperRight.Should().BeEquivalentTo(new Coordinate(240, 250));
        geometry.VisibleBox.LowerLeft.Should().BeEquivalentTo(new Coordinate(20, 0));
        geometry.VisibleBox.UpperRight.Should().BeEquivalentTo(new Coordinate(200, 250));
        geometry.DisplayWidth.Should().Be(180);
        geometry.DisplayHeight.Should().Be(250);
    }

    [Theory]
    [InlineData(0, 0, 100, 200)]
    [InlineData(90, 90, 200, 100)]
    [InlineData(180, 180, 100, 200)]
    [InlineData(270, 270, 200, 100)]
    [InlineData(450, 90, 200, 100)]
    [InlineData(-90, 270, 200, 100)]
    public async Task GetGeometryAsync_NormalisesRotationAndDisplayDimensions(
        int rotation,
        int expectedRotation,
        double expectedWidth,
        double expectedHeight)
    {
        using var pdf = Pdf.Create(options => options.MediaBox = Rectangle.FromDimensions(100, 200));
        var page = await pdf.GetPageAsync(1);
        page.Dictionary.Set(Constants.DictionaryKeys.PageTree.Rotate, (Number)rotation);

        var geometry = await page.GetGeometryAsync();

        geometry.RotationDegrees.Should().Be(expectedRotation);
        geometry.DisplayWidth.Should().Be(expectedWidth);
        geometry.DisplayHeight.Should().Be(expectedHeight);
    }

    [Fact]
    public async Task GetGeometryAsync_ResolvesInheritedPageTreeValues()
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        var root = await pdf.Objects.PageTree.GetRootPageTreeNodeDictionaryAsync();

        page.Dictionary.Unset(Constants.DictionaryKeys.PageTree.MediaBox);
        root.Set(
            Constants.DictionaryKeys.PageTree.MediaBox,
            Rectangle.FromCoordinates(new Coordinate(10, 20), new Coordinate(310, 420)));
        root.Set(
            Constants.DictionaryKeys.PageTree.CropBox,
            Rectangle.FromCoordinates(new Coordinate(30, 50), new Coordinate(280, 350)));
        root.Set(Constants.DictionaryKeys.PageTree.Rotate, (Number)90);

        var geometry = await page.GetGeometryAsync();

        geometry.MediaBox.LowerLeft.Should().BeEquivalentTo(new Coordinate(10, 20));
        geometry.MediaBox.UpperRight.Should().BeEquivalentTo(new Coordinate(310, 420));
        geometry.VisibleBox.LowerLeft.Should().BeEquivalentTo(new Coordinate(30, 50));
        geometry.VisibleBox.UpperRight.Should().BeEquivalentTo(new Coordinate(280, 350));
        geometry.RotationDegrees.Should().Be(90);
        geometry.DisplayWidth.Should().Be(300);
        geometry.DisplayHeight.Should().Be(250);
    }

    [Theory]
    [InlineData(0, 35, 65, 15, 55)]
    [InlineData(90, 35, 65, 45, 15)]
    [InlineData(180, 35, 65, 85, 45)]
    [InlineData(270, 35, 65, 55, 85)]
    public async Task Geometry_MapsCroppedPagePointsToDisplayAndBack(
        int rotation,
        double pageX,
        double pageY,
        double expectedDisplayX,
        double expectedDisplayY)
    {
        using var pdf = Pdf.Create(options => options.MediaBox = Rectangle.FromDimensions(160, 180));
        var page = await pdf.GetPageAsync(1);
        page.Dictionary.Set(
            Constants.DictionaryKeys.PageTree.CropBox,
            Rectangle.FromCoordinates(new Coordinate(20, 20), new Coordinate(120, 120)));
        page.Dictionary.Set(Constants.DictionaryKeys.PageTree.Rotate, (Number)rotation);

        var geometry = await page.GetGeometryAsync();
        var displayPoint = geometry.PageToDisplay(new Coordinate(pageX, pageY));
        var roundTripPoint = geometry.DisplayToPage(displayPoint);

        displayPoint.Should().BeEquivalentTo(new Coordinate(expectedDisplayX, expectedDisplayY));
        roundTripPoint.Should().BeEquivalentTo(new Coordinate(pageX, pageY));
    }

    [Fact]
    public async Task GetGeometryAsync_CancelledToken_ThrowsBeforeResolvingGeometry()
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = () => page.GetGeometryAsync(cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
