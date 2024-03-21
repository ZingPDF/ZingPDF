using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Objects.Primitives;

namespace ZingPDF.Parsing.PrimitiveParsers
{
    internal class RealNumberParser : IPdfObjectParser<RealNumber>
    {
        public async ITask<RealNumber> ParseAsync(Stream stream)
        {
            stream.AdvancePastWhitepace();

            var content = await stream.ReadUpToExcludingAsync(Constants.WhitespaceCharacters);

            return double.Parse(content);
        }
    }
}
