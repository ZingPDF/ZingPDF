using Microsoft.Extensions.DependencyInjection;
using MorseCode.ITask;
using ZingPDF.Diagnostics;
using ZingPDF.DocumentInterchange.Metadata;
using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.Graphics.Images;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Linearization;
using ZingPDF.Logging;
using ZingPDF.Parsing;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Encryption;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.ObjectStreams;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text;
using ZingPDF.Text.CompositeFonts;
using ZingPDF.Text.Encoding;
using ZingPDF.Text.SimpleFonts;

namespace ZingPDF.Parsing.Parsers.Objects.Dictionaries;

internal class StandardDictionaryParser : DictionaryParser, IParser<Dictionary>
{
    // Rectangles are parsed as ArrayObjects. We'll identify them by their keys.
    private static readonly HashSet<string> _rectKeys =
    [
        Constants.DictionaryKeys.PageTree.MediaBox,
        Constants.DictionaryKeys.PageTree.CropBox,
        Constants.DictionaryKeys.PageTree.Page.BleedBox,
        Constants.DictionaryKeys.PageTree.Page.TrimBox,
        Constants.DictionaryKeys.PageTree.Page.ArtBox,
        Constants.DictionaryKeys.Annotation.Rect,
        Constants.DictionaryKeys.Form.Type1.BBox,
    ];

    private readonly IPdf _pdf;
    private readonly IParserResolver _parserResolver;
    private readonly IDictionaryIdentifier _dictionaryIdentifier;
    private readonly ITokenTypeIdentifier _tokenTypeIdentifier = new TokenTypeIdentifier();
    private readonly IParser<Name> _nameParser;
    private readonly IParser<Number> _numberParser;
    private readonly IParser<Keyword> _keywordParser;
    private readonly IParser<ArrayObject> _arrayParser;
    private readonly IParser<IndirectObjectReference> _indirectObjectReferenceParser;
    private readonly IParser<PdfString> _pdfStringParser;
    private readonly IParser<BooleanObject> _booleanObjectParser;
    private readonly IParser<Date> _dateParser;

    public StandardDictionaryParser(
        IPdf pdf,
        IParserResolver parserResolver,
        IDictionaryIdentifier dictionaryIdentifier
        )
    {
        _pdf = pdf;
        _parserResolver = parserResolver;
        _dictionaryIdentifier = dictionaryIdentifier;
        _nameParser = parserResolver.GetParser<Name>();
        _numberParser = parserResolver.GetParser<Number>();
        _keywordParser = parserResolver.GetParser<Keyword>();
        _arrayParser = parserResolver.GetParser<ArrayObject>();
        _indirectObjectReferenceParser = parserResolver.GetParser<IndirectObjectReference>();
        _pdfStringParser = parserResolver.GetParser<PdfString>();
        _booleanObjectParser = parserResolver.GetParser<BooleanObject>();
        _dateParser = parserResolver.GetParser<Date>();
    }

    public async ITask<Dictionary> ParseAsync(Stream stream, ObjectContext context)
    {
        using var trace = PerformanceTrace.Measure("StandardDictionaryParser.ParseAsync");
        var initialStreamPosition = stream.Position;
        Dictionary<string, IPdfObject> dict = [];

        await AdvanceBeyondDictionaryStartAsync(stream);

        while (true)
        {
            await AdvancePastTriviaAsync(stream);

            if (await TryConsumeDictionaryEndAsync(stream))
            {
                break;
            }

            var key = await _nameParser.ParseAsync(stream, context);

            await AdvancePastTriviaAsync(stream);

            var val = await ParseValueAsync(stream, context);

            if (_rectKeys.Contains(key) && val is not IndirectObjectReference)
            {
                var ary = (ArrayObject)val;

                val = Rectangle.FromCoordinates(
                    new Coordinate((Number)ary[0], (Number)ary[1]),
                    new Coordinate((Number)ary[2], (Number)ary[3])
                    );
            }

            dict.Add(key, val);
        }

        Dictionary? output = null;

        var dictType = await _dictionaryIdentifier.IdentifyAsync(dict);

        if (dictType is null)
        {
            output = new Dictionary(dict, _pdf, context);
            goto DictionaryParsed;
        }

        switch (dictType)
        {
            case Type t when t == typeof(DocumentCatalogDictionary):
                output = DocumentCatalogDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(PageDictionary):
                output = PageDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(PageTreeNodeDictionary):
                output = PageTreeNodeDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(CrossReferenceStreamDictionary):
                output = CrossReferenceStreamDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(ObjectStreamDictionary):
                output = ObjectStreamDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(AnnotationDictionary):
                output = AnnotationDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(MetadataStreamDictionary):
                output = MetadataStreamDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(WidgetAnnotationDictionary):
                output = WidgetAnnotationDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(FieldDictionary):
                output = FieldDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(Type1FormDictionary):
                output = Type1FormDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(ImageDictionary):
                output = ImageDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(Type1FontDictionary):
                output = Type1FontDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(TrueTypeFontDictionary):
                output = TrueTypeFontDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(Type3FontDictionary):
                output = Type3FontDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(Type0FontDictionary):
                output = Type0FontDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(FontDescriptorDictionary):
                output = FontDescriptorDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(InteractiveFormDictionary):
                output = InteractiveFormDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(LinearizationParameterDictionary):
                output = LinearizationParameterDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(StandardEncryptionDictionary):
                output = StandardEncryptionDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(StreamDictionary):
                output = StreamDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(AppearanceDictionary):
                output = AppearanceDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(EncodingDictionary):
                output = EncodingDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(CIDFontDictionary):
                output = CIDFontDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;

            case Type t when t == typeof(CIDSystemInfoDictionary):
                output = CIDSystemInfoDictionary.FromDictionary(dict, _pdf, context);
                goto DictionaryParsed;
        }

        output ??= new Dictionary(dict, _pdf, context);

    DictionaryParsed:
        output!.ByteOffset = initialStreamPosition;

        Logger.Log(LogLevel.Trace, $"Parsed Dictionary from {stream.GetType().Name} between offsets: {initialStreamPosition} - {stream.Position}");

        return output;
    }

    private async Task<IPdfObject> ParseValueAsync(Stream stream, ObjectContext context)
    {
        var type = await _tokenTypeIdentifier.TryIdentifyAsync(stream)
            ?? throw new ParserException("Unable to identify dictionary value.");

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
            return await ParseAsync(stream, context);
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

    private static async Task AdvanceBeyondDictionaryStartAsync(Stream stream)
    {
        await AdvancePastTriviaAsync(stream);

        if (stream.ReadByte() != Constants.Characters.LessThan || stream.ReadByte() != Constants.Characters.LessThan)
        {
            throw new ParserException("Expected dictionary start marker '<<'.");
        }
    }

    private static async Task<bool> TryConsumeDictionaryEndAsync(Stream stream)
    {
        if (!stream.CanSeek)
        {
            return false;
        }

        long originalPosition = stream.Position;
        int first = stream.ReadByte();
        int second = stream.ReadByte();

        if (first == Constants.Characters.GreaterThan && second == Constants.Characters.GreaterThan)
        {
            return true;
        }

        stream.Position = originalPosition;
        await Task.CompletedTask;
        return false;
    }

    private static async Task AdvancePastTriviaAsync(Stream stream)
    {
        while (stream.Position < stream.Length)
        {
            int next = stream.ReadByte();
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
                    int commentChar = stream.ReadByte();
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
