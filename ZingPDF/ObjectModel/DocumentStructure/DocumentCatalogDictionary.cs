using ZingPDF.Forms;
using ZingPDF.ObjectModel.CommonDataStructures;
using ZingPDF.ObjectModel.Objects;
using ZingPDF.ObjectModel.Objects.IndirectObjects;

namespace ZingPDF.ObjectModel.DocumentStructure
{
    /// <summary>
    /// ISO 32000-2:2020 7.7.2 - Document catalog dictionary
    /// </summary>
    public class DocumentCatalogDictionary : Dictionary
    {
        public static class DictionaryKeys
        {
            public const string Version = "Version";
            public const string Extensions = "Extensions";
            public const string Pages = "Pages";
            public const string PageLabels = "PageLabels";

            public const string AcroForm = "AcroForm";
        }

        private DocumentCatalogDictionary(Dictionary documentCatalogDictionary) : base(documentCatalogDictionary) { }

        public static Name Type => Constants.DictionaryTypes.Catalog;

        /// <summary>
        /// (Optional; PDF 1.4)<para></para>
        /// The version of the PDF specification to which the document conforms (for example, 1.4) 
        /// if later than the version specified in the file’s header (see 7.5.2, "File header"). 
        /// If the header specifies a later version, or if this entry is absent, the document 
        /// shall conform to the version specified in the header. This entry enables a PDF processor 
        /// to update the version using an incremental update; see 7.5.6, "Incremental updates".<para></para>
        /// The value of this entry shall be a name object, not a number, and therefore shall 
        /// be preceded by a SOLIDUS (2Fh) character (/) when written in the PDF file (for example, /1.4).
        /// </summary>
        public Name? Version => Get<Name>(DictionaryKeys.Version);

        /// <summary>
        /// (Optional; ISO 32000-1)<para></para>
        /// An extensions dictionary containing developer prefix identification and version numbers 
        /// for developer extensions that occur in this document. 7.12, "Extensions dictionary", 
        /// describes this dictionary and how it shall be used.
        /// </summary>
        public ExtensionsDictionary? Extensions => Get<ExtensionsDictionary>(DictionaryKeys.Extensions);

        /// <summary>
        /// (Required)<para></para>
        /// The page tree node that shall be the root of the document’s page tree (see 7.7.3, "Page tree").
        /// </summary>
        public IndirectObjectReference Pages => Get<IndirectObjectReference>(DictionaryKeys.Pages)!;

        /// <summary>
        /// (Optional; PDF 1.3)<para></para>
        /// A number tree (see 7.9.7, "Number trees") defining the page labelling for the document. 
        /// The keys in this tree shall be page indices; the corresponding values shall be page label dictionaries 
        /// (see 12.4.2, "Page labels"). Each page index shall denote the first page in a labelling range to which 
        /// the specified page label dictionary applies. The tree shall include a value for page index 0.
        /// </summary>
        // TODO: Implement NumberTreeNodeDictionary
        public Dictionary? PageLabels => Get<Dictionary>(DictionaryKeys.PageLabels);

        /// <summary>
        /// (Optional; PDF 1.2)<para></para>
        /// The document’s interactive form (AcroForm) dictionary (see 12.7.3, "Interactive form dictionary").
        /// </summary>
        public IndirectObjectReference? AcroForm => Get<IndirectObjectReference>(DictionaryKeys.AcroForm);

        public static DocumentCatalogDictionary FromDictionary(Dictionary documentCatalogDictionary) 
        {
            return documentCatalogDictionary is null
                ? throw new ArgumentNullException(nameof(documentCatalogDictionary))
                : new(documentCatalogDictionary);
        }
    }
}
