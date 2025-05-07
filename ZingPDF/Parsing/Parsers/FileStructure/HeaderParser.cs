using MorseCode.ITask;
using System.Text;
using ZingPDF.Extensions;
using ZingPDF.Syntax.FileStructure;

namespace ZingPDF.Parsing.Parsers.FileStructure;

internal class HeaderParser : IObjectParser<Header>
{
    private readonly IPdfContext _pdfContext;

    public HeaderParser(IPdfContext pdfContext)
    {
        _pdfContext = pdfContext;
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
