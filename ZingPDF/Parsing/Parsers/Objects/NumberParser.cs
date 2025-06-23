using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects;

internal class NumberParser : IParser<Number>
{
    public async ITask<Number> ParseAsync(Stream stream, ParseContext context)
    {
        stream.AdvancePastWhitepace();

        var content = await stream.ReadUpToExcludingAsync([.. Constants.Delimiters, .. Constants.WhitespaceCharacters]);

        content = content.TrimStart();

        var value = double.Parse(content);

        return new Number(value, context.Origin);
    }
}
