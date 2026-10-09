using System.Text;
using FluentAssertions;
using ZingPDF;
using ZingPDF.Extensions;
using ZingPDF.Elements.Forms;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.Elements.Forms.FieldTypes.Choice;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using Xunit;

namespace ZingPDF.Tests.Smoke;

public class ExternalFormFlatteningTests
{
    [Fact]
    public async Task FlattenAsync_WithWidgetMissingNormalAppearance_RejectsBeforeChangingDocument()
    {
        // This input is authored as PDF syntax, independently of ZingPDF's form builder.
        using var input = CreateExternalPdfWithValidThenMissingAppearance();
        using var pdf = Pdf.Load(input);
        var form = await pdf.GetFormAsync();

        form.Should().NotBeNull();
        var page = await pdf.GetPageAsync(1);
        var originalAnnotations = await page.Dictionary.Annots.GetAsync();
        var originalContentsReference = page.Dictionary.GetAs<IndirectObjectReference>("Contents");

        var act = () => form!.FlattenAsync();

        await act.Should().ThrowAsync<InvalidPdfException>()
            .WithMessage("Cannot flatten widget *: it has no usable normal appearance stream for its current /AS state.");

        (await pdf.GetFormAsync()).Should().NotBeNull();
        (await page.Dictionary.Annots.GetAsync()).Should().HaveCount(originalAnnotations!.Count());
        page.Dictionary.GetAs<IndirectObjectReference>("Contents").Should().Be(originalContentsReference);

        var fields = await form!.GetFieldsAsync();
        fields.Should().HaveCount(2);
        var currentAnnotations = await page.Dictionary.Annots.GetAsync();
        currentAnnotations.Should().HaveCount(2);
        foreach (var reference in currentAnnotations!.OfType<IndirectObjectReference>())
        {
            (await pdf.Objects.GetAsync(reference)).Object.Should().BeAssignableTo<WidgetAnnotationDictionary>();
        }
    }

    [Fact]
    public async Task FlattenAsync_WithXfaData_RejectsWithoutRemovingAcroForm()
    {
        using var input = CreateExternalPdfWithXfa();
        using var pdf = Pdf.Load(input);
        var form = await pdf.GetFormAsync();

        var act = () => form!.FlattenAsync();

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("Flattening XFA forms is not supported.*");
        (await pdf.GetFormAsync()).Should().NotBeNull();
    }

