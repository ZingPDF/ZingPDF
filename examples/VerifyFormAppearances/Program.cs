using System.Diagnostics;
using System.ComponentModel;
using System.Text;
using ZingPDF;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.Elements.Forms.FieldTypes.Choice;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Extensions;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

var outputDirectory = Path.GetFullPath(args.FirstOrDefault() ?? "output/verify-form-appearances");
Directory.CreateDirectory(outputDirectory);
var sourcePath = Path.Combine(outputDirectory, "external-inherited-repeated-widgets.pdf");
var filledPath = Path.Combine(outputDirectory, "filled-and-reopened.pdf");
var flattenedPath = Path.Combine(outputDirectory, "flattened.pdf");
var expectedValue = "Zoë Lovelace\nAnalytical Engine";

await File.WriteAllBytesAsync(sourcePath, CreateExternalAcroFormPdf());
using (var input = File.OpenRead(sourcePath))
using (var pdf = Pdf.Load(input))
{
    var form = await pdf.GetFormAsync() ?? throw new InvalidOperationException("The external PDF did not expose an AcroForm.");
    var field = await form.GetFieldAsync<TextFormField>("profile.name")
        ?? throw new InvalidOperationException("The inherited text field was not discovered.");

    await field.SetValueAsync(expectedValue);

    var checkbox = await form.GetFieldAsync<CheckboxFormField>("agreement")
        ?? throw new InvalidOperationException("The external checkbox was not discovered.");
    await (await checkbox.GetOptionsAsync()).Single().SelectAsync();

    var radio = await form.GetFieldAsync<RadioButtonFormField>("delivery")
        ?? throw new InvalidOperationException("The external radio group was not discovered.");
    await (await radio.GetOptionsAsync()).Single(option => option.Value == "pickup").SelectAsync();

    var combo = await form.GetFieldAsync<ComboBoxFormField>("country")
        ?? throw new InvalidOperationException("The external combo box was not discovered.");
    if (!await combo.SelectOptionByValueAsync("ca"))
    {
        throw new InvalidOperationException("The combo box export value was not found.");
    }

    var list = await form.GetFieldAsync<ListBoxFormField>("regions")
        ?? throw new InvalidOperationException("The external list box was not discovered.");
    if (!await list.SelectOptionByValueAsync("vic"))
    {
        throw new InvalidOperationException("The list box export value was not found.");
    }

    await using var output = File.Create(filledPath);
    await pdf.SaveAsync(output);
}

using (var input = File.OpenRead(filledPath))
using (var pdf = Pdf.Load(input))
{
    var form = await pdf.GetFormAsync() ?? throw new InvalidOperationException("The saved PDF lost its AcroForm.");
    var field = await form.GetFieldAsync<TextFormField>("profile.name")
        ?? throw new InvalidOperationException("The saved PDF lost its inherited text field.");
    if (await field.GetValueAsync() != expectedValue)
    {
        throw new InvalidOperationException("The filled field value did not survive save and reopen.");
    }

    if (!(await (await form.GetFieldAsync<CheckboxFormField>("agreement"))!.GetOptionsAsync()).Single().Selected
        || !(await (await form.GetFieldAsync<RadioButtonFormField>("delivery"))!.GetOptionsAsync()).Single(option => option.Value == "pickup").Selected
        || !(await (await form.GetFieldAsync<ComboBoxFormField>("country"))!.GetOptionsAsync()).Single(option => option.Value.Decode() == "ca").Selected
        || !(await (await form.GetFieldAsync<ListBoxFormField>("regions"))!.GetOptionsAsync()).Single(option => option.Value.Decode() == "vic").Selected)
    {
        throw new InvalidOperationException("A button or choice selection did not survive save and reopen.");
    }

    var widgets = 0;
    var pageCount = await pdf.GetPageCountAsync();
    for (var pageNumber = 1; pageNumber <= pageCount; pageNumber++)
    {
        var page = await pdf.GetPageAsync(pageNumber);
        foreach (var reference in (await page.Dictionary.Annots.GetAsync())?.OfType<IndirectObjectReference>() ?? [])
        {
            if ((await pdf.Objects.GetAsync(reference)).Object is WidgetAnnotationDictionary widget)
            {
                var ap = await widget.AP.GetAsync();
                var normal = ap is null ? null : await ap.N.GetAsync();
                if (normal?.Value is not IStreamObject && normal?.Value is not Dictionary)
                {
                    throw new InvalidOperationException($"Widget {reference} has no generated normal appearance stream.");
                }

                widgets++;
            }
        }
    }

    if (widgets != 7)
    {
        throw new InvalidOperationException($"Expected seven external widgets, found {widgets}.");
    }

    await form.FlattenAsync();
    await using var output = File.Create(flattenedPath);
    await pdf.SaveAsync(output);
}

