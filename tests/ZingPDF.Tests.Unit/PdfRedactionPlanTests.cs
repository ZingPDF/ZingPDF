using System.Text;
using FluentAssertions;
using DrawingCoordinate = ZingPDF.Elements.Drawing.Coordinate;
using ZingPDF.Extensions;
using ZingPDF.Graphics;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using Xunit;

namespace ZingPDF.Tests.Unit;

public class PdfRedactionPlanTests
{
    [Fact]
    public async Task ApplyAsync_RegionMark_CanRedactFormXObjectContent()
    {
        using var source = new MemoryStream();
        using var output = new MemoryStream();
        using var pdf = Pdf.Create();

        var page = await pdf.GetPageAsync(1);
        var formBounds = Rectangle.FromCoordinates(new DrawingCoordinate(0, 0), new DrawingCoordinate(100, 100));
        var pagePlacement = Rectangle.FromCoordinates(new DrawingCoordinate(20, 20), new DrawingCoordinate(120, 120));

        var formContent = new ContentStream()
            .SetColour(RGBColour.PrimaryBlue)
            .MoveTo(new DrawingCoordinate(20, 20))
            .LineTo(new DrawingCoordinate(80, 20))
            .LineTo(new DrawingCoordinate(80, 80))
            .LineTo(new DrawingCoordinate(20, 80))
            .LineTo(new DrawingCoordinate(20, 20));
        formContent.Operations.Add(new ContentStreamOperation
        {
            Operator = ContentStream.Operators.PathPainting.f
        });

        var formDictionary = new Type1FormDictionary(
            pdf,
            ObjectContext.UserCreated,
            formBounds,
            new ResourceDictionary(pdf, ObjectContext.UserCreated));

        var formStream = await new ContentStreamFactory([formContent])
            .CreateAsync(formDictionary, ObjectContext.UserCreated);
        var formObject = await pdf.Objects.AddAsync(formStream);

        await page.Dictionary.AddXObjectResourceAsync("Fx", formObject.Reference, pdf);

        var pageContent = new FormXObjectContentStream(
            (Name)"Fx",
            pagePlacement,
            formBounds,
            ObjectContext.UserCreated);

        var pageContentStream = await new ContentStreamFactory([pageContent])
            .CreateAsync(new StreamDictionary(pdf, ObjectContext.UserCreated), ObjectContext.UserCreated);
        var pageContentObject = await pdf.Objects.AddAsync(pageContentStream);

        page.Dictionary.Set(Constants.DictionaryKeys.PageTree.Page.Contents, pageContentObject.Reference);
        pdf.Objects.Update(page.IndirectObject);

        await pdf.SaveAsync(source);

        source.Position = 0;
        using var loaded = Pdf.Load(source);

        var plan = await loaded.RedactionAsync();
        plan.MarkRegion(
            1,
            Rectangle.FromCoordinates(
                new DrawingCoordinate(35, 35),
                new DrawingCoordinate(105, 105)));

        await plan.ApplyAsync();
        await loaded.SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().NotContain("20 20 m 80 20 l 80 80 l 20 80 l 20 20 l f");

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedPage = await reloaded.GetPageAsync(1);
        var rawResources = await reloadedPage.Dictionary.Resources.GetAsync();
        var resources = ResourceDictionary.FromDictionary(rawResources!);
        var xObjects = await resources.XObject.GetAsync();
        var formReference = xObjects!.GetAs<IndirectObjectReference>("Fx");
        var reloadedForm = await reloaded.Objects.GetAsync<StreamObject<Type1FormDictionary>>(formReference);
        await using var decoded = await reloadedForm.GetDecompressedDataAsync();
        using var decodedCopy = new MemoryStream();
        await decoded.CopyToAsync(decodedCopy);

        Encoding.ASCII.GetString(decodedCopy.ToArray()).Should().Contain("20 20 m 80 20 l 80 80 l 20 80 l 20 20 l n");
    }
}
