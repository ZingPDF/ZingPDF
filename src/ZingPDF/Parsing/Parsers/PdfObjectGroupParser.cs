using MorseCode.ITask;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers;

internal class PdfObjectGroupParser : IParser<PdfObjectGroup>
{
    private readonly IParserResolver _parserRegistry;
    private readonly ITokenTypeIdentifier _tokenTypeIdentifier;
    private readonly IParser<Name> _nameParser;
    private readonly IParser<Number> _numberParser;
    private readonly IParser<Keyword> _keywordParser;
    private readonly IParser<ArrayObject> _arrayParser;
    private readonly IParser<Dictionary> _dictionaryParser;
    private readonly IParser<IndirectObjectReference> _indirectObjectReferenceParser;
    private readonly IParser<PdfString> _pdfStringParser;
    private readonly IParser<BooleanObject> _booleanObjectParser;
    private readonly IParser<Date> _dateParser;

    public PdfObjectGroupParser(
        IParserResolver parserRegistry,
        ITokenTypeIdentifier tokenTypeIdentifier,
        IParser<Name> nameParser,
        IParser<Number> numberParser,
        IParser<Keyword> keywordParser,
        IParser<ArrayObject> arrayParser,
        IParser<Dictionary> dictionaryParser,
        IParser<IndirectObjectReference> indirectObjectReferenceParser,
        IParser<PdfString> pdfStringParser,
        IParser<BooleanObject> booleanObjectParser,
        IParser<Date> dateParser)
    {
        _parserRegistry = parserRegistry;
        _tokenTypeIdentifier = tokenTypeIdentifier;
        _nameParser = nameParser;
        _numberParser = numberParser;
        _keywordParser = keywordParser;
        _arrayParser = arrayParser;
        _dictionaryParser = dictionaryParser;
        _indirectObjectReferenceParser = indirectObjectReferenceParser;
        _pdfStringParser = pdfStringParser;
        _booleanObjectParser = booleanObjectParser;
        _dateParser = dateParser;
    }

    public PdfObjectGroupParser(IParserResolver parserRegistry, ITokenTypeIdentifier tokenTypeIdentifier)
        : this(
            parserRegistry,
            tokenTypeIdentifier,
            parserRegistry.GetParser<Name>(),
            parserRegistry.GetParser<Number>(),
            parserRegistry.GetParser<Keyword>(),
            parserRegistry.GetParser<ArrayObject>(),
            parserRegistry.GetParser<Dictionary>(),
            parserRegistry.GetParser<IndirectObjectReference>(),
            parserRegistry.GetParser<PdfString>(),
            parserRegistry.GetParser<BooleanObject>(),
            parserRegistry.GetParser<Date>())
    {
    }

    public async ITask<PdfObjectGroup> ParseAsync(Stream stream, ObjectContext context)
    {
        var items = new List<IPdfObject>(8);

        while (stream.Position < stream.Length)
        {
            var type = await _tokenTypeIdentifier.TryIdentifyAsync(stream);

            if (type != null)
            {
                try
                {
                    items.Add(await ParseKnownObjectAsync(type, stream, context));
                }
                catch (PdfAuthenticationException)
                {
                    throw;
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

    private async Task<IPdfObject> ParseKnownObjectAsync(Type type, Stream stream, ObjectContext context)
    {
        if (type == typeof(Name))
        {
            return await _nameParser.ParseAsync(stream, context);
        }

        if (type == typeof(Number))
        {
            return await _numberParser.ParseAsync(stream, context);
        }

        if (type == typeof(Keyword))
        {
            return await _keywordParser.ParseAsync(stream, context);
        }

        if (type == typeof(ArrayObject))
        {
            return await _arrayParser.ParseAsync(stream, context);
        }

        if (type == typeof(Dictionary))
        {
            return await _dictionaryParser.ParseAsync(stream, context);
        }

        if (type == typeof(IndirectObjectReference))
        {
            return await _indirectObjectReferenceParser.ParseAsync(stream, context);
        }

        if (type == typeof(PdfString))
        {
            return await _pdfStringParser.ParseAsync(stream, context);
        }

        if (type == typeof(BooleanObject))
        {
            return await _booleanObjectParser.ParseAsync(stream, context);
        }

        if (type == typeof(Date))
        {
            return await _dateParser.ParseAsync(stream, context);
        }

        return await _parserRegistry.GetParserFor(type).ParseAsync(stream, context);
    }
}
