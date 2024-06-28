using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
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
