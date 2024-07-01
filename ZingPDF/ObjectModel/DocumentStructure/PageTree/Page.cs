using ZingPDF.ObjectModel.CommonDataStructures;
using ZingPDF.ObjectModel.ContentStreamsAndResources;
using ZingPDF.ObjectModel.Objects;
using ZingPDF.ObjectModel.Objects.IndirectObjects;

namespace ZingPDF.ObjectModel.DocumentStructure.PageTree
{
    /// <summary>
    /// ISO 32000-2:2020 7.7.3.3 - Page objects
    /// </summary>
    public class Page : Dictionary
    {
        internal static class DictionaryKeys
        {
            public const string Page = "Page";
            public const string Parent = "Parent";
            public const string Resources = "Resources";
            public const string MediaBox = "MediaBox";
            public const string CropBox = "CropBox";
            public const string BleedBox = "BleedBox";
            public const string TrimBox = "TrimBox";
            public const string ArtBox = "ArtBox";
            public const string Contents = "Contents";
            public const string Rotate = "Rotate";
        }

        private Page(Dictionary pageDictionary) : base(pageDictionary) { }

        /// <summary>
        /// Required.<para></para>
        /// The page tree node that is the immediate parent of this page object.
        /// Objects of Type Template shall have no Parent key.
        /// </summary>
        public IndirectObjectReference Parent => Get<IndirectObjectReference>(DictionaryKeys.Parent)!;

        /// <summary>
        /// (Required; inheritable)<para></para>
        /// A dictionary containing any resources required by the page contents (see 7.8.3, "Resource dictionaries").
        /// If the page requires no resources, the value of this entry shall be an empty dictionary.
        /// Omitting the entry entirely indicates that the resources shall be inherited from an ancestor 
        /// node in the page tree, but PDF writers should not use this method of sharing resources as 
        /// described in 7.8.3, "Resource dictionaries".
        /// </summary>
        public ResourceDictionary? Resources => Get<ResourceDictionary>(DictionaryKeys.Resources);

        /// <summary>
        /// The boundaries of the physical medium on which the page shall be displayed or printed.
        /// </summary>
        public Rectangle? MediaBox => Get<Rectangle>(DictionaryKeys.MediaBox);

        /// <summary>
        /// Defines the visible region of default user space.
        /// Contents will be clipped to this rectangle.
        /// </summary>
        public Rectangle? CropBox => Get<Rectangle>(DictionaryKeys.CropBox);

        /// <summary>
        /// Defines a clipping rectangle for output in a production environment.
        /// </summary>
        public Rectangle? BleedBox { get => Get<Rectangle>(DictionaryKeys.BleedBox); }

        /// <summary>
        /// Defines the intended dimensions of the finished page after trimming.
        /// </summary>
        public Rectangle? TrimBox { get => Get<Rectangle>(DictionaryKeys.TrimBox); }

        /// <summary>
        /// Defines the extent of the page's meaningful content (including whitespace) intended by the page's creator.
        /// </summary>
        public Rectangle? ArtBox { get => Get<Rectangle>(DictionaryKeys.ArtBox); }

        /// <summary>
        /// Describes the contents of the page.
        /// </summary>
        public ArrayObject? Contents { get => Get<ArrayObject>(DictionaryKeys.Contents); }

        /// <summary>
        /// The number of degrees by which the page shall be rotated when displayed or printed.
        /// </summary>
        public Rotation? Rotate { get => Get<Rotation>(DictionaryKeys.Rotate); }

        /// <summary>
        /// Create a blank page.
        /// </summary>
        /// <param name="parent">An <see cref="IndirectObjectReference"/> pointing to the page's parent. This shall be an <see cref="IndirectObjectReference"/> to a <see cref="PageTreeNode"/>.</param>
        /// <returns>A <see cref="Page"/> instance.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static Page CreateNew(IndirectObjectReference parent, PageCreationOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(parent);

            options ??= new PageCreationOptions();

            var dict = new Dictionary<Name, IPdfObject>
            {
                { Constants.DictionaryKeys.Type, new Name(DictionaryKeys.Page) },
                { DictionaryKeys.Parent, parent },
                { DictionaryKeys.Resources, Empty },
            };

            if (options.MediaBox is not null)
            {
                dict[DictionaryKeys.MediaBox] = options.MediaBox;
            }

            return new(dict);
        }

        /// <summary>
        /// Create a page from an existing page dictionary.
        /// </summary>
        /// <param name="pageDictionary"></param>
        /// <returns></returns>
        internal static Page FromDictionary(Dictionary pageDictionary)
        {
            ArgumentNullException.ThrowIfNull(pageDictionary);

            return new Page(pageDictionary);
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
