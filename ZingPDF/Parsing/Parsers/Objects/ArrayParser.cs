using MorseCode.ITask;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers.Objects;

internal class ArrayParser : IParser<ArrayObject>
{
    private readonly IParserResolver _parserResolver;
    private readonly ITokenTypeIdentifier _tokenTypeIdentifier = new TokenTypeIdentifier();

    public ArrayParser(IParserResolver parserResolver)
    {
        _parserResolver = parserResolver;
    }

    public async ITask<ArrayObject> ParseAsync(Stream stream, ObjectContext context)
    {
        await AdvanceBeyondArrayStartAsync(stream);

        var items = new List<IPdfObject>(8);

        while (true)
        {
            await AdvancePastTriviaAsync(stream);

            if (TryConsumeArrayEnd(stream))
            {
                return new ArrayObject(items, context);
            }

            var type = await _tokenTypeIdentifier.TryIdentifyAsync(stream)
                ?? throw new ParserException("Unable to identify array item.");

            if (type == typeof(Comment))
            {
                _ = await _parserResolver.GetParser<Comment>().ParseAsync(stream, context);
                continue;
            }

            items.Add(await ParseValueAsync(type, stream, context));
        }
    }

    private async Task<IPdfObject> ParseValueAsync(Type type, Stream stream, ObjectContext context)
    {
        if (type == typeof(Name))
        {
            return await _parserResolver.GetParser<Name>().ParseAsync(stream, context);
        }

        if (type == typeof(Number))
        {
            return await _parserResolver.GetParser<Number>().ParseAsync(stream, context);
        }

        if (type == typeof(Keyword))
        {
            return await _parserResolver.GetParser<Keyword>().ParseAsync(stream, context);
        }

        if (type == typeof(ArrayObject))
        {
            return await ParseAsync(stream, context);
        }

        if (type == typeof(Dictionary))
        {
            return await _parserResolver.GetParser<Dictionary>().ParseAsync(stream, context);
        }

        if (type == typeof(IndirectObjectReference))
        {
            return await _parserResolver.GetParser<IndirectObjectReference>().ParseAsync(stream, context);
        }

        if (type == typeof(PdfString))
        {
            return await _parserResolver.GetParser<PdfString>().ParseAsync(stream, context);
        }

        if (type == typeof(BooleanObject))
        {
            return await _parserResolver.GetParser<BooleanObject>().ParseAsync(stream, context);
        }

        if (type == typeof(Date))
        {
            return await _parserResolver.GetParser<Date>().ParseAsync(stream, context);
        }

        return await _parserResolver.GetParserFor(type).ParseAsync(stream, context);
    }

    private static async Task AdvanceBeyondArrayStartAsync(Stream stream)
    {
        await AdvancePastTriviaAsync(stream);

        if (stream.ReadByte() != Constants.Characters.LeftSquareBracket)
        {
            throw new ParserException("Expected array start marker '['.");
        }
    }

    private static bool TryConsumeArrayEnd(Stream stream)
    {
        if (!stream.CanSeek || stream.Position >= stream.Length)
        {
            return false;
        }

        var originalPosition = stream.Position;
        var next = stream.ReadByte();

        if (next == Constants.Characters.RightSquareBracket)
        {
            return true;
        }

        stream.Position = originalPosition;
        return false;
    }

    private static async Task AdvancePastTriviaAsync(Stream stream)
    {
        while (stream.Position < stream.Length)
        {
            var next = stream.ReadByte();
            if (next < 0)
            {
                return;
            }

            if (char.IsWhiteSpace((char)next))
            {
                continue;
            }

            if (next == Constants.Characters.Percent)
            {
                while (stream.Position < stream.Length)
                {
                    var commentChar = stream.ReadByte();
                    if (commentChar is '\r' or '\n' or < 0)
                    {
                        break;
                    }
                }

                continue;
            }

            stream.Position--;
            await Task.CompletedTask;
            return;
        }

        await Task.CompletedTask;
    }
}
