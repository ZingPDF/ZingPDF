using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class NameParser : IPdfObjectParser<Name>
    {
        private readonly char[] _nameDelimiters = new[]
        {
            Constants.Solidus,
            Constants.Space,
            Constants.CarriageReturn,
            Constants.LineFeed,
            Constants.LessThan,
            Constants.ArrayStart
        };

        public async ITask<Name> ParseAsync(Stream stream)
        {
            await stream.AdvanceBeyondNextAsync(Constants.Solidus);

            // TODO: Do we need to account for escaped delimiters in the Name string?
            return await stream.ReadUpToExcludingAsync(_nameDelimiters);
        }
    }
}
