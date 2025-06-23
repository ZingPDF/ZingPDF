using Microsoft.Extensions.DependencyInjection;
using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers.Objects;

internal class HexadecimalStringParser : IParser<HexadecimalString>
{
    public async ITask<HexadecimalString> ParseAsync(Stream stream, ParseContext context)
    {
        await stream.AdvanceBeyondNextAsync(Constants.Characters.LessThan);

        var content = await stream.ReadUpToIncludingAsync(Constants.Characters.GreaterThan);

        var value = content[..^1];

        return HexadecimalString.FromHexString(value, context.Origin);
    }
}
