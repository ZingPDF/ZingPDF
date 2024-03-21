using ZingPDF.Objects;
using ZingPDF;
using ZingPDF.Objects.DataStructures;
using ZingPDF.Objects.Primitives;
using ZingPDF.Objects.Primitives.IndirectObjects;

namespace ZingPDF.Objects.Pages
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

        private Page(IndirectObjectReference parentPageTreeNode)
            : base(new Dictionary<Name, IPdfObject>
            {
                { Constants.DictionaryKeys.Type, new Name(DictionaryKeys.Page) },
                { DictionaryKeys.Parent, parentPageTreeNode },
                { DictionaryKeys.Resources, new Dictionary() },
            })
        { }

        private Page(Dictionary pageDictionary) : base(pageDictionary) { }

        public IndirectObjectReference Parent { get => Get<IndirectObjectReference>(DictionaryKeys.Parent)!; }

        /// <summary>
        /// The boundaries of the physical medium on which the page shall be displayed or printed.
        /// </summary>
        // TODO: this is a required field, but its value can be inherited from an ancestor, check if we need to do anything to achieve this.
        public Rectangle? MediaBox { get => Get<Rectangle>(DictionaryKeys.MediaBox); set => this[DictionaryKeys.MediaBox] = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>
        /// Defines the visible region of default user space.
        /// Contents will be clipped to this rectangle.
        /// </summary>
        public Rectangle? CropBox { get => Get<Rectangle>(DictionaryKeys.CropBox); set => this[DictionaryKeys.CropBox] = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>
        /// Defines a clipping rectangle for output in a production environment.
        /// </summary>
        public Rectangle? BleedBox { get => Get<Rectangle>(DictionaryKeys.BleedBox); set => this[DictionaryKeys.BleedBox] = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>
        /// Defines the intended dimensions of the finished page after trimming.
        /// </summary>
        public Rectangle? TrimBox { get => Get<Rectangle>(DictionaryKeys.TrimBox); set => this[DictionaryKeys.TrimBox] = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>
        /// Defines the extent of the page's meaningful content (including whitespace) intended by the page's creator.
        /// </summary>
        public Rectangle? ArtBox { get => Get<Rectangle>(DictionaryKeys.ArtBox); set => this[DictionaryKeys.ArtBox] = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>
        /// Describes the contents of the page.
        /// </summary>
        public ArrayObject? Contents { get => Get<ArrayObject>(DictionaryKeys.Contents); set => this[DictionaryKeys.Contents] = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>
        /// The number of degrees by which the page shall be rotated when displayed or printed.
        /// </summary>
        public Rotation? Rotate { get => Get<Rotation>(DictionaryKeys.Rotate); set => this[DictionaryKeys.Rotate] = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>
        /// Create a blank page.
        /// </summary>
        /// <param name="parent">An <see cref="IndirectObjectReference"/> pointing to the page's parent. This shall be an <see cref="IndirectObjectReference"/> to a <see cref="PageTreeNode"/>.</param>
        /// <returns>A <see cref="Page"/> instance.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static Page CreateNew(IndirectObjectReference parent, PageCreationOptions? options = null)
        {
            if (parent is null) throw new ArgumentNullException(nameof(parent));

            options ??= new PageCreationOptions();

            var page = new Page(parent);

            if (options.MediaBox is not null)
            {
                page.MediaBox = options.MediaBox;
            }

            return page;
        }

        /// <summary>
        /// Create a page from an existing page dictionary.
        /// </summary>
        /// <param name="pageDictionary"></param>
        /// <returns></returns>
        internal static Page FromDictionary(Dictionary pageDictionary)
        {
            if (pageDictionary is null) throw new ArgumentNullException(nameof(pageDictionary));

            return new Page(pageDictionary);
        }

        internal class PageCreationOptions
        {
            /// <summary>
            /// Represents the overall size of the page.
            /// </summary>
            public Rectangle? MediaBox { get; set; }
        }
    }
}
