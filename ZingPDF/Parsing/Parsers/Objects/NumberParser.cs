using MorseCode.ITask;
using System.Globalization;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects;

internal class NumberParser : IParser<Number>
{
    public async ITask<Number> ParseAsync(Stream stream, ObjectContext context)
    {
        await Task.CompletedTask;
        var value = double.Parse(ParserTokenReader.ReadNumber(stream), CultureInfo.InvariantCulture);
        return new Number(value, context);
    }
}
