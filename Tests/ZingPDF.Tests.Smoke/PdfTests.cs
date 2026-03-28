using FluentAssertions;
using System.Text;
using Xunit;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.Elements.Forms.FieldTypes.Choice;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Elements;
using ZingPDF.Graphics;
using ZingPDF.Extensions;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Text;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Tests.Smoke.TestFiles;

namespace ZingPDF;

public class PdfTests
{
    [Fact]
    public async Task EncryptedPdf_RequiresAuthentication()
    {
        var pdf = Pdf.Load(Files.AsStream(Files.Encrypted));

        var act = async () => await pdf.GetPageCountAsync();

        var exception = await Assert.ThrowsAnyAsync<Exception>(act);

        exception.GetType().Name.Should().Be("PdfAuthenticationException");
    }

    [Fact]
    public async Task EncryptedPdf_CanBeDecryptedWithPassword()
    {
        var pdf = Pdf.Load(Files.AsStream(Files.Encrypted));

        await pdf.AuthenticateAsync("kanbanery");

        var pageCount = await pdf.GetPageCountAsync();

        pageCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AppendPage_PageCount()
    {
        using var pdf = Pdf.Create();

        await pdf.AppendPageAsync();

        var pageCount = await pdf.GetPageCountAsync();

        pageCount.Should().Be(2);
    }

    [Fact]
    public async Task InsertPage_PageCount()
    {
        using var pdf = Pdf.Create();

        await pdf.InsertPageAsync(1);

        var pageCount = await pdf.GetPageCountAsync();

        pageCount.Should().Be(2);
    }

    [Fact]
    public async Task DeletePage_PageCount()
    {
        using var pdf = Pdf.Create();

        await pdf.DeletePageAsync(1);

        var pageCount = await pdf.GetPageCountAsync();

        pageCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPage_PageProperties()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));

        var page = await pdf.GetPageAsync(1);

