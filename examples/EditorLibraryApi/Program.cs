using ZingPDF;
using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Text;
using ZingPDF.Text.Editing;

if (args.Length != 2) throw new ArgumentException("Usage: EditorLibraryApi <font.ttf> <output.pdf>");
using var pdf = Pdf.Create();
var font = await pdf.RegisterUnicodeTrueTypeFontAsync(args[0]);
var page = await pdf.GetPageAsync(1);
await page.AddTextAsync("First \u03A9\nSecond \u0416", new Coordinate(40, 700), font, 14, RGBColour.Black,
    new TextPlacementOptions { LineHeight = 18 });
var another = await pdf.AppendPageAsync(o => o.MediaBox = Rectangle.FromDimensions(595, 842));
await another.AddTextAsync("Another page", new Coordinate(40, 700), font, 14, RGBColour.Black);
await pdf.MovePageAsync(1, 2);
var editor = pdf.CreateTextEditor();
var line = (await editor.GetLinesAsync(2)).First();
var options = new PdfTextReplacementOptions { Font = font };
var check = await editor.CheckReplacementAsync(line, "Revised \u03A9", options);
if (!check.CanApply) throw new InvalidOperationException(check.Message);
await editor.ReplaceLineAsync(line, "Revised \u03A9", options);
using (var output = File.Create(args[1])) await pdf.SaveAsync(output);
using var input = File.OpenRead(args[1]);
using var reopened = Pdf.Load(input);
if (!(await reopened.CreateTextEditor().GetLinesAsync(2)).Any(l => l.Text == "Revised \u03A9"))
    throw new InvalidOperationException("Saved text did not round-trip.");
Console.WriteLine("Unicode authoring, page move, registered-font replacement and save/reopen passed.");
