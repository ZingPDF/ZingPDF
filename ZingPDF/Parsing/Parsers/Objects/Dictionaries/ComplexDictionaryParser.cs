using MorseCode.ITask;
using ZingPDF.DocumentInterchange.Metadata;
using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.Graphics.Images;
using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Linearization;
using ZingPDF.Logging;
using ZingPDF.Syntax;
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
using ZingPDF.Text.SimpleFonts;

namespace ZingPDF.Parsing.Parsers.Objects.Dictionaries;

internal class ComplexDictionaryParser : DictionaryParser, IObjectParser<Dictionary>
{
    // Rectangles are parsed as ArrayObjects. We'll identify them by their keys.
    private readonly List<Name> _rectKeys =
    [
        Constants.DictionaryKeys.PageTree.MediaBox,
        Constants.DictionaryKeys.PageTree.CropBox,
        Constants.DictionaryKeys.PageTree.Page.BleedBox,
        Constants.DictionaryKeys.PageTree.Page.TrimBox,
        Constants.DictionaryKeys.PageTree.Page.ArtBox,
        Constants.DictionaryKeys.Annotation.Rect,
        Constants.DictionaryKeys.Form.Type1.BBox,
    ];

    private readonly IPdfEditor _pdfEditor;

    public ComplexDictionaryParser(IPdfEditor pdfEditor)
    {
        _pdfEditor = pdfEditor;
    }

    public async ITask<Dictionary> ParseAsync(Stream stream)
    {
        //Logger.Log(LogLevel.Trace, $"Parsing Dictionary from {stream.GetType().Name} at offset: {stream.Position}.");

        // A dictionary is a key-value collection, where the key is always a 'Name' object
        // and the value can be any type of PDF object

        // << /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>

        var initialStreamPosition = stream.Position; // Reference starting point for output

        SubStream dictStream = await ExtractDictionarySegmentAsync(stream);

        var objectGroup = await new PdfObjectGroupParser(_pdfEditor).ParseAsync(dictStream);

        if (objectGroup.Objects.Count % 2 != 0)
        {
            throw new InvalidOperationException("Odd count of objects parsed from dictionary.");
        }

        Dictionary<Name, IPdfObject> dict = [];

        for (int j = 0; j < objectGroup.Objects.Count; j += 2)
        {
            var key = (Name)objectGroup.Objects[j];
            var val = objectGroup.Objects[j + 1];

            if (_rectKeys.Contains(key) && val is not IndirectObjectReference)
            {
                var ary = (ArrayObject)val;

                val = new Rectangle(
                    new Coordinate((Number)ary[0], (Number)ary[1]),
                    new Coordinate((Number)ary[2], (Number)ary[3])
                    );
            }

            dict.Add(key, val);
        }

        Dictionary? output = null;

        var dictType = await DictionaryIdentifier.IdentifyAsync(dict, _pdfEditor);

        if (dictType is null)
        {
            output = new Dictionary(dict, _pdfEditor);
            goto DictionaryParsed;
        }

        switch (dictType)
        {
            case Type t when t == typeof(DocumentCatalogDictionary):
                output = DocumentCatalogDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(PageDictionary):
                output = PageDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(PageTreeNodeDictionary):
                output = PageTreeNodeDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(CrossReferenceStreamDictionary):
                output = CrossReferenceStreamDictionary.FromDictionary(dict);
                goto DictionaryParsed;

            case Type t when t == typeof(ObjectStreamDictionary):
                output = ObjectStreamDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(AnnotationDictionary):
                output = AnnotationDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(MetadataStreamDictionary):
                output = MetadataStreamDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(WidgetAnnotationDictionary):
                output = WidgetAnnotationDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(FieldDictionary):
                output = FieldDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(Type1FormDictionary):
                output = Type1FormDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(ImageDictionary):
                output = ImageDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(Type1FontDictionary):
                output = Type1FontDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(TrueTypeFontDictionary):
                output = TrueTypeFontDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(Type3FontDictionary):
                output = Type3FontDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(Type0FontDictionary):
                output = Type0FontDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(FontDescriptorDictionary):
                output = FontDescriptorDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(InteractiveFormDictionary):
                output = InteractiveFormDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(LinearizationParameterDictionary):
                output = InteractiveFormDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(StreamDictionary):
                output = StreamDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;

            case Type t when t == typeof(AppearanceDictionary):
                output = AppearanceDictionary.FromDictionary(dict, _pdfEditor);
                goto DictionaryParsed;
        }

        output ??= new Dictionary(dict, _pdfEditor); 

    DictionaryParsed:
        stream.Position = dictStream.To + 2;

        output!.ByteOffset = initialStreamPosition;

        Logger.Log(LogLevel.Trace, $"Parsed Dictionary from {stream.GetType().Name} between offsets: {initialStreamPosition} - {stream.Position}");

        return output;
    }
}
