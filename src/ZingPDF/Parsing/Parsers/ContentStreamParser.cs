using MorseCode.ITask;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers;

internal class ContentStreamParser : IParser<ContentStream>
{
    private static readonly HashSet<string> _operatorSet = [.. ContentStream.Operators.All];

    private readonly IParserResolver _parserResolver;
    private readonly ITokenTypeIdentifier _tokenTypeIdentifier;
    private readonly IParser<Comment> _commentParser;
    private readonly IParser<Name> _nameParser;
    private readonly IParser<Number> _numberParser;
    private readonly IParser<Keyword> _keywordParser;
    private readonly IParser<ArrayObject> _arrayParser;
    private readonly IParser<Dictionary> _dictionaryParser;
    private readonly IParser<IndirectObjectReference> _indirectObjectReferenceParser;
    private readonly IParser<PdfString> _pdfStringParser;
    private readonly IParser<BooleanObject> _booleanObjectParser;
    private readonly IParser<Date> _dateParser;

    public ContentStreamParser(
        IParserResolver parserResolver,
        ITokenTypeIdentifier tokenTypeIdentifier,
        IParser<Comment> commentParser,
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
        _parserResolver = parserResolver;
        _tokenTypeIdentifier = tokenTypeIdentifier;
        _commentParser = commentParser;
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

    public ContentStreamParser(IParserResolver parserResolver, ITokenTypeIdentifier tokenTypeIdentifier)
        : this(
            parserResolver,
            tokenTypeIdentifier,
            parserResolver.GetParser<Comment>(),
            parserResolver.GetParser<Name>(),
            parserResolver.GetParser<Number>(),
            parserResolver.GetParser<Keyword>(),
            parserResolver.GetParser<ArrayObject>(),
            parserResolver.GetParser<Dictionary>(),
            parserResolver.GetParser<IndirectObjectReference>(),
            parserResolver.GetParser<PdfString>(),
            parserResolver.GetParser<BooleanObject>(),
            parserResolver.GetParser<Date>())
    {
    }

    public async ITask<ContentStream> ParseAsync(Stream stream, ObjectContext context)
    {
        List<ContentStreamOperation> instructions = [];
        List<IPdfObject> operands = [];
        var itemContext = ObjectContext.WithOrigin(ObjectOrigin.ParsedContentStream);

        while (stream.Position < stream.Length)
        {
            var type = await _tokenTypeIdentifier.TryIdentifyAsync(stream);

            if (type == null)
            {
                stream.Position += 1;
                continue;
            }

            try
            {
                if (type == typeof(Comment))
                {
                    _ = await _commentParser.ParseAsync(stream, itemContext);
                    continue;
                }

                var item = await ParseKnownObjectAsync(type, stream, itemContext);

                if (item is Keyword keyword && _operatorSet.Contains(keyword.Value))
                {
                    instructions.Add(new ContentStreamOperation
                    {
                        Operator = keyword.Value,
                        Operands = operands.Count != 0 ? [.. operands] : null
                    });

                    operands.Clear();
                    continue;
                }

                operands.Add(item);
            }
            catch (PdfAuthenticationException)
            {
                throw;
            }
            catch
            {
                // Content streams can contain malformed or unsupported sections.
                // Return the successfully parsed prefix instead of failing the whole stream.
                break;
            }
        }

        return new ContentStream(instructions, context);
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

        return await _parserResolver.GetParserFor(type).ParseAsync(stream, context);
    }
}
