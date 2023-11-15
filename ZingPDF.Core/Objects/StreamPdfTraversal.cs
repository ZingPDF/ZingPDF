using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Pages;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;
using ZingPdf.Core.Parsing;

namespace ZingPdf.Core.Objects
{
    internal class StreamPdfTraversal : IPdfTraversal
    {
        private readonly Stream _stream;

        public StreamPdfTraversal(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public DocumentCatalog GetDocumentCatalog(Dictionary trailerDictionary)
        {
            throw new NotImplementedException();
        }

        public async Task<Trailer> GetLatestTrailerAsync()
        {
            await new TrailerFinder().FindAsync(_stream);

            return await Parser.For<Trailer>().ParseAsync(_stream);
        }

        public IEnumerable<Page> GetPages(Dictionary trailerDictionary)
        {
            throw new NotImplementedException();
        }

        public PageTreeNode GetPageTreeNode(IndirectObject pageTreeNodeIndirectObject)
        {
            throw new NotImplementedException();
        }

        public PageTreeNode GetRootPageTreeNode(Dictionary trailerDictionary)
        {
            throw new NotImplementedException();
        }
    }
}
