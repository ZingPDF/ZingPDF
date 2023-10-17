using ZingPdf.Core.Objects.IndirectObjects;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects
{
    internal class PageTreeNode : PdfObject
    {
        private readonly IndirectObjectReference[] _pages;

        public PageTreeNode(IndirectObjectReference[] pages)
        {
            _pages = pages ?? throw new ArgumentNullException(nameof(pages));
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await new Dictionary(new Dictionary<Name, PdfObject>
            {
                { "Type", new Name("Pages") },
                { "Kids", new Primitives.Array(_pages) },
                { "Count", new Integer(_pages.Length) },
            }).WriteAsync(stream);
        }
    }
}