using (var input = File.OpenRead(flattenedPath))
using (var pdf = Pdf.Load(input))
{
    if (await pdf.GetFormAsync() is not null)
    {
        throw new InvalidOperationException("The flattened PDF still has an AcroForm.");
    }

    var page = await pdf.GetPageAsync(1);
    var annotations = await page.Dictionary.Annots.GetAsync();
    foreach (var reference in annotations?.OfType<IndirectObjectReference>() ?? [])
    {
        if ((await pdf.Objects.GetAsync(reference)).Object is WidgetAnnotationDictionary)
        {
            throw new InvalidOperationException("The flattened PDF still has a widget annotation.");
        }
    }

    if (await page.Dictionary.Contents.GetAsync() is null)
    {
        throw new InvalidOperationException("The flattened page has no page content stream.");
    }
}

Console.WriteLine($"Saved source:   {sourcePath}");
Console.WriteLine($"Saved filled:   {filledPath}");
Console.WriteLine($"Saved flattened: {flattenedPath}");
    Console.WriteLine("Assertions passed: inherited text fill, repeated widgets, checkbox/radio and combo/list selection, save/reopen, and flatten.");
TryRenderWithPoppler(flattenedPath, outputDirectory);

static void TryRenderWithPoppler(string pdfPath, string outputDirectory)
{
    var prefix = Path.Combine(outputDirectory, "flattened-page");
    try
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "pdftoppm",
            UseShellExecute = false,
            ArgumentList = { "-png", "-r", "144", pdfPath, prefix }
        });
        if (process is null)
        {
            Console.WriteLine("Poppler was not available; inspect flattened.pdf with an independent PDF renderer.");
            return;
        }

        process.WaitForExit();
        if (process.ExitCode == 0)
        {
            Console.WriteLine($"Poppler rendered the flattened page to {prefix}-1.png");
        }
        else
        {
            Console.WriteLine("Poppler could not render the output; inspect flattened.pdf with another independent PDF renderer.");
        }
    }
    catch (Win32Exception)
    {
        Console.WriteLine("Poppler was not available; inspect flattened.pdf with an independent PDF renderer.");
    }
}

static byte[] CreateExternalAcroFormPdf()
{
    var objects = new List<string>
    {
        "<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>",
        "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
        "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 320 300] /Resources << >> /Contents 5 0 R /Annots [8 0 R 9 0 R 11 0 R 13 0 R 14 0 R 15 0 R 16 0 R] >>",
        "<< /Fields [6 0 R 11 0 R 12 0 R 15 0 R 16 0 R] /DA (/Helv 10 Tf 0 g) /DR << /Font << /Helv 10 0 R >> >> >>",
        "<< /Length 0 >>\nstream\n\nendstream",
        "<< /T (profile) /FT /Tx /DA (/Helv 12 Tf 0 g) /Ff 4096 /Q 1 /Kids [7 0 R] >>",
        "<< /T (name) /Parent 6 0 R /Kids [8 0 R 9 0 R] >>",
        "<< /Type /Annot /Subtype /Widget /Parent 7 0 R /P 3 0 R /Rect [40 190 280 240] >>",
        "<< /Type /Annot /Subtype /Widget /Parent 7 0 R /P 3 0 R /Rect [40 120 280 170] >>",
        "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
        "<< /Type /Annot /Subtype /Widget /FT /Btn /T (agreement) /Opt [(accept)] /V /Off /AS /Off /Rect [20 80 40 100] /P 3 0 R >>",
        "<< /FT /Btn /T (delivery) /Ff 32768 /Opt [(mail) (pickup)] /V /Off /Kids [13 0 R 14 0 R] >>",
        "<< /Type /Annot /Subtype /Widget /Parent 12 0 R /P 3 0 R /Rect [60 80 80 100] >>",
        "<< /Type /Annot /Subtype /Widget /Parent 12 0 R /P 3 0 R /Rect [100 80 120 100] >>",
        "<< /Type /Annot /Subtype /Widget /FT /Ch /T (country) /Ff 131072 /Opt [[(us) (United States)] [(ca) (Canada)]] /V (us) /Rect [20 40 150 65] /P 3 0 R >>",
        "<< /Type /Annot /Subtype /Widget /FT /Ch /T (regions) /Ff 2097152 /Opt [[(nsw) (New South Wales)] [(vic) (Victoria)]] /V [(nsw)] /Rect [170 35 300 70] /P 3 0 R >>"
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
    return Encoding.ASCII.GetBytes(builder.ToString());
}
