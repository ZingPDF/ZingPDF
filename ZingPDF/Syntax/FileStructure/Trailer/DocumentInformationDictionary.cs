using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Syntax.FileStructure.Trailer;

/// <summary>
/// Represents the document information dictionary referenced from the trailer <c>Info</c> entry.
/// </summary>
public class DocumentInformationDictionary : Dictionary
{
    public DocumentInformationDictionary(Dictionary dictionary)
        : base(dictionary)
    {
    }

    private DocumentInformationDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectContext context)
        : base(dictionary, pdf, context)
    {
    }

    public OptionalProperty<PdfString> Title => GetOptionalProperty<PdfString>(Constants.DictionaryKeys.DocumentInformation.Title);
    public OptionalProperty<PdfString> Author => GetOptionalProperty<PdfString>(Constants.DictionaryKeys.DocumentInformation.Author);
    public OptionalProperty<PdfString> Subject => GetOptionalProperty<PdfString>(Constants.DictionaryKeys.DocumentInformation.Subject);
    public OptionalProperty<PdfString> Keywords => GetOptionalProperty<PdfString>(Constants.DictionaryKeys.DocumentInformation.Keywords);
    public OptionalProperty<PdfString> Creator => GetOptionalProperty<PdfString>(Constants.DictionaryKeys.DocumentInformation.Creator);
    public OptionalProperty<PdfString> Producer => GetOptionalProperty<PdfString>(Constants.DictionaryKeys.DocumentInformation.Producer);
    public OptionalProperty<Date> CreationDate => GetOptionalProperty<Date>(Constants.DictionaryKeys.DocumentInformation.CreationDate);
    public OptionalProperty<Date> ModDate => GetOptionalProperty<Date>(Constants.DictionaryKeys.DocumentInformation.ModDate);
    public OptionalProperty<Name> Trapped => GetOptionalProperty<Name>(Constants.DictionaryKeys.DocumentInformation.Trapped);

    internal static DocumentInformationDictionary CreateNew(IPdf pdf, ObjectContext context)
        => new([], pdf, context);

    internal static DocumentInformationDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectContext context)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        return new DocumentInformationDictionary(dictionary, pdf, context);
    }
}
