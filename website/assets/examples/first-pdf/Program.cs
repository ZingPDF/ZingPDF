using ZingPDF;

Directory.CreateDirectory("output");
var inputPath = Path.Combine("output", "original.pdf");
var editedPath = Path.Combine("output", "edited.pdf");

// Create the input document. Coordinates are in PDF points.
await Pdf.New()
    .Page(page => page
        .Size(595, 842)
        .Text(text => text
            .Value("Delivery report")
            .HelveticaBold()
            .FontSize(24)
            .At(48, 780)))
    .SaveToFileAsync(inputPath);

// Keep the seekable input stream open until the loaded PDF is disposed.
using (var input = File.OpenRead(inputPath))
using (var pdf = Pdf.Load(input))
{
    await pdf.Pages(pages => pages
            .Page(1, page => page
                .Text(text => text
                    .Value("Approved for delivery")
                    .Helvetica()
                    .FontSize(14)
                    .At(48, 740)))
            .Append(page => page
                .Size(595, 842)
                .Text(text => text
                    .Value("Delivery checklist")
                    .HelveticaBold()
                    .FontSize(24)
                    .At(48, 780))))
        .SaveToFileAsync(editedPath);
}

// Reopen the saved file and check its page tree and embedded text.
using var savedInput = File.OpenRead(editedPath);
using var saved = Pdf.Load(savedInput);
var pageCount = await saved.GetPageCountAsync();
var extractedText = string.Join(Environment.NewLine, (await saved.ExtractTextAsync()).Select(item => item.Text));
if (pageCount != 2 || !extractedText.Contains("Delivery report") ||
    !extractedText.Contains("Approved for delivery") || !extractedText.Contains("Delivery checklist"))
    throw new InvalidDataException("The saved PDF did not contain the expected pages and text.");

await File.WriteAllTextAsync(Path.Combine("output", "extracted-text.txt"), extractedText);
Console.WriteLine("Verified: 2 pages; original heading, approval note, and appended-page heading found.");
Console.WriteLine($"PDF: {Path.GetFullPath(editedPath)}");
