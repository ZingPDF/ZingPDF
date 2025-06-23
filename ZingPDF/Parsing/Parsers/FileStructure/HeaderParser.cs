using MorseCode.ITask;
using System.Text;
using ZingPDF.Extensions;
using ZingPDF.Syntax.FileStructure;

namespace ZingPDF.Parsing.Parsers.FileStructure;

internal class HeaderParser : IParser<Header>
{
    private readonly IPdfObjectCollection _pdfObjects;

    public HeaderParser(IPdfObjectCollection pdfObjects)
    {
        _pdfObjects = pdfObjects;
    }

    public async ITask<Header> ParseAsync(Stream stream, ParseContext context)
    {
        await stream.AdvanceBeyondNextAsync("%PDF-");

        var version = Encoding.ASCII.GetString(
        [
            (byte)stream.ReadByte(),
            (byte)stream.ReadByte(),
            (byte)stream.ReadByte(),
        ]);

        return new Header(double.Parse(version), context.Origin);
    }
}
