using MorseCode.ITask;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers;

internal class ContentStreamParser : IObjectParser<ContentStream>
{
    private static readonly HashSet<string> _operatorSet = [.. ContentStream.Operators.All];
    private readonly IPdfContext _pdfContext;

    public ContentStreamParser(IPdfContext pdfContext)
    {
        ArgumentNullException.ThrowIfNull(pdfContext, nameof(pdfContext));

        _pdfContext = pdfContext;
    }

    public async ITask<ContentStream> ParseAsync(Stream stream, ParseContext context)
    {
        var group = await new PdfObjectGroupParser(_pdfContext).ParseAsync(stream, ParseContext.WithOrigin(ObjectOrigin.ParsedContentStream));

        List<ContentStreamOperation> instructions = [];
        List<IPdfObject> operands = [];

        for (var i = 0; i < group.Objects.Count; i++)
        {
            var item = group.Objects[i];
            
            if (item is Keyword k && _operatorSet.Contains(k.Value))
            {
                instructions.Add(new ContentStreamOperation { Operator = k.Value, Operands = operands.Count != 0 ? [..operands] : null });
                operands.Clear();
            }
            else
            {
                operands.Add(item);
            }
        }

        return new ContentStream(instructions, context.Origin);
    }
}
