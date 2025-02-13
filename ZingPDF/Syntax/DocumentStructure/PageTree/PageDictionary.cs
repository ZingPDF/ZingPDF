using ZingPDF.Extensions;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Syntax.DocumentStructure.PageTree
{
    /// <summary>
    /// ISO 32000-2:2020 7.7.3.3 - Page objects
    /// </summary>
    public class PageDictionary : PageNode
    {
        private PageDictionary(Dictionary pageDictionary) : base(pageDictionary) { }

        /// <summary>
        /// (Optional; PDF 1.3) A rectangle, expressed in default user space units, that shall define 
        /// the region to which the contents of the page shall be clipped when output in a production 
        /// environment (see 14.11.2, "Page boundaries"). Default value: the value of CropBox.
        /// </summary>
        public Rectangle? BleedBox=> Get<Rectangle>(Constants.DictionaryKeys.PageTree.Page.BleedBox);

        /// <summary>
        /// (Optional; PDF 1.3) A rectangle, expressed in default user space units, that shall define 
        /// the intended dimensions of the finished page after trimming (see 14.11.2, "Page boundaries"). 
        /// Default value: the value of CropBox.
        /// </summary>
        public Rectangle? TrimBox => Get<Rectangle>(Constants.DictionaryKeys.PageTree.Page.TrimBox);

        /// <summary>
        /// (Optional; PDF 1.3) A rectangle, expressed in default user space units, that shall define 
        /// the extent of the page’s meaningful content (including potential white-space) as intended 
        /// by the page’s creator (see 14.11.2, "Page boundaries"). Default value: the value of CropBox.
        /// </summary>
        public Rectangle? ArtBox => Get<Rectangle>(Constants.DictionaryKeys.PageTree.Page.ArtBox);

        /// <summary>
        /// (Optional; PDF 1.4) A box colour information dictionary that shall specify the colours and 
        /// other visual characteristics that should be used in displaying guidelines on the screen for 
        /// the various page boundaries (see 14.11.2.2, "Display of page boundaries"). If this entry is 
        /// absent, the application shall use its own current default settings.
        /// </summary>
        public Dictionary? BoxColorInfo => Get<Dictionary>(Constants.DictionaryKeys.PageTree.Page.BoxColorInfo);

        /// <summary>
        /// <para>(Optional) A content stream (see 7.8.2, "Content streams") that shall describe the 
        /// contents of this page. If this entry is absent, the page shall be empty.</para>
        /// <para>The value shall be either a single stream or an array of streams. If the value is 
        /// an array, the effect shall be as if all of the streams in the array were concatenated with 
        /// at least one white-space character added between the streams’ data, in order, to form a 
        /// single stream. PDF writers can create image objects and other resources as they occur, even 
        /// though they interrupt the content stream. The division between streams may occur only at the 
        /// boundaries between lexical tokens (see 7.2, "Lexical conventions") but shall be unrelated to 
        /// the page’s logical content or organisation. Applications that consume or produce PDF files 
        /// need not preserve the existing structure of the Contents array. PDF writers shall not create 
        /// a Contents array containing no elements.</para>
        /// </summary>
        public IPdfObject? Contents => Get<IPdfObject>(Constants.DictionaryKeys.PageTree.Page.Contents);

        /// <summary>
        /// (Optional; PDF 1.4) A group attributes dictionary that shall specify the attributes of 
        /// the page’s page group for use in the transparent imaging model (see 11.4.7, "Page group" 
        /// and 11.6.6, "Transparency group XObjects").
        /// </summary>
        public Dictionary? Group => Get<Dictionary>(Constants.DictionaryKeys.PageTree.Page.Group);

        /// <summary>
        /// (Optional) A stream object that shall define the page’s thumbnail image (see 12.3.4, "Thumbnail images").
        /// </summary>
        public IndirectObjectReference? Thumb => Get<IndirectObjectReference>(Constants.DictionaryKeys.PageTree.Page.Thumb);

        /// <summary>
        /// <para>(Optional; PDF 1.1; recommended if the page contains article beads) An array that shall contain 
        /// indirect references to all article beads appearing on the page (see 12.4.3, "Articles"). The beads 
        /// shall be listed in the array in natural reading order. Objects of Type Template shall have no B key.</para>
        /// <para>NOTE 2 The information in this entry can be created or recreated from the information obtained 
        /// from the Threads key in the catalog dictionary.</para>
        /// </summary>
        public ArrayObject? B => Get<ArrayObject>(Constants.DictionaryKeys.PageTree.Page.B);

        /// <summary>
        /// (Optional; PDF 1.1) The page’s display duration (also called its advance timing): the maximum length 
        /// of time, in seconds, that the page shall be displayed during presentations before the viewer application 
        /// shall automatically advance to the next page (see 12.4.4, "Presentations"). By default, the viewer shall 
        /// not advance automatically.
        /// </summary>
        public RealNumber? Dur => Get<RealNumber>(Constants.DictionaryKeys.PageTree.Page.Dur);

        /// <summary>
        /// (Optional; PDF 1.1) A transition dictionary describing the transition effect that shall be used when 
        /// displaying the page during presentations (see 12.4.4, "Presentations").
        /// </summary>
        public Dictionary? Trans => Get<Dictionary>(Constants.DictionaryKeys.PageTree.Page.Trans);

        /// <summary>
        /// (Optional) An array of annotation dictionaries that shall contain indirect references to all 
        /// annotations associated with the page (see 12.5, "Annotations").
        /// </summary>
        public IndirectObjectReference? Annots => Get<IndirectObjectReference>(Constants.DictionaryKeys.PageTree.Page.Annots);

        /// <summary>
        /// (Optional; PDF 1.2) An additional-actions dictionary that shall define actions to be performed 
        /// when the page is opened or closed (see 12.6.3, "Trigger events"). (PDF 1.3) additional-actions 
        /// dictionaries are not inheritable.
        /// </summary>
        public Dictionary? AA => Get<Dictionary>(Constants.DictionaryKeys.PageTree.Page.AA);

        /// <summary>
        /// (Optional; PDF 1.4) A metadata stream that shall contain metadata for the page (see 14.3.2, "Metadata streams").
        /// </summary>
        public IndirectObjectReference? Metadata => Get<IndirectObjectReference>(Constants.DictionaryKeys.PageTree.Page.Metadata);

        /// <summary>
        /// (Optional; PDF 1.3) A page-piece dictionary associated with the page (see 14.5, "Page-piece dictionaries").
        /// </summary>
        public Dictionary? PieceInfo => Get<Dictionary>(Constants.DictionaryKeys.PageTree.Page.PieceInfo);

        /// <summary>
        /// (Required if the page contains structural content items; PDF 1.3) The integer key of the page’s entry 
        /// in the structural parent tree (see 14.7.5.4, "Finding structure elements from content items").
        /// </summary>
        public Integer? StructParents => Get<Integer>(Constants.DictionaryKeys.PageTree.Page.StructParents);

        /// <summary>
        /// (Optional; PDF 1.3; indirect reference preferred) The digital identifier of the page’s parent Web Capture 
        /// content set (see 14.10.6, "Object attributes related to web capture").
        /// </summary>
        public HexadecimalString? ID => Get<HexadecimalString>(Constants.DictionaryKeys.PageTree.Page.ID);

        /// <summary>
        /// (Optional; PDF 1.3) The page’s preferred zoom (magnification) factor: the factor by which it shall be 
        /// scaled to achieve the natural display magnification (see 14.10.6, "Object attributes related to web capture").
        /// </summary>
        public RealNumber? PZ => Get<RealNumber>(Constants.DictionaryKeys.PageTree.Page.PZ);

        /// <summary>
        /// (Optional; PDF 1.3) A separation dictionary that shall contain information needed to generate colour separations 
        /// for the page (see 14.11.4, "Separation dictionaries").
        /// </summary>
        public Dictionary? SeparationInfo => Get<Dictionary>(Constants.DictionaryKeys.PageTree.Page.SeparationInfo);

        /// <summary>
        /// (Optional; PDF 1.5) A name specifying the tab order that shall be used for annotations on the page 
        /// (see 12.5 "Annotations"). If present, the values shall be one of R (row order), C (column order), 
        /// and S (structure order). Beginning with PDF 2.0, additional values also include A (annotations array order) 
        /// and W (widget order). Annotations array order refers to the order of the annotation enumerated in the Annots 
        /// entry of the Page dictionary (see "Table 31 — Entries in a page object"). Widget order means using the same 
        /// array ordering but making two passes, the first only picking the widget annotations and the second picking 
        /// all other annotations.
        /// </summary>
        public Name? Tabs => Get<Name>(Constants.DictionaryKeys.PageTree.Page.Tabs);

        /// <summary>
        /// (Required if this page was created from a named page object; PDF 1.5) The name of the originating page object 
        /// (see 12.7.7, "Named pages").
        /// </summary>
        public Name? TemplateInstantiated => Get<Name>(Constants.DictionaryKeys.PageTree.Page.TemplateInstantiated);

        /// <summary>
        /// (Optional; PDF 1.5) A navigation node dictionary that shall represent the first node on the page 
        /// (see 12.4.4.2, "Sub-page navigation").
        /// </summary>
        public Dictionary? PresSteps => Get<Dictionary>(Constants.DictionaryKeys.PageTree.Page.PresSteps);

        /// <summary>
        /// <para>(Optional; PDF 1.6) A positive number that shall give the size of default user space units, in multiples of 1 ⁄ 72 inch. The range of supported values shall be implementation-dependent.</para>
        /// <para>Default value: 1.0 (user space unit is 1 ⁄ 72 inch).</para>
        /// </summary>
        public RealNumber? UserUnit => Get<RealNumber>(Constants.DictionaryKeys.PageTree.Page.UserUnit);

        /// <summary>
        /// (Optional; PDF 1.6) An array of viewport dictionaries (see "Table 265 — Entries in a viewport dictionary") 
        /// that shall specify rectangular regions of the page.
        /// </summary>
        public ArrayObject? VP => Get<ArrayObject>(Constants.DictionaryKeys.PageTree.Page.VP);

        /// <summary>
        /// (Optional; PDF 2.0) An array of one or more file specification dictionaries 
        /// (7.11.3, "File specification dictionaries") which denote the associated files 
        /// for this page. See 14.13, "Associated files" and 14.13.8, "Associated files linked to DParts" for more details.
        /// </summary>
        public ArrayObject? AF => Get<ArrayObject>(Constants.DictionaryKeys.PageTree.Page.AF);

        /// <summary>
        /// (Optional; PDF 2.0) An array of output intent dictionaries that shall specify the colour characteristics of output 
        /// devices on which this page might be rendered (see 14.11.5, "Output intents").
        /// </summary>
        public ArrayObject? OutputIntents => Get<ArrayObject>(Constants.DictionaryKeys.PageTree.Page.OutputIntents);

        /// <summary>
        /// <para>(Required, if this page is within the range of a DPart, not permitted otherwise; PDF 2.0) 
        /// An indirect reference to the DPart dictionary whose range of pages includes this page object 
        /// (see 14.12.3, "Connecting the DPart tree structure to pages").</para>
        /// <para>NOTE 3 The DPart key in a page object allows a PDF processor to directly retrieve 
        /// the section of the document part hierarchy that applies to this page object. 
        /// This also allows for ready access of DPM data in PDF processors.</para>
        /// </summary>
        public Dictionary? DPart => Get<Dictionary>(Constants.DictionaryKeys.PageTree.Page.DPart);

        public void AddContent(IEnumerable<ContentStreamObject> content, IPdfEditor pdfEditor)
        {
            ArgumentNullException.ThrowIfNull(content, nameof(content));
            ArgumentNullException.ThrowIfNull(pdfEditor, nameof(pdfEditor));

            if (Contents is null)
            {
                Set(Constants.DictionaryKeys.PageTree.Page.Contents, ArrayObject.Empty);
            }
            else if (Contents is IndirectObjectReference ior)
            {
                Set(Constants.DictionaryKeys.PageTree.Page.Contents, new ArrayObject([ior]));
            }

            var contentStream = new ContentStreamFactory<StreamDictionary>(
                content,
                StreamDictionary.FromDictionary(Empty)
                )
                .Create();

            var contentObject = pdfEditor.Add(contentStream);

            var contents = (Contents as ArrayObject)!;

            contents.Add(contentObject.Id.Reference);
        }

        /// <summary>
        /// Create a blank page.
        /// </summary>
        /// <param name="parent">An <see cref="IndirectObjectReference"/> pointing to the page's parent. This shall be an <see cref="IndirectObjectReference"/> to a <see cref="PageTreeNodeDictionary"/>.</param>
        /// <returns>A <see cref="PageDictionary"/> instance.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static PageDictionary CreateNew(IndirectObjectReference parent, PageCreationOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(parent);

            options ??= new PageCreationOptions();

            var dict = new Dictionary<Name, IPdfObject>
            {
                { Constants.DictionaryKeys.Type, new Name(Constants.DictionaryTypes.Page) },
                { Constants.DictionaryKeys.PageTree.Parent, parent },
                { Constants.DictionaryKeys.PageTree.Resources, Empty },
            };

            if (options.MediaBox is not null)
            {
                dict[Constants.DictionaryKeys.PageTree.MediaBox] = options.MediaBox;
            }

            return new(dict);
        }

        /// <summary>
        /// Create a page from an existing page dictionary.
        /// </summary>
        /// <param name="pageDictionary"></param>
        /// <returns></returns>
        internal static PageDictionary FromDictionary(Dictionary pageDictionary)
        {
            ArgumentNullException.ThrowIfNull(pageDictionary);

            return new PageDictionary(pageDictionary);
        }

        public class PageCreationOptions
        {
            private static readonly PageCreationOptions _default = new();

            /// <summary>
            /// Represents the overall size of the page.
            /// </summary>
            public Rectangle? MediaBox { get; set; }

            public static readonly PageCreationOptions Default = _default;

            public static PageCreationOptions Initialize(Action<PageCreationOptions>? configure)
            {
                var options = Default.Clone();
                configure?.Invoke(options);
                return options;
            }

            public PageCreationOptions Clone()
            {
                var deepCopy = new PageCreationOptions();

                if (MediaBox != null)
                {
                    deepCopy.MediaBox = new Rectangle(MediaBox.LowerLeft, MediaBox.UpperRight);
                }

                return deepCopy;
            }
        }
    }
}
