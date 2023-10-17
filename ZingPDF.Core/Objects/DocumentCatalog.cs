using ZingPdf.Core.Objects.IndirectObjects;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects
{
    /// <summary>
    /// PDF 32000-1:2008 7.7.2
    /// </summary>
    internal class DocumentCatalog : PdfObject
    {
        private readonly IndirectObjectReference _pageTreeNode;

        public DocumentCatalog(IndirectObjectReference pageTreeNode)
        {
            _pageTreeNode = pageTreeNode ?? throw new ArgumentNullException(nameof(pageTreeNode));
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            var catalogDictionary = new Dictionary<Name, PdfObject>()
            {
                { "Type", new Name("Catalog") },
                { "Pages", _pageTreeNode },
            };

            await new Dictionary(catalogDictionary).WriteAsync(stream);
        }
    }
}
