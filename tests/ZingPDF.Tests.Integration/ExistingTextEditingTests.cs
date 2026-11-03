using FluentAssertions;
using Xunit;
using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics;
using ZingPDF.Text;

namespace ZingPDF.Tests.Integration;

public class ExistingTextEditingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedPageContent_EditSaveReopen_ChangesOnlySelectedPage(bool rewrite)
    {
        using var pdf = Pdf.Create();
        var first = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterStandardFontAsync("Helvetica");
        await first.AddTextAsync(new TextObject("Shared original", new Coordinate(30, 100), font, 12, RGBColour.Black));
        var second = await pdf.AppendPageAsync(o => o.MediaBox = ZingPDF.Syntax.CommonDataStructures.Rectangle.FromDimensions(595, 842));
        second.Dictionary.Set("Contents", await first.Dictionary.Contents.GetRawValueAsync());
        second.Dictionary.Set("Resources", await first.Dictionary.Resources.GetRawValueAsync());
        pdf.Objects.Update(second.IndirectObject);
        using var original = new MemoryStream(); await pdf.SaveAsync(original); original.Position = 0;
        using var document = Pdf.Load(original);
        var editor = document.CreateTextEditor();
        await editor.ReplaceLineAsync((await editor.GetLinesAsync(1)).Single(), "Changed first page");
        if (rewrite) await document.RemoveHistoryAsync();
        using var output = new MemoryStream(); await document.SaveAsync(output); output.Position = 0;
        using var reopened = Pdf.Load(output);
        (await reopened.CreateTextEditor().GetLinesAsync(1)).Single().Text.Should().Be("Changed first page");
        (await reopened.CreateTextEditor().GetLinesAsync(2)).Single().Text.Should().Be("Shared original");
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MovePage_PreservesInheritedGeometryResourcesWidgetsAndPageIdentity(bool rewrite)
    {
        using var pdf = Pdf.Create();
        var font = await pdf.RegisterStandardFontAsync("Helvetica");
        var pages = new List<ZingPDF.Elements.Page> { await pdf.GetPageAsync(1) };
        pages.Add(await pdf.AppendPageAsync(o => o.MediaBox = ZingPDF.Syntax.CommonDataStructures.Rectangle.FromDimensions(300, 400)));
        pages.Add(await pdf.AppendPageAsync(o => o.MediaBox = ZingPDF.Syntax.CommonDataStructures.Rectangle.FromDimensions(400, 500)));
        for (var index = 0; index < pages.Count; index++)
            await pages[index].AddTextAsync($"Page {index + 1}", new Coordinate(20, 100), font, 12, RGBColour.Black);
        var form = await pdf.GetOrCreateFormAsync();
        var field = await form.AddTextFieldAsync(1, "name", ZingPDF.Syntax.CommonDataStructures.Rectangle.FromDimensions(100, 30));
        await field.SetValueAsync("Before move");
        var firstId = pages[0].IndirectObject.Id;
        var firstGeometry = await pages[0].GetGeometryAsync();
        var root = await pdf.Objects.PageTree.GetRootPageTreeNodeAsync();
        var rootDictionary = await pdf.Objects.PageTree.GetRootPageTreeNodeDictionaryAsync();
        rootDictionary.Set("MediaBox", await pages[0].Dictionary.MediaBox.GetAsync());
        rootDictionary.Set("Resources", await pages[0].Dictionary.Resources.GetAsync());
        pages[0].Dictionary.Unset("MediaBox"); pages[0].Dictionary.Unset("Resources");
        pdf.Objects.Update(root); pdf.Objects.Update(pages[0].IndirectObject);
        await pdf.MovePageAsync(1, 3);
        (await pdf.GetPageAsync(3)).IndirectObject.Id.Should().Be(firstId);
        (await pages[0].GetGeometryAsync()).PageNumber.Should().Be(3);
        (await (await pdf.GetPageAsync(3)).GetGeometryAsync()).DisplayWidth.Should().Be(firstGeometry.DisplayWidth);
        if (rewrite) await pdf.RemoveHistoryAsync();
        using var output = new MemoryStream(); await pdf.SaveAsync(output); output.Position = 0;
        using var saved = Pdf.Load(output);
        (await saved.CreateTextEditor().GetLinesAsync(1)).Should().Contain(l => l.Text == "Page 2");
        (await saved.CreateTextEditor().GetLinesAsync(2)).Should().Contain(l => l.Text == "Page 3");
        (await saved.CreateTextEditor().GetLinesAsync(3)).Should().Contain(l => l.Text == "Page 1");
        var savedField = await (await saved.GetFormAsync())!.GetFieldAsync<ZingPDF.Elements.Forms.FieldTypes.Text.TextFormField>("name");
        (await savedField!.GetValueAsync()).Should().Be("Before move");
        (await (await saved.GetPageAsync(3)).Dictionary.Annots.GetAsync()).Should().NotBeNull();
    }
}