    [Fact]
    public async Task TextField_SetValue_WithInheritedAppearanceAndRepeatedWidgets_RegeneratesEveryWidgetAppearance()
    {
        using var input = CreateExternalInheritedRepeatedWidgetPdf();
        using var pdf = Pdf.Load(input);
        var form = await pdf.GetFormAsync();
        var field = await form!.GetFieldAsync<TextFormField>("profile.name");
        field.Should().NotBeNull();
        (await field!.GetValueAsync()).Should().Be("before");

        await field.SetValueAsync("Zoë Lovelace\nAnalytical Engine");
        using var output = new MemoryStream();
        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reopened = Pdf.Load(output);
        var reopenedForm = await reopened.GetFormAsync();
        var reopenedField = await reopenedForm!.GetFieldAsync<TextFormField>("profile.name");
        (await reopenedField!.GetValueAsync()).Should().Be("Zoë Lovelace\nAnalytical Engine");

        var page = await reopened.GetPageAsync(1);
        var widgets = new List<WidgetAnnotationDictionary>();
        foreach (var reference in (await page.Dictionary.Annots.GetAsync())!.OfType<IndirectObjectReference>())
        {
            var widget = (WidgetAnnotationDictionary)(await reopened.Objects.GetAsync(reference)).Object;
            var appearance = await widget.AP.GetAsync();
            appearance.Should().NotBeNull();
            var normal = await appearance!.N.GetAsync();
            normal.Value.Should().BeAssignableTo<IStreamObject>();
            widgets.Add(widget);
        }

        widgets.Should().HaveCount(2);
        await reopenedForm.FlattenAsync();
        using var flattened = new MemoryStream();
        await reopened.SaveAsync(flattened);
        flattened.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExternalCheckboxRadioComboAndListSelections_SaveAndFlattenWithAppearances()
    {
        using var input = CreateExternalButtonAndChoicePdf();
        using var pdf = Pdf.Load(input);
        var form = await pdf.GetFormAsync();

        var checkbox = await form!.GetFieldAsync<CheckboxFormField>("agreement");
        var checkboxOption = (await checkbox!.GetOptionsAsync()).Single();
        checkboxOption.Value.Should().Be("accept");
        await checkboxOption.SelectAsync();

        var radio = await form.GetFieldAsync<RadioButtonFormField>("delivery");
        var radioOption = (await radio!.GetOptionsAsync()).Single(option => option.Value == "pickup");
        await radioOption.SelectAsync();

        var combo = await form.GetFieldAsync<ComboBoxFormField>("country");
        (await combo!.GetOptionsAsync()).Select(option => option.Value.Decode()).Should().Contain(["us", "ca"]);
        (await combo!.SelectOptionByValueAsync("ca")).Should().BeTrue();

        var list = await form.GetFieldAsync<ListBoxFormField>("regions");
        (await list!.SelectOptionByValueAsync("vic")).Should().BeTrue();

        using var saved = new MemoryStream();
        await pdf.SaveAsync(saved);
        saved.Position = 0;
        using var reopened = Pdf.Load(saved);
        var reopenedForm = await reopened.GetFormAsync();
        (await (await reopenedForm!.GetFieldAsync<CheckboxFormField>("agreement"))!.GetOptionsAsync())
            .Single().Selected.Should().BeTrue();
        (await (await reopenedForm.GetFieldAsync<RadioButtonFormField>("delivery"))!.GetOptionsAsync())
            .Single(option => option.Value == "pickup").Selected.Should().BeTrue();
        (await (await reopenedForm.GetFieldAsync<ComboBoxFormField>("country"))!.GetOptionsAsync())
            .Single(option => option.Value.Decode() == "ca").Selected.Should().BeTrue();
        (await (await reopenedForm.GetFieldAsync<ListBoxFormField>("regions"))!.GetOptionsAsync())
            .Single(option => option.Value.Decode() == "vic").Selected.Should().BeTrue();

        await reopenedForm.FlattenAsync();
        using var flattened = new MemoryStream();
        await reopened.SaveAsync(flattened);
        flattened.Position = 0;
        using var flattenedPdf = Pdf.Load(flattened);
        (await flattenedPdf.GetFormAsync()).Should().BeNull();
    }

    [Fact]
    public async Task FlattenAsync_Directly_WalksNestedFieldWithoutLocalFieldType()
    {
        using var input = CreateExternalInheritedHierarchyWithAppearances();
        using var pdf = Pdf.Load(input);
        var form = await pdf.GetFormAsync();

        // Do not enumerate fields first: flatten must discover inherited terminal fields on its own.
        await form!.FlattenAsync();

        using var output = new MemoryStream();
        await pdf.SaveAsync(output);
        output.Position = 0;
        using var reopened = Pdf.Load(output);
        (await reopened.GetFormAsync()).Should().BeNull();
    }

    [Fact]
    public async Task NamedMergedWidgetChild_IsDiscoveredFilledAndFlattened()
    {
        using var input = CreateExternalNamedMergedWidgetChild();
        using var pdf = Pdf.Load(input);
        var form = await pdf.GetFormAsync();
        var field = await form!.GetFieldAsync<TextFormField>("profile.name");
        field.Should().NotBeNull();

        await field!.SetValueAsync("Ada Lovelace");
        await form.FlattenAsync();
        (await pdf.GetFormAsync()).Should().BeNull();
    }

    [Fact]
    public async Task FlattenAsync_WithMultipleAppearanceStatesAndMissingAs_RejectsAmbiguousSelection()
    {
        using var input = CreateExternalButtonWithMissingAppearanceState();
        using var pdf = Pdf.Load(input);
        var form = await pdf.GetFormAsync();
        var page = await pdf.GetPageAsync(1);
        var originalContent = page.Dictionary.GetAs<IndirectObjectReference>("Contents");

        var act = () => form!.FlattenAsync();

        await act.Should().ThrowAsync<InvalidPdfException>()
            .WithMessage("Cannot flatten widget *: it has no usable normal appearance stream for its current /AS state.");
        (await pdf.GetFormAsync()).Should().NotBeNull();
        page.Dictionary.GetAs<IndirectObjectReference>("Contents").Should().Be(originalContent);
    }

    [Fact]
    public async Task FlattenAsync_WithAppearanceStreamMissingBBox_RejectsBeforeChangingDocument()
    {
        using var input = CreateExternalButtonWithMissingAppearanceBBox();
        using var pdf = Pdf.Load(input);
        var form = await pdf.GetFormAsync();

        var act = () => form!.FlattenAsync();

        await act.Should().ThrowAsync<InvalidPdfException>()
            .WithMessage("Cannot flatten widget appearance: its /BBox entry is missing.");
        (await pdf.GetFormAsync()).Should().NotBeNull();
    }

    [Fact]
    public async Task FlattenAsync_PreservesPageWidgetAppearanceWhenWidgetIsMissingFromAcroFormFields()
    {
        using var input = CreateExternalOrphanWidgetPdf();
        using var pdf = Pdf.Load(input);
        var form = await pdf.GetFormAsync();

        await form!.FlattenAsync();

        (await pdf.GetFormAsync()).Should().BeNull();
        var page = await pdf.GetPageAsync(1);
        (await page.Dictionary.Annots.GetAsync()).Should().BeNull();
        (await page.Dictionary.Contents.GetAsync()).Should().NotBeNull();
    }

    [Fact]
    public async Task FlattenAsync_RemovesEmptyZeroAreaWidgetWithoutAddingPageContent()
    {
        using var input = CreateExternalEmptyZeroAreaWidgetPdf();
        using var pdf = Pdf.Load(input);
        var form = await pdf.GetFormAsync();
        var page = await pdf.GetPageAsync(1);
        var originalContents = page.Dictionary.GetAs<IndirectObjectReference>("Contents");

        await form!.FlattenAsync();

        (await pdf.GetFormAsync()).Should().BeNull();
        (await page.Dictionary.Annots.GetAsync()).Should().BeNull();
        page.Dictionary.GetAs<IndirectObjectReference>("Contents").Should().Be(originalContents);
    }

    private static MemoryStream CreateExternalPdfWithValidThenMissingAppearance()
        => CreateExternalPdf(xfa: false, includeValidAppearance: true);

    private static MemoryStream CreateExternalPdfWithXfa()
        => CreateExternalPdf(xfa: true, includeValidAppearance: false);

    private static MemoryStream CreateExternalInheritedRepeatedWidgetPdf()
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 320 300] /Resources << >> /Contents 5 0 R /Annots [8 0 R 9 0 R] >>",
            "<< /Fields [6 0 R] /DA (/Helv 10 Tf 0 g) /DR << /Font << /Helv 10 0 R >> >> >>",
            "<< /Length 0 >>\nstream\n\nendstream",
            "<< /T (profile) /FT /Tx /V (before) /DA (/Helv 12 Tf 0 g) /Ff 4096 /Q 1 /Kids [7 0 R] >>",
            "<< /T (name) /Parent 6 0 R /Kids [8 0 R 9 0 R] >>",
            "<< /Type /Annot /Subtype /Widget /Parent 7 0 R /P 3 0 R /Rect [40 190 280 240] >>",
            "<< /Type /Annot /Subtype /Widget /Parent 7 0 R /P 3 0 R /Rect [40 120 280 170] >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"
        };

        var builder = new StringBuilder("%PDF-1.7\n");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer\n<< /Size ").Append(objects.Count + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xrefOffset).Append("\n%%EOF\n");

        return new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
    }

    private static MemoryStream CreateExternalButtonAndChoicePdf()
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 360 360] /Resources << >> /Contents 5 0 R /Annots [6 0 R 8 0 R 9 0 R 10 0 R 11 0 R] >>",
            "<< /Fields [6 0 R 7 0 R 10 0 R 11 0 R] /DA (/Helv 10 Tf 0 g) /DR << /Font << /Helv 12 0 R >> >> >>",
            "<< /Length 0 >>\nstream\n\nendstream",
            "<< /Type /Annot /Subtype /Widget /FT /Btn /T (agreement) /Opt [(accept)] /V /Off /AS /Off /Rect [20 300 40 320] /P 3 0 R >>",
            "<< /FT /Btn /T (delivery) /Ff 32768 /Opt [(mail) (pickup)] /V /Off /Kids [8 0 R 9 0 R] >>",
            "<< /Type /Annot /Subtype /Widget /Parent 7 0 R /P 3 0 R /Rect [20 260 40 280] >>",
            "<< /Type /Annot /Subtype /Widget /Parent 7 0 R /P 3 0 R /Rect [60 260 80 280] >>",
            "<< /Type /Annot /Subtype /Widget /FT /Ch /T (country) /Ff 131072 /Opt [[(us) (United States)] [(ca) (Canada)]] /V (us) /Rect [20 200 240 230] /P 3 0 R >>",
            "<< /Type /Annot /Subtype /Widget /FT /Ch /T (regions) /Ff 2097152 /Opt [[(nsw) (New South Wales)] [(vic) (Victoria)]] /V [(nsw)] /Rect [20 140 240 190] /P 3 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"
        };

        var builder = new StringBuilder("%PDF-1.7\n");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer\n<< /Size ").Append(objects.Count + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xrefOffset).Append("\n%%EOF\n");

        return new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
    }

    private static MemoryStream CreateExternalInheritedHierarchyWithAppearances()
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 320 300] /Resources << >> /Contents 5 0 R /Annots [8 0 R 9 0 R] >>",
            "<< /Fields [6 0 R] >>",
            "<< /Length 0 >>\nstream\n\nendstream",
            "<< /T (profile) /FT /Tx /Kids [7 0 R] >>",
            "<< /T (name) /Parent 6 0 R /Kids [8 0 R 9 0 R] >>",
            "<< /Type /Annot /Subtype /Widget /Parent 7 0 R /P 3 0 R /Rect [40 190 280 240] /AP << /N 10 0 R >> >>",
            "<< /Type /Annot /Subtype /Widget /Parent 7 0 R /P 3 0 R /Rect [40 120 280 170] /AP << /N 10 0 R >> >>",
            "<< /Type /XObject /Subtype /Form /BBox [0 0 240 50] /Matrix [0 1 -1 0 50 0] /Resources << >> /Length 0 >>\nstream\n\nendstream"
        };

        var builder = new StringBuilder("%PDF-1.7\n");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer\n<< /Size ").Append(objects.Count + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xrefOffset).Append("\n%%EOF\n");

        return new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
    }

    private static MemoryStream CreateExternalButtonWithMissingAppearanceState()
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << >> /Contents 5 0 R /Annots [6 0 R] >>",
            "<< /Fields [6 0 R] >>",
            "<< /Length 0 >>\nstream\n\nendstream",
            "<< /Type /Annot /Subtype /Widget /FT /Btn /T (approval) /V /Yes /Rect [20 20 60 60] /P 3 0 R /AP << /N << /Off 7 0 R /Yes 8 0 R >> >> >>",
            "<< /Type /XObject /Subtype /Form /BBox [0 0 40 40] /Resources << >> /Length 0 >>\nstream\n\nendstream",
            "<< /Type /XObject /Subtype /Form /BBox [0 0 40 40] /Resources << >> /Length 0 >>\nstream\n\nendstream"
        };

        var builder = new StringBuilder("%PDF-1.7\n");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer\n<< /Size ").Append(objects.Count + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xrefOffset).Append("\n%%EOF\n");

        return new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
    }

    private static MemoryStream CreateExternalButtonWithMissingAppearanceBBox()
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << >> /Contents 5 0 R /Annots [6 0 R] >>",
            "<< /Fields [6 0 R] >>",
            "<< /Length 0 >>\nstream\n\nendstream",
            "<< /Type /Annot /Subtype /Widget /FT /Btn /T (approval) /V /Yes /AS /Yes /Rect [20 20 60 60] /P 3 0 R /AP << /N 7 0 R >> >>",
            "<< /Type /XObject /Subtype /Form /Resources << >> /Length 0 >>\nstream\n\nendstream"
        };

        var builder = new StringBuilder("%PDF-1.7\n");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer\n<< /Size ").Append(objects.Count + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xrefOffset).Append("\n%%EOF\n");

        return new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
    }

    private static MemoryStream CreateExternalOrphanWidgetPdf()
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << >> /Contents 5 0 R /Annots [6 0 R] >>",
            "<< /Fields [] >>",
            "<< /Length 0 >>\nstream\n\nendstream",
            "<< /Type /Annot /Subtype /Widget /FT /Tx /T (orphan) /V (visible) /Rect [20 20 180 60] /P 3 0 R /AP << /N 7 0 R >> >>",
            "<< /Type /XObject /Subtype /Form /BBox [0 0 160 40] /Resources << >> /Length 0 >>\nstream\n\nendstream"
        };

        var builder = new StringBuilder("%PDF-1.7\n");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer\n<< /Size ").Append(objects.Count + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xrefOffset).Append("\n%%EOF\n");

        return new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
    }

    private static MemoryStream CreateExternalEmptyZeroAreaWidgetPdf()
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << >> /Contents 5 0 R /Annots [6 0 R] >>",
            "<< /Fields [6 0 R] >>",
            "<< /Length 0 >>\nstream\n\nendstream",
            "<< /Type /Annot /Subtype /Widget /FT /Tx /T (empty) /Rect [20 20 180 20] /P 3 0 R /AP << /N 7 0 R >> >>",
            "<< /Type /XObject /Subtype /Form /BBox [0 0 160 0] /Resources << >> /Length 0 >>\nstream\n\nendstream"
        };

        var builder = new StringBuilder("%PDF-1.7\n");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer\n<< /Size ").Append(objects.Count + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xrefOffset).Append("\n%%EOF\n");

        return new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
    }

    private static MemoryStream CreateExternalNamedMergedWidgetChild()
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 240 200] /Resources << >> /Contents 5 0 R /Annots [7 0 R] >>",
            "<< /Fields [6 0 R] /DA (/Helv 10 Tf 0 g) /DR << /Font << /Helv 9 0 R >> >> >>",
            "<< /Length 0 >>\nstream\n\nendstream",
            "<< /T (profile) /FT /Tx /DA (/Helv 12 Tf 0 g) /Kids [7 0 R] >>",
            "<< /Type /Annot /Subtype /Widget /T (name) /Parent 6 0 R /Rect [20 100 220 150] /P 3 0 R >>",
            "<< /Type /XObject /Subtype /Form /BBox [0 0 200 50] /Resources << >> /Length 0 >>\nstream\n\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"
        };

        var builder = new StringBuilder("%PDF-1.7\n");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer\n<< /Size ").Append(objects.Count + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xrefOffset).Append("\n%%EOF\n");

        return new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
    }

    private static MemoryStream CreateExternalPdf(bool xfa, bool includeValidAppearance)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 300] /Resources << >> /Contents 5 0 R /Annots [6 0 R 7 0 R] >>",
            xfa
                ? "<< /Fields [6 0 R 7 0 R] /XFA (external XFA packet) >>"
                : "<< /Fields [6 0 R 7 0 R] >>",
            "<< /Length 0 >>\nstream\n\nendstream",
            includeValidAppearance
                ? "<< /Type /Annot /Subtype /Widget /FT /Tx /T (first) /Rect [20 240 140 270] /P 3 0 R /V (first value) /AP << /N 8 0 R >> >>"
                : "<< /Type /Annot /Subtype /Widget /FT /Tx /T (first) /Rect [20 240 140 270] /P 3 0 R /V (first value) >>",
            "<< /Type /Annot /Subtype /Widget /FT /Tx /T (second) /Rect [20 200 140 230] /P 3 0 R /V (second value) >>"
        };

        if (includeValidAppearance)
        {
            objects.Add("<< /Type /XObject /Subtype /Form /BBox [0 0 120 30] /Resources << >> /Length 0 >>\nstream\n\nendstream");
        }

        var builder = new StringBuilder("%PDF-1.7\n");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(objects.Count + 1).Append("\n");
        builder.Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer\n<< /Size ").Append(objects.Count + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xrefOffset).Append("\n%%EOF\n");

        return new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
    }
}
