using MorseCode.ITask;
using ZingPDF.Syntax;

namespace ZingPDF.Parsing.Parsers;

internal class PdfObjectGroupParser : IParser<PdfObjectGroup>
{
    private readonly IParserResolver _parserRegistry;
    private readonly ITokenTypeIdentifier _tokenTypeIdentifier;

    public PdfObjectGroupParser(IParserResolver parserRegistry, ITokenTypeIdentifier tokenTypeIdentifier)
    {
        _parserRegistry = parserRegistry;
        _tokenTypeIdentifier = tokenTypeIdentifier;
    }

    public async ITask<PdfObjectGroup> ParseAsync(Stream stream, ObjectContext context)
    {
        var items = new List<IPdfObject>();

        while (stream.Position < stream.Length)
        {
            var type = await _tokenTypeIdentifier.TryIdentifyAsync(stream);

            if (type != null)
            {
                try
                {
                    items.Add(await _parserRegistry.GetParserFor(type).ParseAsync(stream, context));
                }
                catch
                {
                    // If any exception is thrown, gracefully exit.
                    // The sub-object could be invalid or not understood by this library.
                    // There are also scenarios where we don't have complete data, but want to parse what we can anyway,
                    // such as reading a fixed size chunk from the beginning of the file to find the linearization dictionary.
                    break;
                }
            }
            else
            {
                stream.Position += 1;
            }
        }

        return new PdfObjectGroup(items, context);
    }
}