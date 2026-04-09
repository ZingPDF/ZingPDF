using MorseCode.ITask;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers;

internal sealed class StrictContentStreamParser : IParser<ContentStream>
{
    private static readonly HashSet<string> OperatorSet = [.. ContentStream.Operators.All];

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

    public StrictContentStreamParser(IParserResolver parserResolver, ITokenTypeIdentifier tokenTypeIdentifier)
    {
        _parserResolver = parserResolver;
        _tokenTypeIdentifier = tokenTypeIdentifier;
        _commentParser = parserResolver.GetParser<Comment>();
        _nameParser = parserResolver.GetParser<Name>();
        _numberParser = parserResolver.GetParser<Number>();
        _keywordParser = parserResolver.GetParser<Keyword>();
        _arrayParser = parserResolver.GetParser<ArrayObject>();
        _dictionaryParser = parserResolver.GetParser<Dictionary>();
        _indirectObjectReferenceParser = parserResolver.GetParser<IndirectObjectReference>();
        _pdfStringParser = parserResolver.GetParser<PdfString>();
        _booleanObjectParser = parserResolver.GetParser<BooleanObject>();
        _dateParser = parserResolver.GetParser<Date>();
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

            if (type == typeof(Comment))
            {
                _ = await _commentParser.ParseAsync(stream, itemContext);
                continue;
            }

            var item = await ParseKnownObjectAsync(type, stream, itemContext);

            if (item is Keyword keyword && OperatorSet.Contains(keyword.Value))
            {
                var operatorValue = keyword.Value;
                if (operands.LastOrDefault() is Keyword operatorPrefix)
                {
                    var combinedOperator = operatorPrefix.Value + operatorValue;
                    if (OperatorSet.Contains(combinedOperator))
                    {
                        operands.RemoveAt(operands.Count - 1);
                        operatorValue = combinedOperator;
                    }
                }

                instructions.Add(new ContentStreamOperation
                {
                    Operator = operatorValue,
                    Operands = operands.Count != 0 ? [.. operands] : null
                });

                operands.Clear();
                continue;
            }

            operands.Add(item);
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
