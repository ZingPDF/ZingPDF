using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZingPDF.Editor.Core;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Fonts;
using ZingPDF.Graphics;
using ZingPDF.Extensions;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using ZingPDF.Text;
using Xunit;
using Path = System.IO.Path;
using Rectangle = ZingPDF.Syntax.CommonDataStructures.Rectangle;
using Image = SixLabors.ImageSharp.Image;
using Number = ZingPDF.Syntax.Objects.Number;

namespace ZingPDF.Editor.Tests;

public sealed class EditorDocumentTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "zingpdf-editor-tests-" + Guid.NewGuid().ToString("N"));
    public EditorDocumentTests() => Directory.CreateDirectory(_directory);
    private string FilePath(string name) => Path.Combine(_directory, name);

    [Fact]
    public async Task OversizedSource_IsRejectedBeforeReadingAndPreviewCacheFollowsRevisions()
    {
        var oversized = FilePath("oversized.pdf");
        await using (var file = File.Create(oversized)) file.SetLength(EditorDocument.MaximumFileBytes + 1);
        var error = await Assert.ThrowsAsync<NotSupportedException>(() => EditorDocument.OpenAsync(oversized));
        Assert.Contains("256 MB", error.Message);
        var document = await EditorDocument.OpenAsync(await CreatePdfAsync());
        var first = await document.RenderAsync(1, 1);
        Assert.Same(first, await document.RenderAsync(1, 1));
        await document.AddTextAsync(1, 20, 30);
        var edited = await document.RenderAsync(1, 1);
        Assert.NotSame(first, edited);
        Assert.Same(edited, await document.RenderAsync(1, 1));
    }

    public async Task<string> CreatePdfAsync(int rotation = 0, bool crop = false, bool forms = false, int pages = 1)
    {
        using var pdf = Pdf.Create(o => o.MediaBox = Rectangle.FromDimensions(300, 400));
        var first = await pdf.GetPageAsync(1);
        if (crop) first.Dictionary.Set("CropBox", Rectangle.FromCoordinates(new Coordinate(20, 30), new Coordinate(280, 370)));
        await first.RotateAsync(Rotation.FromValue(rotation));
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);
        for (var n = 1; n <= pages; n++)
        {
            var page = n == 1 ? first : await pdf.AppendPageAsync(o => o.MediaBox = Rectangle.FromDimensions(300, 400));
            await page.AddTextAsync(new TextObject($"Original page {n}", new Coordinate(30, 55), font, 12, RGBColour.Black));
        }
        if (forms)
        {
            var form = await pdf.GetOrCreateFormAsync();
            var field = await form.AddTextFieldAsync(1, "visitor", Rectangle.FromCoordinates(new Coordinate(30, 120), new Coordinate(180, 150)));
            await field.SetValueAsync("Before");
            await form.AddTextFieldAsync(1, "date", Rectangle.FromCoordinates(new Coordinate(30, 170), new Coordinate(180, 200)));
        }
        var path = FilePath(Guid.NewGuid() + ".pdf");
        using var output = File.Create(path);
        await pdf.SaveAsync(output);
        return path;
    }

    [Theory]
    [InlineData(0)] [InlineData(90)] [InlineData(180)] [InlineData(270)]
    public async Task AddedText_CroppedRotatedPage_PreviewMatchesSavedPixels(int rotation)
    {
        var path = await CreatePdfAsync(rotation, crop: true);
        var original = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        var editor = await EditorDocument.OpenAsync(path);
        var before = await editor.RenderAsync(1, 2);
        var id = await editor.AddTextAsync(1, 40, 60);
        editor.UpdateText(id, "HELLO\nSecond line", 16);
        var preview = await editor.RenderAsync(1, 2);
        using var image = Image.Load<Rgba32>(preview.PngBytes.Span);
        // The first glyph should be at the requested displayed position at every page rotation.
        Assert.True(CountDark(image, 78, 118, 180, 185) > 80);
        using var beforeImage = Image.Load<Rgba32>(before.PngBytes.Span);
        Assert.Equal(CountDark(beforeImage, 20, 20, 60, 80), CountDark(image, 20, 20, 60, 80));
        var outputPath = FilePath("edited.pdf");
        await editor.SaveCopyAsync(outputPath);
        using var output = File.OpenRead(outputPath);
        using var pdf = Pdf.Load(output);
        var saved = await pdf.RenderPageAsync(1, new() { Scale = 2 }, TestContext.Current.CancellationToken);
        Assert.Equal(preview.PngBytes.ToArray(), saved.PngBytes.ToArray());
        var extracted = string.Join(" ", (await pdf.ExtractTextAsync()).Select(t => t.Text));
        Assert.Contains("HELLO", extracted);
        Assert.Contains("Second line", extracted);
        Assert.Equal(original, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public async Task ExistingTransformAndClip_DoNotAffectAddedText()
    {
        var source = FilePath("transformed.pdf");
        using (var pdf = Pdf.Create(o => o.MediaBox = Rectangle.FromDimensions(300, 400)))
        {
            var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);
            var text = new TextObject("Existing", new Coordinate(5, 30), font, 12, RGBColour.Black);
            text.Operations.InsertRange(0, new ContentStream().ConcatenateMatrix(2, 0, 0, 2, 40, 10)
                .SetClippingPath(Rectangle.FromDimensions(80, 100)).Operations);
            await (await pdf.GetPageAsync(1)).AddTextAsync(text);
            using var output = File.Create(source); await pdf.SaveAsync(output);
        }
        var editor = await EditorDocument.OpenAsync(source);
        var id = await editor.AddTextAsync(1, 40, 60); editor.UpdateText(id, "HELLO", 16);
        var preview = await editor.RenderAsync(1, 2);
        using var image = Image.Load<Rgba32>(preview.PngBytes.Span);
        Assert.True(CountDark(image, 78, 118, 200, 160) > 80);
        await editor.SaveCopyAsync(FilePath("isolated.pdf"));
        using var input = File.OpenRead(FilePath("isolated.pdf")); using var saved = Pdf.Load(input);
        var render = await saved.RenderPageAsync(1, new() { Scale = 2 }, TestContext.Current.CancellationToken);
        Assert.Equal(preview.PngBytes.ToArray(), render.PngBytes.ToArray());
    }

    [Fact]
    public async Task TextMoveUndoRedoAndSave_DoesNotDuplicateText()
    {
        var path = await CreatePdfAsync();
        var editor = await EditorDocument.OpenAsync(path);
        var id = await editor.AddTextAsync(1, 30, 40);
        editor.UpdateText(id, "Once", 20);
        await editor.MoveTextAsync(id, 100, 150);
        var geometry = await editor.GetGeometryAsync(1);
        var anchor = geometry.PageToDisplay(new Coordinate(editor.AddedText[0].X, editor.AddedText[0].Y));
        Assert.Equal(100, anchor.X); Assert.Equal(150, anchor.Y);
        await editor.UndoAsync();
        Assert.Equal("Once", editor.AddedText[0].Text);
        await editor.UndoAsync(); Assert.Equal("Your text", editor.AddedText[0].Text);
        await editor.UndoAsync(); Assert.Empty(editor.AddedText); Assert.False(editor.IsDirty);
        await editor.RedoAsync(); await editor.RedoAsync(); await editor.RedoAsync();
        await editor.SaveCopyAsync(FilePath("first.pdf"));
        await editor.SaveCopyAsync(FilePath("second.pdf"));
        using var stream = File.OpenRead(FilePath("second.pdf")); using var pdf = Pdf.Load(stream);
        Assert.Single(await pdf.ExtractTextAsync(), t => t.Text.Contains("Once"));
        editor.RemoveText(id); await editor.UndoAsync(); Assert.False(editor.IsDirty);
    }

    [Fact]
    public async Task PageOperations_PreserveTextFormsAndUndoHistory()
    {
        var path = await CreatePdfAsync(forms: true, pages: 3);
        var editor = await EditorDocument.OpenAsync(path);
        var id = await editor.AddTextAsync(1, 30, 60);
        editor.UpdateText(id, "Added to first", 12);
        await editor.SetFormFieldAsync("visitor", "After");
        await editor.MovePageAsync(1, 3);
        Assert.Equal(3, editor.AddedText[0].PageNumber);
        Assert.Equal("After", (await editor.GetFormFieldsAsync()).Single(f => f.Name == "visitor").Value);
        await editor.RotateAsync(3);
        Assert.Equal(90, (await editor.GetGeometryAsync(3)).RotationDegrees);
        await editor.DeletePageAsync(2);
        Assert.Equal(2, editor.PageCount); Assert.Equal(2, editor.AddedText[0].PageNumber);
        await editor.UndoAsync(); Assert.Equal(3, editor.PageCount);
        var output = FilePath("pages.pdf"); await editor.SaveCopyAsync(output);
        using var stream = File.OpenRead(output); using var pdf = Pdf.Load(stream);
        Assert.Contains("Original page 2", string.Join(" ", (await pdf.ExtractTextAsync(1)).Select(t => t.Text)));
        Assert.Contains("Added to first", string.Join(" ", (await pdf.ExtractTextAsync(3)).Select(t => t.Text)));
        Assert.Equal("After", await (await (await pdf.GetFormAsync())!.GetFieldAsync<TextFormField>("visitor"))!.GetValueAsync());
    }

    [Fact]
    public async Task ReorderingPages_MaterializesInheritedGeometryAndResources()
    {
        var source = FilePath("inherited.pdf");
        using (var pdf = Pdf.Create(o => o.MediaBox = Rectangle.FromDimensions(200, 300)))
        {
            var first = await pdf.GetPageAsync(1);
            var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);
            await first.AddTextAsync(new TextObject("Inherited font", new Coordinate(30, 100), font, 12, RGBColour.Black));
            var root = await pdf.Objects.PageTree.GetRootPageTreeNodeDictionaryAsync();
            root.Set("Resources", await first.Dictionary.Resources.GetAsync());
            root.Set("MediaBox", Rectangle.FromDimensions(200, 300));
            root.Set("CropBox", Rectangle.FromCoordinates(new Coordinate(10, 20), new Coordinate(190, 280)));
            root.Set("Rotate", (Number)90);
            first.Dictionary.Unset("Resources"); first.Dictionary.Unset("MediaBox");
            await pdf.AppendPageAsync(o => o.MediaBox = Rectangle.FromDimensions(200, 300));
            using var output = File.Create(source); await pdf.SaveAsync(output);
        }
        var editor = await EditorDocument.OpenAsync(source);
        var before = await editor.RenderAsync(1, 1);
        await editor.MovePageAsync(1, 2);
        var after = await editor.RenderAsync(2, 1);
        Assert.Equal(before.PngBytes.ToArray(), after.PngBytes.ToArray());
        Assert.Equal(90, after.Geometry.RotationDegrees);
        Assert.Equal(260, after.Geometry.DisplayWidth);
        Assert.Equal(180, after.Geometry.DisplayHeight);
    }

    [Fact]
    public async Task AppendAndExtract_PreserveContentAndOriginal()
    {
        var editor = await EditorDocument.OpenAsync(await CreatePdfAsync());
        await editor.AppendAsync(await CreatePdfAsync(pages: 2));
        Assert.Equal(3, editor.PageCount);
        var id = await editor.AddTextAsync(3, 20, 20); editor.UpdateText(id, "Last page", 12);
        await editor.SaveCopyAsync(FilePath("one.pdf"), 3);
        Assert.True(editor.IsDirty);
        using var stream = File.OpenRead(FilePath("one.pdf")); using var pdf = Pdf.Load(stream);
        Assert.Equal(1, await pdf.GetPageCountAsync());
        Assert.Contains("Last page", string.Join(" ", (await pdf.ExtractTextAsync()).Select(t => t.Text)));
    }

    [Fact]
    public async Task UnsupportedInputAndInvalidChanges_LeaveStateAndFilesUntouched()
    {
        var path = await CreatePdfAsync(); var original = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        var editor = await EditorDocument.OpenAsync(path);
        await Assert.ThrowsAsync<InvalidOperationException>(() => editor.SaveCopyAsync(path));
        await Assert.ThrowsAsync<InvalidOperationException>(() => editor.DeletePageAsync(1));
        var id = await editor.AddTextAsync(1, 30, 30);
        Assert.Throws<ArgumentException>(() => editor.UpdateText(id, "Unsupported 😀", 16));
        Assert.Equal("Your text", editor.AddedText[0].Text);
        var formSource = await CreatePdfAsync(forms: true);
        await Assert.ThrowsAsync<NotSupportedException>(() => editor.AppendAsync(formSource));
        Assert.Equal(1, editor.PageCount);
        Assert.Equal(original, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        var failureTarget = FilePath("absent") + Path.DirectorySeparatorChar + "file.pdf";
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => editor.SaveCopyAsync(failureTarget));
        Assert.True(editor.IsDirty);
    }

    [Fact]
    public async Task SignedAndEncryptedDocuments_AreRejectedBeforeEditing()
    {
        using (var pdf = Pdf.Create())
        {
            await pdf.EncryptAsync("owner", "user");
            using var output = File.Create(FilePath("encrypted.pdf")); await pdf.SaveAsync(output);
        }
        var rejected = await Assert.ThrowsAnyAsync<Exception>(() => EditorDocument.OpenAsync(FilePath("encrypted.pdf")));
        Assert.Matches("(?i)(password|encrypted)", rejected.Message);
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Editor test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        using (var pdf = Pdf.Create())
        {
            await pdf.SignInvisibleAsync(cert);
            using var output = File.Create(FilePath("signed.pdf")); await pdf.SaveAsync(output);
        }
        await Assert.ThrowsAsync<NotSupportedException>(() => EditorDocument.OpenAsync(FilePath("signed.pdf")));
    }

    private static int CountDark(Image<Rgba32> image, int x0, int y0, int x1, int y1)
    {
        var count = 0;
        for (var y = y0; y < Math.Min(y1, image.Height); y++) for (var x = x0; x < Math.Min(x1, image.Width); x++)
            if (image[x, y].R < 150 && image[x, y].G < 150 && image[x, y].B < 150) count++;
        return count;
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
