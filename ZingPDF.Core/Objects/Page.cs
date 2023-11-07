using ZingPdf.Core.Objects.DataStructures;
using ZingPdf.Core.Objects.IndirectObjects;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects
{
    /// <summary>
    /// ISO 32000-2:2020 7.7.3.3 - Page objects
    /// </summary>
    public class Page : PdfObject
    {
        private readonly IndirectObjectReference _parentPageTreeNode;
        private readonly Dictionary _resourceDictionary = new();

        public Page(IndirectObjectReference parentPageTreeNode)
        {
            _parentPageTreeNode = parentPageTreeNode ?? throw new ArgumentNullException(nameof(parentPageTreeNode));
        }

        /// <summary>
        /// The boundaries of the physical medium on which the page shall be displayed or printed.
        /// </summary>
        public Rectangle MediaBox { get; set; } = new(new(0, 0), new(200, 200)); // TODO: should inherit if applicable rather than impose a default

        /// <summary>
        /// Defines the visible region of default user space.
        /// Contents will be clipped to this rectangle.
        /// </summary>
        public Rectangle? CropBox { get; set; } = null; // TODO: should inherit, or use MediaBox if null and no inherited value

        /// <summary>
        /// Defines a clipping rectangle for output in a production environment.
        /// </summary>
        public Rectangle? BleedBox { get; set; } = null; // TODO: should use CropBox if null

        /// <summary>
        /// Defines the intended dimensions of the finished page after trimming.
        /// </summary>
        public Rectangle? TrimBox { get; set; } = null; // TODO: should use CropBox if null

        /// <summary>
        /// Defines the extent of the page's meaningful content (including whitespace) intended by the page's creator.
        /// </summary>
        public Rectangle? ArtBox { get; set; } = null; // TODO: 

        /// <summary>
        /// Describes the contents of the page.
        /// </summary>
        public ArrayObject? Contents { get; set; } = null;

        /// <summary>
        /// The number of degrees by which the page shall be rotated when displayed or printed.
        /// </summary>
        public Rotation? Rotation { get; set; } = null;

        protected override async Task WriteOutputAsync(Stream stream)
        {
            var pageDictionary = new Dictionary<Name, PdfObject>
            {
                { "Type", new Name("Page") },
                { "Parent", _parentPageTreeNode },
                { "Resources", _resourceDictionary },
                { "MediaBox", MediaBox },
            };

            if (CropBox != null)
            {
                pageDictionary.Add("CropBox", CropBox);
            }

            if (BleedBox != null)
            {
                pageDictionary.Add("BleedBox", BleedBox);
            }

            if (TrimBox != null)
            {
                pageDictionary.Add("TrimBox", TrimBox);
            }

            if (ArtBox != null)
            {
                pageDictionary.Add("ArtBox", ArtBox);
            }

            if (Contents != null)
            {
                pageDictionary.Add("Contents", Contents);
            }

            if (Rotation != null)
            {
                pageDictionary.Add("Rotate", Rotation);
            }

            await new Dictionary(pageDictionary).WriteAsync(stream);
        }
    }
}
