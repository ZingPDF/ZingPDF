using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.DocumentStructure.PageTree
{
    /// <summary>
    /// ISO 32000-2:2020 7.7.3.3 - Page objects
    /// </summary>
    public class PageDictionary : Dictionary
    {
        private PageDictionary(Dictionary pageDictionary) : base(pageDictionary) { }

        /// <summary>
        /// Required.<para></para>
        /// The page tree node that is the immediate parent of this page object.
        /// Objects of Type Template shall have no Parent key.
        /// </summary>
        public IndirectObjectReference Parent => Get<IndirectObjectReference>(Constants.DictionaryKeys.Page.Parent)!;

        /// <summary>
        /// (Required; inheritable)<para></para>
        /// A dictionary containing any resources required by the page contents (see 7.8.3, "Resource dictionaries").
        /// If the page requires no resources, the value of this entry shall be an empty dictionary.
        /// Omitting the entry entirely indicates that the resources shall be inherited from an ancestor 
        /// node in the page tree, but PDF writers should not use this method of sharing resources as 
        /// described in 7.8.3, "Resource dictionaries".
        /// </summary>
        public IPdfObject? Resources => Get<IPdfObject>(Constants.DictionaryKeys.Page.Resources);

        /// <summary>
        /// (Required; inheritable) A rectangle (see 7.9.5, "Rectangles"), expressed in default user space units, 
        /// that shall define the boundaries of the physical medium on which the page shall be displayed or printed 
        /// (see 14.11.2, "Page boundaries").
        /// </summary>
        public Rectangle? MediaBox => Get<Rectangle>(Constants.DictionaryKeys.Page.MediaBox);

        /// <summary>
        /// <para>(Optional; Inheritable) A rectangle, expressed in default user space units, that 
        /// shall define the visible region of default user space. When the page is displayed or 
        /// printed, its contents shall be clipped (cropped) to this rectangle (see 14.11.2, "Page boundaries"). 
        /// Default value: the value of MediaBox.</para>
        /// <para>NOTE 1 This clipped page output will often be placed (imposed) on the output medium 
        /// in some implementation-defined manner.</para>
        /// </summary>
        public Rectangle? CropBox => Get<Rectangle>(Constants.DictionaryKeys.Page.CropBox);

        /// <summary>
        /// (Optional; PDF 1.3) A rectangle, expressed in default user space units, that shall define 
        /// the region to which the contents of the page shall be clipped when output in a production 
        /// environment (see 14.11.2, "Page boundaries"). Default value: the value of CropBox.
        /// </summary>
        public Rectangle? BleedBox=> Get<Rectangle>(Constants.DictionaryKeys.Page.BleedBox);

        /// <summary>
        /// (Optional; PDF 1.3) A rectangle, expressed in default user space units, that shall define 
        /// the intended dimensions of the finished page after trimming (see 14.11.2, "Page boundaries"). 
        /// Default value: the value of CropBox.
        /// </summary>
        public Rectangle? TrimBox => Get<Rectangle>(Constants.DictionaryKeys.Page.TrimBox);

        /// <summary>
        /// (Optional; PDF 1.3) A rectangle, expressed in default user space units, that shall define 
        /// the extent of the page’s meaningful content (including potential white-space) as intended 
        /// by the page’s creator (see 14.11.2, "Page boundaries"). Default value: the value of CropBox.
        /// </summary>
        public Rectangle? ArtBox => Get<Rectangle>(Constants.DictionaryKeys.Page.ArtBox);

        /// <summary>
        /// (Optional; PDF 1.4) A box colour information dictionary that shall specify the colours and 
        /// other visual characteristics that should be used in displaying guidelines on the screen for 
        /// the various page boundaries (see 14.11.2.2, "Display of page boundaries"). If this entry is 
        /// absent, the application shall use its own current default settings.
        /// </summary>
        public Dictionary? BoxColorInfo => Get<Dictionary>(Constants.DictionaryKeys.Page.BoxColorInfo);

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
        public IPdfObject? Contents => Get<IPdfObject>(Constants.DictionaryKeys.Page.Contents);

        /// <summary>
        /// The number of degrees by which the page shall be rotated when displayed or printed.
        /// </summary>
        public Integer? Rotate => Get<Integer>(Constants.DictionaryKeys.Page.Rotate);

        /// <summary>
        /// (Optional; PDF 1.4) A group attributes dictionary that shall specify the attributes of 
        /// the page’s page group for use in the transparent imaging model (see 11.4.7, "Page group" 
        /// and 11.6.6, "Transparency group XObjects").
        /// </summary>
        public Dictionary? Group => Get<Dictionary>(Constants.DictionaryKeys.Page.Group);

        /// <summary>
        /// (Optional) A stream object that shall define the page’s thumbnail image (see 12.3.4, "Thumbnail images").
        /// </summary>
        public IndirectObjectReference? Thumb => Get<IndirectObjectReference>(Constants.DictionaryKeys.Page.Thumb);

        /// <summary>
        /// <para>(Optional; PDF 1.1; recommended if the page contains article beads) An array that shall contain 
        /// indirect references to all article beads appearing on the page (see 12.4.3, "Articles"). The beads 
        /// shall be listed in the array in natural reading order. Objects of Type Template shall have no B key.</para>
        /// <para>NOTE 2 The information in this entry can be created or recreated from the information obtained 
        /// from the Threads key in the catalog dictionary.</para>
        /// </summary>
        public ArrayObject? B => Get<ArrayObject>(Constants.DictionaryKeys.Page.B);

        /// <summary>
        /// (Optional; PDF 1.1) The page’s display duration (also called its advance timing): the maximum length 
        /// of time, in seconds, that the page shall be displayed during presentations before the viewer application 
        /// shall automatically advance to the next page (see 12.4.4, "Presentations"). By default, the viewer shall 
        /// not advance automatically.
        /// </summary>
        public RealNumber? Dur => Get<RealNumber>(Constants.DictionaryKeys.Page.Dur);

        /// <summary>
        /// (Optional; PDF 1.1) A transition dictionary describing the transition effect that shall be used when 
        /// displaying the page during presentations (see 12.4.4, "Presentations").
        /// </summary>
        public Dictionary? Trans => Get<Dictionary>(Constants.DictionaryKeys.Page.Trans);

        /// <summary>
        /// (Optional) An array of annotation dictionaries that shall contain indirect references to all 
        /// annotations associated with the page (see 12.5, "Annotations").
        /// </summary>
        public IndirectObjectReference? Annots => Get<IndirectObjectReference>(Constants.DictionaryKeys.Page.Annots);

        /// <summary>
        /// (Optional; PDF 1.2) An additional-actions dictionary that shall define actions to be performed 
        /// when the page is opened or closed (see 12.6.3, "Trigger events"). (PDF 1.3) additional-actions 
        /// dictionaries are not inheritable.
        /// </summary>
        public Dictionary? AA => Get<Dictionary>(Constants.DictionaryKeys.Page.AA);

        /// <summary>
        /// (Optional; PDF 1.4) A metadata stream that shall contain metadata for the page (see 14.3.2, "Metadata streams").
        /// </summary>
        public IndirectObjectReference? Metadata => Get<IndirectObjectReference>(Constants.DictionaryKeys.Page.Metadata);

        /// <summary>
        /// (Optional; PDF 1.3) A page-piece dictionary associated with the page (see 14.5, "Page-piece dictionaries").
        /// </summary>
        public Dictionary? PieceInfo => Get<Dictionary>(Constants.DictionaryKeys.Page.PieceInfo);

        /// <summary>
        /// (Required if the page contains structural content items; PDF 1.3) The integer key of the page’s entry 
        /// in the structural parent tree (see 14.7.5.4, "Finding structure elements from content items").
        /// </summary>
        public Integer? StructParents => Get<Integer>(Constants.DictionaryKeys.Page.StructParents);

        /// <summary>
        /// (Optional; PDF 1.3; indirect reference preferred) The digital identifier of the page’s parent Web Capture 
        /// content set (see 14.10.6, "Object attributes related to web capture").
        /// </summary>
        public HexadecimalString? ID => Get<HexadecimalString>(Constants.DictionaryKeys.Page.ID);

        /// <summary>
        /// (Optional; PDF 1.3) The page’s preferred zoom (magnification) factor: the factor by which it shall be 
        /// scaled to achieve the natural display magnification (see 14.10.6, "Object attributes related to web capture").
        /// </summary>
        public RealNumber? PZ => Get<RealNumber>(Constants.DictionaryKeys.Page.PZ);

        /// <summary>
        /// (Optional; PDF 1.3) A separation dictionary that shall contain information needed to generate colour separations 
        /// for the page (see 14.11.4, "Separation dictionaries").
        /// </summary>
        public Dictionary? SeparationInfo => Get<Dictionary>(Constants.DictionaryKeys.Page.SeparationInfo);

        /// <summary>
        /// (Optional; PDF 1.5) A name specifying the tab order that shall be used for annotations on the page 
        /// (see 12.5 "Annotations"). If present, the values shall be one of R (row order), C (column order), 
        /// and S (structure order). Beginning with PDF 2.0, additional values also include A (annotations array order) 
        /// and W (widget order). Annotations array order refers to the order of the annotation enumerated in the Annots 
        /// entry of the Page dictionary (see "Table 31 — Entries in a page object"). Widget order means using the same 
        /// array ordering but making two passes, the first only picking the widget annotations and the second picking 
        /// all other annotations.
        /// </summary>
        public Name? Tabs => Get<Name>(Constants.DictionaryKeys.Page.Tabs);

        /// <summary>
        /// (Required if this page was created from a named page object; PDF 1.5) The name of the originating page object 
        /// (see 12.7.7, "Named pages").
        /// </summary>
        public Name? TemplateInstantiated => Get<Name>(Constants.DictionaryKeys.Page.TemplateInstantiated);

        /// <summary>
        /// (Optional; PDF 1.5) A navigation node dictionary that shall represent the first node on the page 
        /// (see 12.4.4.2, "Sub-page navigation").
        /// </summary>
        public Dictionary? PresSteps => Get<Dictionary>(Constants.DictionaryKeys.Page.PresSteps);

        /// <summary>
        /// <para>(Optional; PDF 1.6) A positive number that shall give the size of default user space units, in multiples of 1 ⁄ 72 inch. The range of supported values shall be implementation-dependent.</para>
        /// <para>Default value: 1.0 (user space unit is 1 ⁄ 72 inch).</para>
        /// </summary>
        public RealNumber? UserUnit => Get<RealNumber>(Constants.DictionaryKeys.Page.UserUnit);

        /// <summary>
        /// (Optional; PDF 1.6) An array of viewport dictionaries (see "Table 265 — Entries in a viewport dictionary") 
        /// that shall specify rectangular regions of the page.
        /// </summary>
        public ArrayObject? VP => Get<ArrayObject>(Constants.DictionaryKeys.Page.VP);

        /// <summary>
        /// (Optional; PDF 2.0) An array of one or more file specification dictionaries 
        /// (7.11.3, "File specification dictionaries") which denote the associated files 
        /// for this page. See 14.13, "Associated files" and 14.13.8, "Associated files linked to DParts" for more details.
        /// </summary>
        public ArrayObject? AF => Get<ArrayObject>(Constants.DictionaryKeys.Page.AF);

        /// <summary>
        /// (Optional; PDF 2.0) An array of output intent dictionaries that shall specify the colour characteristics of output 
        /// devices on which this page might be rendered (see 14.11.5, "Output intents").
        /// </summary>
        public ArrayObject? OutputIntents => Get<ArrayObject>(Constants.DictionaryKeys.Page.OutputIntents);

        /// <summary>
        /// <para>(Required, if this page is within the range of a DPart, not permitted otherwise; PDF 2.0) 
        /// An indirect reference to the DPart dictionary whose range of pages includes this page object 
        /// (see 14.12.3, "Connecting the DPart tree structure to pages").</para>
        /// <para>NOTE 3 The DPart key in a page object allows a PDF processor to directly retrieve 
        /// the section of the document part hierarchy that applies to this page object. 
        /// This also allows for ready access of DPM data in PDF processors.</para>
        /// </summary>
        public Dictionary? DPart => Get<Dictionary>(Constants.DictionaryKeys.Page.DPart);

        public async Task AddXObjectResourceAsync(
            Name name,
            IndirectObjectReference reference,
            IndirectObjectManager indirectObjectManager
            )
        {
            ArgumentNullException.ThrowIfNull(name, nameof(name));
            ArgumentNullException.ThrowIfNull(reference, nameof(reference));
            ArgumentNullException.ThrowIfNull(indirectObjectManager, nameof(indirectObjectManager));

            // Resources can be null, a ResourceDictionary, or an indirect object reference to a ResourceDictionary

            var resources = Resources ?? Empty;

            if (resources is IndirectObjectReference resourceRef)
            {
                var resourcesIndirectObject = await indirectObjectManager.GetAsync(resourceRef);
                var resourceDict = resourcesIndirectObject!.Get<ResourceDictionary>();

                resourceDict.AddXObject(name, reference);
            }
            else if (resources is ResourceDictionary resourceDict)
            {
                resourceDict.AddXObject(name, reference);
            }
            else if (resources is Dictionary dict)
            {
                var editableResourceDict = new Dictionary<Name, IPdfObject>(dict)
                {
                    { name, reference }
                };

                Set(Constants.DictionaryKeys.Page.Resources, new ResourceDictionary(xObject: editableResourceDict));
            }
        }

        public void AddContent(IEnumerable<ContentStreamObject> content, IndirectObjectManager indirectObjectManager)
        {
            ArgumentNullException.ThrowIfNull(content, nameof(content));
            ArgumentNullException.ThrowIfNull(indirectObjectManager, nameof(indirectObjectManager));

            var contentStream = new ContentStream(content, filters: null, sourceDataIsCompressed: false);

            if (Contents is null)
            {
                Set(Constants.DictionaryKeys.Page.Contents, ArrayObject.Empty);
            }
            else if (Contents is IndirectObjectReference ior)
            {
                Set(Constants.DictionaryKeys.Page.Contents, new ArrayObject([ior]));
            }

            var contentObject = indirectObjectManager.Add(contentStream);

            var contents = (Contents as ArrayObject)!;

            contents.Add(contentObject.Id.Reference);
        }

        public void SetRotation(Rotation rotation)
        {
            ArgumentNullException.ThrowIfNull(rotation);

            Set<Integer>(Constants.DictionaryKeys.Page.Rotate, rotation);
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
                { Constants.DictionaryKeys.Page.Parent, parent },
                { Constants.DictionaryKeys.Page.Resources, Empty },
            };

            if (options.MediaBox is not null)
            {
                dict[Constants.DictionaryKeys.Page.MediaBox] = options.MediaBox;
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
        }
    }
}
