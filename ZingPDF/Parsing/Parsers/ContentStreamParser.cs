using MorseCode.ITask;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers;

internal class ContentStreamParser : IParser<ContentStream>
{
    private static readonly HashSet<string> _operatorSet = [.. ContentStream.Operators.All];

    private readonly IParser<PdfObjectGroup> _objectGroupParser;

    public ContentStreamParser(IParser<PdfObjectGroup> objectGroupParser)
    {
        _objectGroupParser = objectGroupParser;
    }

    public async ITask<ContentStream> ParseAsync(Stream stream, ObjectContext context)
    {
        var group = await _objectGroupParser.ParseAsync(stream, ObjectContext.WithOrigin(ObjectOrigin.ParsedContentStream));

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

        return new ContentStream(instructions, context);
    }
}
