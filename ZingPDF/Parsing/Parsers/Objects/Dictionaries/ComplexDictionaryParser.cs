using MorseCode.ITask;
using System.Text.Json;
using System.Text.Json.Nodes;
using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics;
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
using ZingPDF.Text.SimpleFonts;

namespace ZingPDF.Parsing.Parsers.Objects.Dictionaries;

internal class ComplexDictionaryParser(IPdfEditor pdfEditor) : DictionaryParser, IObjectParser<Dictionary>
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

    public async ITask<Dictionary> ParseAsync(Stream stream)
    {
        //Logger.Log(LogLevel.Trace, $"Parsing Dictionary from {stream.GetType().Name} at offset: {stream.Position}.");

        // A dictionary is a key-value collection, where the key is always a 'Name' object
        // and the value can be any type of PDF object

        // << /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>

        var initialStreamPosition = stream.Position; // Reference starting point for output

        SubStream dictStream = await ExtractDictionarySegmentAsync(stream);

        //if (dictStream == null)
        //{
        //    return new Dictionary(pdfEditor);
        //}

        var objectGroup = await new PdfObjectGroupParser(pdfEditor).ParseAsync(dictStream);

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

        if (dict.ContainsKey(Constants.DictionaryKeys.Type))
        {
            switch ((Name)dict[Constants.DictionaryKeys.Type])
            {
                case Constants.DictionaryTypes.Catalog:
                    output = DocumentCatalogDictionary.FromDictionary(dict, pdfEditor);
                    goto DictionaryParsed;

                case Constants.DictionaryTypes.Page:
                    output = PageDictionary.FromDictionary(dict, pdfEditor);
                    goto DictionaryParsed;

                case Constants.DictionaryTypes.Pages:
                    output = PageTreeNodeDictionary.FromDictionary(dict, pdfEditor);
                    goto DictionaryParsed;

                case Constants.DictionaryTypes.XRef:
                    output = CrossReferenceStreamDictionary.FromDictionary(dict);
                    goto DictionaryParsed;

                case Constants.DictionaryTypes.ObjStm:
                    output = ObjectStreamDictionary.FromDictionary(dict, pdfEditor);
                    goto DictionaryParsed;

                case Constants.DictionaryTypes.Annot:
                    output = await CreateAnnotationDictionaryAsync(dict, pdfEditor);
                    goto DictionaryParsed;

                case Constants.DictionaryTypes.XObject:
                    output = (string)(Name)dict[Constants.DictionaryKeys.Subtype] switch
                    {
                        XObjectDictionary.Subtypes.Form => Type1FormDictionary.FromDictionary(dict, pdfEditor), // There is only 1 type of form dictionary
                        XObjectDictionary.Subtypes.Image => ImageDictionary.FromDictionary(dict, pdfEditor),
                        _ => throw new ParserException("Unexpected XObject Subtype")
                    };
                    goto DictionaryParsed;

                case Constants.DictionaryTypes.Font:
                    output = FontDictionary.FromDictionary(dict, pdfEditor);
                    goto DictionaryParsed;

                case Constants.DictionaryTypes.FontDescriptor:
                    output = FontDescriptorDictionary.FromDictionary(dict, pdfEditor);
                    goto DictionaryParsed;
            }
        }

        if (dict.ContainsKey(Constants.DictionaryKeys.InteractiveForm.Fields))
        {
            output = InteractiveFormDictionary.FromDictionary(dict, pdfEditor);
            goto DictionaryParsed;
        }

        if (dict.ContainsKey(Constants.DictionaryKeys.LinearizationParameter.Linearized))
        {
            output = LinearizationParameterDictionary.FromDictionary(dict, pdfEditor);
            goto DictionaryParsed;
        }

        // TODO: using length to identify a stream dictionary, which seems dodgy, revisit this to make it more reliable.
        if (dict.ContainsKey(Constants.DictionaryKeys.Stream.Length))
        {
            output = StreamDictionary.FromDictionary(dict, pdfEditor);
            goto DictionaryParsed;
        }

        if (dict.ContainsKey(Constants.DictionaryKeys.Appearance.N))
        {
            output = AppearanceDictionary.FromDictionary(dict, pdfEditor);
            goto DictionaryParsed;
        }

        output ??= new Dictionary(dict, pdfEditor);

    DictionaryParsed:
        stream.Position = dictStream.To + 2;

        output!.ByteOffset = initialStreamPosition;

        Logger.Log(LogLevel.Trace, $"Parsed Dictionary from {stream.GetType().Name} between offsets: {initialStreamPosition} - {stream.Position}");

        return output;
    }

    private static async Task<Dictionary> CreateAnnotationDictionaryAsync(Dictionary<Name, IPdfObject> dict, IPdfEditor pdfEditor)
    {
        Dictionary? output = (string)(Name)dict[Constants.DictionaryKeys.Subtype] switch
        {
            AnnotationDictionary.Subtypes.Widget => WidgetAnnotationDictionary.FromDictionary(dict, pdfEditor),
            _ => AnnotationDictionary.FromDictionary(dict, pdfEditor),
        };

        if (dict.ContainsKey(Constants.DictionaryKeys.Field.FT))
        {
            output = FieldDictionary.FromDictionary(dict, pdfEditor);
        }
        else
        {
            // FT is inheritable, test if this is a field dictionary by creating one and checking FT.
            // This will automatically check the parent hierarchy if FT is not found in the current dictionary.
            var fieldDict = FieldDictionary.FromDictionary(dict, pdfEditor);
            if (await fieldDict.FT.GetAsync() is not null)
            {
                output = fieldDict;
            }
        }

        return output;
    }
}
