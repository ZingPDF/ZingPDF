using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Encryption;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers.Objects;

internal class HexadecimalStringParser : IParser<HexadecimalString>
{
    private readonly IPdfEncryptionProvider _encryptionProvider;

    public HexadecimalStringParser(IPdfEncryptionProvider encryptionProvider)
    {
        _encryptionProvider = encryptionProvider;
    }

    public async ITask<HexadecimalString> ParseAsync(Stream stream, ObjectContext context)
    {
        await stream.AdvanceBeyondNextAsync(Constants.Characters.LessThan);

        var content = await stream.ReadUpToIncludingAsync(Constants.Characters.GreaterThan);

        var value = content[..^1];

        var bytes = Convert.FromHexString(value);

        bytes = await _encryptionProvider.DecryptObjectBytesAsync(context, bytes, null);

        return HexadecimalString.FromBytes(bytes, context);
    }
}