        page.Dictionary.MediaBox.Should().NotBeNull();
    }

    [Fact]
    public async Task AddWatermarkAsync_SavesModifiedPdf()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));
        using var output = new MemoryStream();

        await pdf.AddWatermarkAsync("FAST");
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("watermark-minimal.pdf", output);

        output.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AppendPdfAsync_AppendsPagesFromSecondDocument()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));
        using var appendedStream = Files.AsStream(Files.Minimal2);
        using var output = new MemoryStream();

        await pdf.AppendPdfAsync(appendedStream);
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("append-minimal1-minimal2.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        (await reloaded.GetPageCountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Page_AddTextAsync_PersistsWrittenContent()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Test));
        using var output = new MemoryStream();

        var page = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromDimensions(200, 200));

        await page.AddTextAsync(new TextObject(
            "test",
            Rectangle.FromDimensions(200, 200),
            new FontOptions
            {
                ResourceName = "Helv",
                Size = 24,
                Colour = RGBColour.PrimaryRed
                    }));

        await pdf.SaveAsync(output);
        await WriteArtifactAsync("page-add-text.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedPage = await reloaded.GetPageAsync(1);
        var contents = await reloadedPage.Dictionary.Contents.GetAsync();

        (await reloaded.GetPageCountAsync()).Should().BeGreaterThan(1);
        contents.Should().NotBeNull();
        contents!.Count().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Page_AddImageAsync_WritesValidImageXObject()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));
        using var output = new MemoryStream();
        using var image = Image.FromFile(Files.CatImage, Rectangle.FromDimensions(200, 200));

        var page = await pdf.GetPageAsync(1);

        await page.AddImageAsync(image);
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("page-add-image.pdf", output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("/Subtype /Image");
        writtenPdf.Should().Contain("/Type /XObject");
        writtenPdf.Should().Contain("/Length ");
        writtenPdf.Should().Contain("/Resources <</XObject <<");
        writtenPdf.Should().Contain(" Do");
    }

    [Fact]
    public async Task DecompressAsync_DoesNotThrow()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Form));

        await pdf.DecompressAsync();
    }

    [Fact]
    public async Task Compress_ImageHeavyFixture_DoesNotThrow()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedImageHeavy));
        using var output = new MemoryStream();

        pdf.Compress(144, 75);
        await pdf.SaveAsync(output);

        output.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Page_RotateAsync_PersistsPageRotation()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Test));
        using var output = new MemoryStream();

        var page = await pdf.GetPageAsync(1);
        await page.RotateAsync(Rotation.Degrees90);
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("page-rotate.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedPage = await reloaded.GetPageAsync(1);

        ((int)(await reloadedPage.Dictionary.Rotate.GetAsync())!).Should().Be(90);
    }

    [Fact]
    public async Task SetRotationAsync_SavesModifiedPdf()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));
        using var output = new MemoryStream();

        await pdf.SetRotationAsync(Rotation.Degrees90);
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("document-rotate.pdf", output);
        output.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task StreamObject_GetDecompressedDataAsync_CanReadUnfilteredStreamMultipleTimes()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Test));

        var firstStream = await GetFirstStreamObjectAsync(pdf);

        using var firstRead = await firstStream.GetDecompressedDataAsync();
        var firstBytes = await ReadAllBytesAsync(firstRead);

        using var secondRead = await firstStream.GetDecompressedDataAsync();
        var secondBytes = await ReadAllBytesAsync(secondRead);

        secondBytes.Should().Equal(firstBytes);
    }

    [Fact]
    public async Task ExtractTextAsync_TextHeavyFixture_ReturnsNonEmptySegments()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));

        var extracted = (await pdf.ExtractTextAsync()).ToList();

        extracted.Should().NotBeEmpty();
        extracted.Any(x => !string.IsNullOrWhiteSpace(x.Text)).Should().BeTrue();
    }

    [Fact]
    public async Task EncryptAsync_SavesEncryptedPdf()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        await pdf.EncryptAsync("secret-password");
        await pdf.SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("/Encrypt");

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        await reloaded.AuthenticateAsync("secret-password");
        var pageCount = await reloaded.GetPageCountAsync();
        pageCount.Should().Be(1);
    }

    [Fact]
    public async Task GetFormAsync_ExposesPublicButtonFieldTypes()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));

        var form = await pdf.GetFormAsync();
        var fields = await form!.GetFieldsAsync();

        fields.OfType<CheckboxFormField>().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFormAsync_FieldBoundsInspection_IsAvailable()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));

        var form = await pdf.GetFormAsync();
        var field = (await form!.GetFieldsAsync()).First();

        var bounds = await field.GetFieldBoundsAsync();
        var dimensions = await field.GetFieldDimensionsAsync();

        bounds.LowerLeft.X.Should().BeGreaterThanOrEqualTo(0);
        bounds.LowerLeft.Y.Should().BeGreaterThanOrEqualTo(0);
        bounds.UpperRight.X.Should().BeGreaterThan(bounds.LowerLeft.X);
        bounds.UpperRight.Y.Should().BeGreaterThan(bounds.LowerLeft.Y);
        bounds.Size.Width.Should().Be(dimensions.Width);
        bounds.Size.Height.Should().Be(dimensions.Height);
    }

    [Fact]
    public async Task CheckboxFormField_SelectOption_PersistsAfterSave()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));
        using var output = new MemoryStream();

        var form = await pdf.GetFormAsync();
        var checkbox = (await form!.GetFieldsAsync()).OfType<CheckboxFormField>().First(x => x.Name == "Phone1");
        var option = (await checkbox.GetOptionsAsync()).Single();

        option.Selected.Should().BeFalse();

        await option.SelectAsync();
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("form-checkbox-select.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedForm = await reloaded.GetFormAsync();
        var reloadedCheckbox = (await reloadedForm!.GetFieldsAsync()).OfType<CheckboxFormField>().First(x => x.Name == "Phone1");
        var reloadedOption = (await reloadedCheckbox.GetOptionsAsync()).Single();

        reloadedOption.Selected.Should().BeTrue();
        reloadedOption.Value.Should().Be(option.Value);
    }

    [Fact]
    public async Task ChoiceFormField_SelectOption_ThenSave_DoesNotThrow()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComboboxForm));
        using var output = new MemoryStream();

        var form = await pdf.GetFormAsync();
        var choiceField = (await form!.GetFieldsAsync()).OfType<ChoiceFormField>().First();
        var option = (await choiceField.GetOptionsAsync()).First();

        await option.SelectAsync();
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("form-choice-select.pdf", output);
        output.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TextFormField_SetValue_PersistsAfterSave()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));
        using var output = new MemoryStream();

        var form = await pdf.GetFormAsync();
        var textField = (await form!.GetFieldsAsync()).OfType<TextFormField>().First();

        await textField.SetValueAsync("test");
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("form-text-set-value.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedForm = await reloaded.GetFormAsync();
        var reloadedTextField = (await reloadedForm!.GetFieldsAsync()).OfType<TextFormField>().First(x => x.Name == textField.Name);

        (await reloadedTextField.GetValueAsync()).Should().Be("test");
    }

    [Fact]
    public async Task TextFormField_ClearAsync_ThenSave_DoesNotThrow()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));
        using var output = new MemoryStream();

        var form = await pdf.GetFormAsync();
        var textField = (await form!.GetFieldsAsync()).OfType<TextFormField>().First();

        await textField.SetValueAsync("test");
        await textField.ClearAsync();
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("form-text-clear.pdf", output);
        output.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Metadata_CanBeEdited_AndRoundTrips()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        var metadata = await pdf.GetMetadataAsync();
        metadata.Title = "Quarterly Report";
        metadata.Author = "Taylor Smith";
        metadata.Subject = "Financial summary";
        metadata.Keywords = "finance,quarterly";
        metadata.Creator = "Integration Test";
        metadata.CreationDate = new DateTimeOffset(2025, 04, 01, 9, 30, 0, TimeSpan.FromHours(10));

        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedMetadata = await reloaded.GetMetadataAsync();

        reloadedMetadata.Title.Should().Be("Quarterly Report");
        reloadedMetadata.Author.Should().Be("Taylor Smith");
        reloadedMetadata.Subject.Should().Be("Financial summary");
        reloadedMetadata.Keywords.Should().Be("finance,quarterly");
        reloadedMetadata.Creator.Should().Be("Integration Test");
        reloadedMetadata.CreationDate.Should().Be(new DateTimeOffset(2025, 04, 01, 9, 30, 0, TimeSpan.FromHours(10)));
    }

    [Fact]
    public async Task SaveAsync_StampsProducerWithZingPdf()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var metadata = await reloaded.GetMetadataAsync();

        metadata.Producer.Should().Be(PdfMetadata.ProducerName);
        metadata.ModifiedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task EncryptAsync_AllowsOwnerPasswordAuthentication()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        await pdf.EncryptAsync("user-secret", "owner-secret");
        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        await reloaded.AuthenticateAsync("owner-secret");
        var pageCount = await reloaded.GetPageCountAsync();
        pageCount.Should().Be(1);
    }

    [Fact]
    public async Task EncryptAsync_ThenDecryptAsync_RoundTripsToPlainPdf()
    {
        using var pdf = Pdf.Create();
        using var encryptedOutput = new MemoryStream();

        await pdf.EncryptAsync("secret-password");
        await pdf.SaveAsync(encryptedOutput);

        encryptedOutput.Position = 0;
        using var encryptedPdf = Pdf.Load(encryptedOutput);

        await encryptedPdf.DecryptAsync("secret-password");

        using var decryptedOutput = new MemoryStream();
        await encryptedPdf.SaveAsync(decryptedOutput);

        decryptedOutput.Position = 0;
        using var reloaded = Pdf.Load(decryptedOutput);
        var trailer = await reloaded.Objects.GetLatestTrailerDictionaryAsync();

        (await trailer.Encrypt.GetAsync()).Should().BeNull();

        var pageCount = await reloaded.GetPageCountAsync();
        pageCount.Should().Be(1);
    }

    private static async Task<IStreamObject> GetFirstStreamObjectAsync(Pdf pdf)
    {
        await foreach (var obj in pdf.Objects)
        {
            if (obj.Object is IStreamObject streamObject)
            {
                return streamObject;
            }
        }

        throw new InvalidOperationException("Expected a stream object in the PDF.");
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        return memory.ToArray();
    }

    private static async Task WriteArtifactAsync(string fileName, MemoryStream output)
    {
        var artifactDirectory = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "artifacts", "manual-verification");
        Directory.CreateDirectory(artifactDirectory);

        var artifactPath = Path.Combine(artifactDirectory, fileName);

        output.Position = 0;
        await File.WriteAllBytesAsync(artifactPath, output.ToArray());
    }
}
