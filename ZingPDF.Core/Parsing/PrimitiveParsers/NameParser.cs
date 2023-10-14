using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class NameParser : IPdfObjectParser<Name>
    {
        public async ITask<Name> ParseAsync(Stream stream)
        {
            await stream.AdvanceBeyondNextAsync(Constants.Solidus);

            return await stream.ReadUpToExcludingAsync(Constants.Solidus, Constants.Space);
        }
    }
}
