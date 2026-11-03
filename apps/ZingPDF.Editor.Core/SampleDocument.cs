using ZingPDF.Elements.Drawing;
using ZingPDF.Fonts;
using ZingPDF.Graphics;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Text;

namespace ZingPDF.Editor.Core;

/// <summary>Creates a three-page example with normal page content and two fillable text fields.</summary>
public static class SampleDocument
{
    /// <summary>Writes a new sample PDF to a writable, seekable, empty stream. The caller owns the stream.</summary>
    public static async Task WriteAsync(Stream output)
    {
        using var pdf = Pdf.Create(o => o.MediaBox = Rectangle.FromDimensions(420, 560));
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);
        var ink = new RGBColour(.15, .22, .18);
        async Task Text(int page, string text, double x, double y, int size)
            => await (await pdf.GetPageAsync(page)).AddTextAsync(new TextObject(text, new Coordinate(x, y), font, size, ink));
        await Text(1, "WILLOW HOUSE", 40, 510, 11);
        await Text(1, "Visitor details", 40, 465, 27);
        await Text(1, "Please complete your details before your visit.", 40, 425, 12);
        await Text(1, "We look forward to welcoming you.", 40, 405, 12);
        await Text(1, "Full name", 40, 350, 11);
        await Text(1, "Date of visit", 40, 275, 11);
        await Text(1, "Anything we should know?", 40, 200, 11);
        await Text(1, "You can add a note in the space below.", 40, 180, 11);
        var form = await pdf.GetOrCreateFormAsync();
        await form.AddTextFieldAsync(1, "visitor.name", Rectangle.FromCoordinates(new Coordinate(40, 310), new Coordinate(370, 340)), o => o.Description = "Full name");
        await form.AddTextFieldAsync(1, "visitor.date", Rectangle.FromCoordinates(new Coordinate(40, 235), new Coordinate(370, 265)), o => o.Description = "Date of visit");
        await pdf.AppendPageAsync(o => o.MediaBox = Rectangle.FromDimensions(420, 560));
        await Text(2, "WILLOW HOUSE", 40, 510, 11);
        await Text(2, "Your visit", 40, 465, 27);
        await Text(2, "Your room will be ready from 2 pm.", 40, 420, 12);
        await Text(2, "Please let us know if you expect to arrive late.", 40, 398, 12);
        await Text(2, "Breakfast is served from 7 am to 9 am.", 40, 345, 12);
        await Text(2, "Make yourself at home.", 40, 292, 12);
        await pdf.AppendPageAsync(o => o.MediaBox = Rectangle.FromDimensions(420, 560));
        await Text(3, "WILLOW HOUSE", 40, 510, 11);
        await Text(3, "Getting here", 40, 465, 27);
        await Text(3, "Enter through the garden gate.", 40, 420, 12);
        await Text(3, "Visitor parking is available beside the house.", 40, 398, 12);
        await Text(3, "Please call us if you need directions.", 40, 345, 12);
        await pdf.SaveAsync(output);
    }
}
