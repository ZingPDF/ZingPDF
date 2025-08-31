using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects;

internal class KeywordParser : IParser<Keyword>
{
    public async ITask<Keyword> ParseAsync(Stream stream, ObjectContext context)
    {
        stream.AdvancePastWhitepace();

        var keyword = await stream.ReadUpToExcludingAsync(Constants.WhitespaceCharacters);

        return new Keyword(keyword, context);
    }
}
