using MorseCode.ITask;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects;

internal class KeywordParser : IParser<Keyword>
{
    public async ITask<Keyword> ParseAsync(Stream stream, ObjectContext context)
    {
        await Task.CompletedTask;
        return new Keyword(ParserTokenReader.ReadKeyword(stream), context);
    }
}
