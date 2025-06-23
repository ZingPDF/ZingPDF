using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class BooleanObjectParser : IParser<BooleanObject>
    {
        public async ITask<BooleanObject> ParseAsync(Stream stream, ParseContext context)
        {
            stream.AdvancePastWhitepace();

            var parsed = bool.Parse(await stream.ReadUpToExcludingAsync([..Constants.Delimiters, ..Constants.WhitespaceCharacters]));
            
            return BooleanObject.FromBool(parsed, context.Origin);
        }
    }
}
