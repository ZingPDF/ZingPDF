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
        public ResourceDictionary? Resources => Get<ResourceDictionary>(Constants.DictionaryKeys.Page.Resources);

        /// <summary>
        /// The boundaries of the physical medium on which the page shall be displayed or printed.
        /// </summary>
        public Rectangle? MediaBox => Get<Rectangle>(Constants.DictionaryKeys.Page.MediaBox);

        /// <summary>
        /// Defines the visible region of default user space.
        /// Contents will be clipped to this rectangle.
        /// </summary>
        public Rectangle? CropBox => Get<Rectangle>(Constants.DictionaryKeys.Page.CropBox);

        /// <summary>
        /// Defines a clipping rectangle for output in a production environment.
        /// </summary>
        public Rectangle? BleedBox { get => Get<Rectangle>(Constants.DictionaryKeys.Page.BleedBox); }

        /// <summary>
        /// Defines the intended dimensions of the finished page after trimming.
        /// </summary>
        public Rectangle? TrimBox { get => Get<Rectangle>(Constants.DictionaryKeys.Page.TrimBox); }

        /// <summary>
        /// Defines the extent of the page's meaningful content (including whitespace) intended by the page's creator.
        /// </summary>
        public Rectangle? ArtBox { get => Get<Rectangle>(Constants.DictionaryKeys.Page.ArtBox); }

        /// <summary>
        /// Describes the contents of the page.
        /// </summary>
        public ArrayObject? Contents { get => Get<ArrayObject>(Constants.DictionaryKeys.Page.Contents); }

        /// <summary>
        /// The number of degrees by which the page shall be rotated when displayed or printed.
        /// </summary>
        public Rotation? Rotate { get => Get<Rotation>(Constants.DictionaryKeys.Page.Rotate); }

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
