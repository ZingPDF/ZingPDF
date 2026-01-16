using Microsoft.Extensions.DependencyInjection;
using ZingPDF.DocumentInterchange.Metadata;
using ZingPDF.Graphics;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.Graphics.Images;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Linearization;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Encryption;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.ObjectStreams;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Text;
using ZingPDF.Text.CompositeFonts;
using ZingPDF.Text.Encoding;
using ZingPDF.Text.SimpleFonts;

namespace ZingPDF.Parsing.Parsers.Objects.Dictionaries;

public class DictionaryIdentifier : IDictionaryIdentifier
{
    private static readonly Dictionary<string, Type> _dictionaryTypeMap = new()
    {
        [Constants.DictionaryTypes.Catalog] = typeof(DocumentCatalogDictionary),
        [Constants.DictionaryTypes.Page] = typeof(PageDictionary),
        [Constants.DictionaryTypes.Pages] = typeof(PageTreeNodeDictionary),
        [Constants.DictionaryTypes.XRef] = typeof(CrossReferenceStreamDictionary),
        [Constants.DictionaryTypes.ObjStm] = typeof(ObjectStreamDictionary),
        [Constants.DictionaryTypes.XObject] = typeof(XObjectDictionary),
        [Constants.DictionaryTypes.Font] = typeof(FontDictionary),
        [Constants.DictionaryTypes.FontDescriptor] = typeof(FontDescriptorDictionary),
        [Constants.DictionaryTypes.Annot] = typeof(AnnotationDictionary),
        [Constants.DictionaryTypes.Metadata] = typeof(MetadataStreamDictionary),
        [Constants.DictionaryTypes.Encoding] = typeof(EncodingDictionary),
    };

    private static readonly Dictionary<string, Type> _dictionarySubtypeMap = new()
    {
        [XObjectDictionary.Subtypes.Form] = typeof(Type1FormDictionary),
        [XObjectDictionary.Subtypes.Image] = typeof(ImageDictionary),
        [Constants.DictionaryTypes.Annot] = typeof(AnnotationDictionary),
        [AnnotationDictionary.Subtypes.Widget] = typeof(WidgetAnnotationDictionary),
        [FontDictionary.Subtypes.Simple.Type1] = typeof(Type1FontDictionary),
        [FontDictionary.Subtypes.Simple.TrueType] = typeof(TrueTypeFontDictionary),
        [FontDictionary.Subtypes.Simple.Type3] = typeof(Type3FontDictionary),
        [FontDictionary.Subtypes.Composite.Type0] = typeof(Type0FontDictionary),
        [FontDictionary.Subtypes.CID.CIDFontType0] = typeof(CIDFontDictionary),
        [FontDictionary.Subtypes.CID.CIDFontType2] = typeof(CIDFontDictionary),
    };

    private readonly IPdf _pdf;

    public DictionaryIdentifier(IPdf pdf)
    {
        _pdf = pdf;
    }

    public async Task<Type?> IdentifyAsync(Dictionary<string, IPdfObject> dictionary)
    {
        _ = dictionary.TryGetValue(Constants.DictionaryKeys.Type, out IPdfObject? type);
        _ = dictionary.TryGetValue(Constants.DictionaryKeys.Subtype, out IPdfObject? subtype);

        if (type is not null)
        {
            _ = _dictionaryTypeMap.TryGetValue((Name)type, out Type? dictType);
            Type? dictSubtype = null;

            // Widget annotations can double as form fields
            if (subtype is not null)
            {
                if ((Name)subtype == AnnotationDictionary.Subtypes.Widget)
                {
                    if (dictionary.ContainsKey(Constants.DictionaryKeys.Field.FT))
                    {
                        return typeof(FieldDictionary);
                    }
                    else
                    {
                        // FT is inheritable, test if this is a field dictionary by creating one and checking FT.
                        // This will automatically check the parent hierarchy if FT is not found in the current dictionary.
                        var fieldDict = FieldDictionary.FromDictionary(dictionary, _pdf, ObjectContext.None);
                        if (await fieldDict.FT.GetAsync() is not null)
                        {
                            return typeof(FieldDictionary);
                        }
                    }
                }

                _ = _dictionarySubtypeMap.TryGetValue((Name)subtype, out dictSubtype);
            }

            return dictSubtype ?? dictType;
        }

        if (dictionary.ContainsKey(Constants.DictionaryKeys.InteractiveForm.Fields))
        {
            return typeof(InteractiveFormDictionary);
        }

        if (dictionary.ContainsKey(Constants.DictionaryKeys.LinearizationParameter.Linearized))
        {
            return typeof(LinearizationParameterDictionary);
        }

        if (dictionary.TryGetValue(Constants.DictionaryKeys.Encryption.Filter, out var filterValue)
            && filterValue is Name filterName
            && filterName == "Standard"
            && dictionary.ContainsKey(Constants.DictionaryKeys.Encryption.Standard.O)
            && dictionary.ContainsKey(Constants.DictionaryKeys.Encryption.Standard.U))
        {
            return typeof(StandardEncryptionDictionary);
        }

        // TODO: using length to identify a stream dictionary, which seems dodgy, revisit this to make it more reliable.
        if (dictionary.ContainsKey(Constants.DictionaryKeys.Stream.Length))
        {
            return typeof(StreamDictionary);
        }

        if (dictionary.ContainsKey(Constants.DictionaryKeys.Appearance.N))
        {
            return typeof(AppearanceDictionary);
        }

        return null;
    }
}
