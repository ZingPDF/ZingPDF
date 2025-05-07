using MorseCode.ITask;
using System.Text;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects;

internal class NameParser : IObjectParser<Name>
{
    private readonly IPdfContext _pdfContext;

    readonly char[] _nameDelimiters = [.. Constants.Delimiters, .. Constants.WhitespaceCharacters];
    
    public NameParser(IPdfContext pdfContext)
    {
        _pdfContext = pdfContext;
    }

    public async ITask<Name> ParseAsync(Stream stream, ParseContext context)
    {
        //Logger.Log(LogLevel.Trace, $"Parsing Name from {stream.GetType().Name} at offset: {stream.Position}.");

        await stream.AdvanceBeyondNextAsync(Constants.Characters.Solidus);

        var nameStart = stream.Position;

        // Since a delimiter is likely to be found relatively early within the data,
        // it will be more efficient to find the first delimiter, and then only convert the necessary
        // data to a string.

        var bufferSize = 4096;
        var buffer = new byte[bufferSize];

        StringBuilder content = new();

        do
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, bufferSize));
            if (read == 0)
            {
                break;
            }

            for (var i = 0; i < read; i++)
            {
                if (_nameDelimiters.Contains((char)buffer[i]))
                {
                    content.Append(Encoding.ASCII.GetString(buffer, 0, i));

                    stream.Position = nameStart + i;

                    return new Name(content.ToString().ReplaceHexCodes(), context.Origin);
                }
            }

            content.Append(Encoding.ASCII.GetString(buffer, 0, read));
        }
        while (stream.Position < stream.Length);

        var value = content.ToString().ReplaceHexCodes();

        Logger.Log(LogLevel.Trace, $"Parsed Name: {{{value}}}. {stream.GetType().Name} now at: {stream.Position}.");

        return new Name(value, context.Origin);
    }
}
