using ZingPDF.DocumentInterchange.Metadata;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Syntax.DocumentStructure
{
    /// <summary>
    /// ISO 32000-2:2020 7.7.2 - Document catalog dictionary
    /// </summary>
    public class DocumentCatalogDictionary : Dictionary
    {
        public DocumentCatalogDictionary(Dictionary documentCatalogDictionary)
            : base(documentCatalogDictionary) { }

        private DocumentCatalogDictionary(Dictionary<string, IPdfObject> documentCatalogDictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin)
            : base(documentCatalogDictionary, pdfContext, objectOrigin) { }

        /// <summary>
        /// <para>
        /// (Optional; PDF 1.4) The version of the PDF specification to which the document conforms (for example, 1.4) 
        /// if later than the version specified in the file’s header (see 7.5.2, "File header"). 
        /// If the header specifies a later version, or if this entry is absent, the document 
        /// shall conform to the version specified in the header. This entry enables a PDF processor 
        /// to update the version using an incremental update; see 7.5.6, "Incremental updates".
        /// </para>
        /// <para>
        /// The value of this entry shall be a name object, not a number, and therefore shall 
        /// be preceded by a SOLIDUS (2Fh) character (/) when written in the PDF file (for example, /1.4).
        /// </para>
        /// </summary>
        public OptionalProperty<Name> Version => GetOptionalProperty<Name>(Constants.DictionaryKeys.DocumentCatalog.Version);

        /// <summary>
        /// (Optional; ISO 32000-1) An extensions dictionary containing developer prefix identification and 
        /// version numbers for developer extensions that occur in this document. 7.12, "Extensions dictionary", 
        /// describes this dictionary and how it shall be used.
        /// </summary>
        public OptionalProperty<ExtensionsDictionary> Extensions => GetOptionalProperty<ExtensionsDictionary>(Constants.DictionaryKeys.DocumentCatalog.Extensions);

        /// <summary>
        /// (Required) The page tree node that shall be the root of the document’s page tree (see 7.7.3, "Page tree").
        /// </summary>
        public RequiredProperty<PageTreeNodeDictionary> Pages => GetRequiredProperty<PageTreeNodeDictionary>(Constants.DictionaryKeys.DocumentCatalog.Pages);

        /// <summary>
        /// (Optional; PDF 1.3) A number tree (see 7.9.7, "Number trees") defining the page labelling for the document. 
        /// The keys in this tree shall be page indices; the corresponding values shall be page label dictionaries 
        /// (see 12.4.2, "Page labels"). Each page index shall denote the first page in a labelling range to which 
        /// the specified page label dictionary applies. The tree shall include a value for page index 0.
        /// </summary>
        // TODO: Implement NumberTreeNodeDictionary
        public OptionalProperty<Dictionary> PageLabels => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.PageLabels);

        /// <summary>
        /// <para>
        /// (Optional; PDF 1.2) The document’s name dictionary (see 7.7.4, "Name dictionary").
        /// </para>
        /// <para>
        /// (PDF 2.0) For unencrypted wrapper documents for an encrypted payload document 
        /// (see 7.6.7, "Unencrypted wrapper document") the Names dictionary is required and shall contain the EmbeddedFiles name tree.
        /// </para>
        /// </summary>
        public OptionalProperty<NameDictionary> Names => GetOptionalProperty<NameDictionary>(Constants.DictionaryKeys.DocumentCatalog.Names);

        /// <summary>
        /// (Optional; PDF 1.1; shall be an indirect reference) A dictionary of names and corresponding destinations (see 12.3.2.4, "Named destinations").
        /// </summary>
        public OptionalProperty<Dictionary> Dests => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.Dests);

        /// <summary>
        /// (Optional; PDF 1.2) A viewer preferences dictionary (see 12.2, "Viewer preferences") specifying the way the document shall be 
        /// displayed on the screen. If this entry is absent, PDF readers shall use their own current user preference settings.
        /// </summary>
        public OptionalProperty<Dictionary> ViewerPreferences => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.ViewerPreferences);

        /// <summary>
        /// <para>(Optional) A name object specifying the page layout shall be used when the document is opened:</para>
        /// <para>SinglePage        Display one page at a time</para>
        /// <para>OneColumn         Display the pages in one column</para>
        /// <para>TwoColumnLeft     Display the pages in two columns, with odd-numbered pages on the left</para>
        /// <para>TwoColumnRight    Display the pages in two columns, with odd-numbered pages on the right</para>
        /// <para>TwoPageLeft       (PDF 1.5) Display the pages two at a time, with odd-numbered pages on the left</para>
        /// <para>TwoPageRight      (PDF 1.5) Display the pages two at a time, with odd-numbered pages on the right</para>
        /// <para>Default value: SinglePage.</para>
        /// </summary>
        public OptionalProperty<Name> PageLayout => GetOptionalProperty<Name>(Constants.DictionaryKeys.DocumentCatalog.PageLayout);

        /// <summary>
        /// <para>(Optional) A name object specifying how the document shall be displayed when opened:</para>
        /// <para>UseNone           Neither document outline nor thumbnail images visible</para>
        /// <para>UseOutlines       Document outline visible</para>
        /// <para>UseThumbs         Thumbnail images visible</para>
        /// <para>FullScreen        Full-screen mode, with no menu bar, window controls, or any other window visible</para>
        /// <para>UseOC             (PDF 1.5) Optional content group panel visible</para>
        /// <para>UseAttachments    (PDF 1.6) Attachments panel visible</para>
        /// <para>Default value: UseNone.</para>
        /// </summary>
        public OptionalProperty<Name> PageMode => GetOptionalProperty<Name>(Constants.DictionaryKeys.DocumentCatalog.PageMode);

        /// <summary>
        /// (Optional; shall be an indirect reference) The outline dictionary that shall be the root of the document’s outline 
        /// hierarchy (see 12.3.3, "Document outline").
        /// </summary>
        public OptionalProperty<Dictionary> Outlines => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.Outlines);

        /// <summary>
        /// (Optional; PDF 1.1; shall be an indirect reference) An array of thread dictionaries that shall represent the 
        /// document’s article threads (see 12.4.3, "Articles").
        /// </summary>
        public OptionalProperty<ArrayObject> Threads => GetOptionalProperty<ArrayObject>(Constants.DictionaryKeys.DocumentCatalog.Threads);

        /// <summary>
        /// (Optional; PDF 1.1) A value specifying a destination that shall be displayed or an action that shall be performed 
        /// when the document is opened. The value shall be either an array defining a destination (see 12.3.2, "Destinations") 
        /// or an action dictionary representing an action (12.6.2, "Action dictionaries"). If this entry is absent, the document 
        /// shall be opened to the top of the first page at the default magnification factor.
        /// </summary>
        public OptionalArrayOrSingle<Dictionary> OpenAction => GetOptionalArrayOrSingle<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.OpenAction);

        /// <summary>
        /// (Optional; PDF 1.2) An additional-actions dictionary defining the actions that shall be taken in response to various 
        /// trigger events affecting the document as a whole (see 12.6.3, "Trigger events").
        /// </summary>
        public OptionalProperty<Dictionary> AA => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.AA);

        /// <summary>
        /// (Optional; PDF 1.1) A URI dictionary containing document-level information for URI (uniform resource identifier) 
        /// actions (see 12.6.4.8, "URI actions").
        /// </summary>
        public OptionalProperty<Dictionary> URI => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.URI);

        /// <summary>
        /// (Optional; PDF 1.2)<para></para>
        /// The document’s interactive form (AcroForm) dictionary (see 12.7.3, "Interactive form dictionary").
        /// </summary>
        public OptionalProperty<InteractiveFormDictionary> AcroForm => GetOptionalProperty<InteractiveFormDictionary>(Constants.DictionaryKeys.DocumentCatalog.AcroForm);

        /// <summary>
        /// (Optional; PDF 1.4; shall be an indirect reference) A metadata stream that shall contain metadata for the document (see 14.3.2, "Metadata streams").
        /// </summary>
        public OptionalProperty<StreamObject<MetadataStreamDictionary>> Metadata
            => GetOptionalProperty<StreamObject<MetadataStreamDictionary>>(Constants.DictionaryKeys.DocumentCatalog.Metadata);

        /// <summary>
        /// (Optional; PDF 1.3) The document’s structure tree root dictionary (see 14.7.2, "Structure hierarchy").
        /// </summary>
        public OptionalProperty<Dictionary> StructTreeRoot => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.StructTreeRoot);

        /// <summary>
        /// (Optional; PDF 1.4) A mark information dictionary that shall contain information about the document’s usage of tagged PDF conventions (see 14.7, "Logical structure").
        /// </summary>
        public OptionalProperty<Dictionary> MarkInfo => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.MarkInfo);

        /// <summary>
        /// (Optional; PDF 1.4) A language identifier that shall specify the natural language for all text in the document except where overridden by language 
        /// specifications for structure elements or marked-content (see 14.9.2, "Natural language specification"). If this entry is absent, the language shall 
        /// be considered unknown.
        /// </summary>
        public OptionalProperty<LiteralString> Lang => GetOptionalProperty<LiteralString>(Constants.DictionaryKeys.DocumentCatalog.Lang);

        /// <summary>
        /// (Optional; PDF 1.3) A Web Capture information dictionary that shall contain state information used by any Web Capture extension 
        /// (see 14.10.2, "Web capture information dictionary").
        /// </summary>
        public OptionalProperty<Dictionary> SpiderInfo => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.SpiderInfo);

        /// <summary>
        /// (Optional; PDF 1.4) An array of output intent dictionaries that shall specify the colour characteristics of output devices on which the document might 
        /// be rendered (see 14.11.5, "Output intents").
        /// </summary>
        public OptionalProperty<ArrayObject> OutputIntents => GetOptionalProperty<ArrayObject>(Constants.DictionaryKeys.DocumentCatalog.OutputIntents);

        /// <summary>
        /// (Optional; PDF 1.4) A page-piece dictionary associated with the document (see 14.5, "Page-piece dictionaries").
        /// </summary>
        public OptionalProperty<Dictionary> PieceInfo => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.PieceInfo);

        /// <summary>
        /// (Optional; PDF 1.5; required if a document contains optional content) The document’s optional content properties dictionary 
        /// (see 8.11.4, "Configuring optional content").
        /// </summary>
        public OptionalProperty<Dictionary> OCProperties => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.OCProperties);

        /// <summary>
        /// (Optional; PDF 1.5) A permissions dictionary that shall specify user access permissions for the document. 12.8.6, "Permissions", describes 
        /// this dictionary and how it shall be used.
        /// </summary>
        public  OptionalProperty<Dictionary> Perms => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.Perms);

        /// <summary>
        /// (Optional; PDF 1.5) A dictionary that shall contain attestations regarding the content of a PDF document, as it relates to the 
        /// legality of digital signatures (see 12.8.7, "Legal content attestations").
        /// </summary>
        public OptionalProperty<Dictionary> Legal => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.Legal);

        /// <summary>
        /// (Optional; PDF 1.7) An array of requirement dictionaries that shall represent requirements for the document. Subclause 12.11, 
        /// "Document requirements", describes this dictionary and how it shall be used.
        /// </summary>
        public OptionalProperty<ArrayObject> Requirements => GetOptionalProperty<ArrayObject>(Constants.DictionaryKeys.DocumentCatalog.Requirements);

        /// <summary>
        /// <para>
        /// (Optional; PDF 1.7) A collection dictionary that an interactive PDF processor shall use to enhance the presentation of file attachments 
        /// stored in the PDF document. (see 12.3.5, "Collections").
        /// </para>
        /// <para>
        /// (PDF 2.0) For unencrypted wrapper documents for an encrypted payload document (see 7.6.7, "Unencrypted wrapper document") the Collection 
        /// is required and shall identify the encrypted payload as the default document (as indicated by the D entry) of the collection and shall 
        /// further specify that the collection View should be initially H (hidden).
        /// </para>
        /// </summary>
        public OptionalProperty<Dictionary> Collection => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.Collection);

        /// <summary>
        /// <para>
        /// (Optional; deprecated in PDF 2.0) A flag used to expedite the display of PDF documents containing XFA forms. It specifies whether the 
        /// document shall be regenerated when the document is first opened. See Annex K, “XFA forms”.
        /// </para>
        /// <para>Default value: false.</para>
        /// </summary>
        public OptionalProperty<BooleanObject> NeedsRendering => GetOptionalProperty<BooleanObject>(Constants.DictionaryKeys.DocumentCatalog.NeedsRendering);

        /// <summary>
        /// (Optional; PDF 2.0) A DSS dictionary containing document-wide security information. See 12.8.4.3, "Document Security Store (DSS)".
        /// </summary>
        public OptionalProperty<Dictionary> DSS => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.DSS);

        /// <summary>
        /// <para>
        /// (Optional; PDF 2.0) An array of one or more file specification dictionaries (7.11.3, "File specification dictionaries") which denote the associated 
        /// files for this PDF document. See 14.13, "Associated files" and 14.13.3, "Associated files linked to the PDF document’s catalog" for more details.
        /// </para>
        /// <para>
        /// For unencrypted wrapper documents for an encrypted payload document (see 7.6.7, "Unencrypted wrapper document") the AF key is required and shall 
        /// include a reference to the file specification dictionary for the encrypted payload document.
        /// </para>
        /// </summary>
        public OptionalProperty<ArrayObject> AF => GetOptionalProperty<ArrayObject>(Constants.DictionaryKeys.DocumentCatalog.AF);

        /// <summary>
        /// (Optional; PDF 2.0) A DPartRoot dictionary used to describe the document parts hierarchy for this PDF document. See 14.12, "Document parts".
        /// </summary>
        public OptionalProperty<Dictionary> DPartRoot => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.DocumentCatalog.DPartRoot);

        public static DocumentCatalogDictionary FromDictionary(Dictionary<string, IPdfObject> documentCatalogDictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin) 
        {
            return documentCatalogDictionary is null
                ? throw new ArgumentNullException(nameof(documentCatalogDictionary))
                : new(documentCatalogDictionary, pdfContext, objectOrigin);
        }
    }
}
