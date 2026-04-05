using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects;

internal class NameParser : IParser<Name>
{
    public async ITask<Name> ParseAsync(Stream stream, ObjectContext context)
    {
        await Task.CompletedTask;
        return new Name(ParserTokenReader.ReadName(stream).ReplaceHexCodes(), context);
    }
}
