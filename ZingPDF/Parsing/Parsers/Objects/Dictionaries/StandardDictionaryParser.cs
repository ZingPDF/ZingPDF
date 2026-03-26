using Microsoft.Extensions.DependencyInjection;
using MorseCode.ITask;
using ZingPDF.DocumentInterchange.Metadata;
using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.Graphics.Images;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Linearization;
using ZingPDF.Logging;
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

    public StandardDictionaryParser(
        IPdf pdf,
        IParserResolver parserResolver,
        IDictionaryIdentifier dictionaryIdentifier
        )
    {
        _pdf = pdf;
        _parserResolver = parserResolver;
        _dictionaryIdentifier = dictionaryIdentifier;
    }

    public async ITask<Dictionary> ParseAsync(Stream stream, ObjectContext context)
    {
        var initialStreamPosition = stream.Position;
        SubStream dictStream = await ExtractDictionarySegmentAsync(stream);

        var objectGroup = await _parserResolver.GetParser<PdfObjectGroup>().ParseAsync(dictStream, context);

        if (objectGroup.Objects.Count % 2 != 0)
        {
            throw new InvalidOperationException("Odd count of objects parsed from dictionary.");
        }

        Dictionary<string, IPdfObject> dict = [];

        for (int j = 0; j < objectGroup.Objects.Count; j += 2)
        {
            var key = (Name)objectGroup.Objects[j];
            var val = objectGroup.Objects[j + 1];

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
        stream.Position = dictStream.To + 2;

        output!.ByteOffset = initialStreamPosition;

        Logger.Log(LogLevel.Trace, $"Parsed Dictionary from {stream.GetType().Name} between offsets: {initialStreamPosition} - {stream.Position}");

        return output;
    }
}
