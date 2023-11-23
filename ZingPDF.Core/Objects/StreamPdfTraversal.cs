using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
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

        public async Task<IEnumerable<CrossReferenceEntry>> GetAggregateCrossReferencesAsync(bool linearizedPdf)
        {
            var trailer = await GetLatestTrailerAsync(linearizedPdf);
            Dictionary<int, CrossReferenceEntry> xrefs = new();

            while(true)
            {
                _stream.Position = trailer.XrefTableByteOffset;

                var xrefTable = await Parser.For<CrossReferenceTable>().ParseAsync(_stream);

                foreach(var section in xrefTable.Sections)
                {
                    var maxIndex = section.Index.StartIndex + section.Entries.Count;

                    for (var i = section.Index.StartIndex; i < maxIndex; i++)
                    {
                        var entry = section.Entries[i];

                        xrefs.TryAdd(i, entry);
                    }
                }

                if (trailer.Dictionary.Prev is not null)
                {
                    _stream.Position = trailer.Dictionary.Prev;
                    trailer = await Parser.For<Trailer>().ParseAsync(_stream);
                }
                else
                {
                    break;
                }
            }

            return xrefs.Values;
        }

        public async Task<Trailer> GetLatestTrailerAsync(bool linearizedPdf)
        {
            _ = await new TrailerFinder().FindAsync(_stream, linearizedPdf);

            return await Parser.For<Trailer>().ParseAsync(_stream);
        }

        public async Task<IEnumerable<Page>> GetPagesAsync(TrailerDictionary trailerDictionary, bool linearizedPdf)
        {
            if (trailerDictionary is null) throw new ArgumentNullException(nameof(trailerDictionary));

            var rootPageTreeNodeIndirectObject = await GetRootPageTreeNodeAsync(trailerDictionary, linearizedPdf);

            var rootPageTreeNode = PageTreeNode.FromDictionary((rootPageTreeNodeIndirectObject.Children.First() as Dictionary)!);

            return await GetSubPagesAsync(rootPageTreeNode, linearizedPdf);
        }

        public async Task<IndirectObject> GetRootPageTreeNodeAsync(TrailerDictionary trailerDictionary, bool linearizedPdf)
        {
            if (trailerDictionary is null) throw new ArgumentNullException(nameof(trailerDictionary));

            IndirectObjectDereferencer indirectObjectDereferencer = new();

            var documentCatalog = DocumentCatalog.FromDictionary(
                await indirectObjectDereferencer.GetSingleAsync<Dictionary>(_stream, linearizedPdf, trailerDictionary.Root));

            return await indirectObjectDereferencer.GetAsync(_stream, linearizedPdf, documentCatalog.Pages);
        }

        /// <summary>
        /// Recursively get all descendant subpages from the supplied <see cref="PageTreeNode"/>.
        /// </summary>
        private async Task<IEnumerable<Page>> GetSubPagesAsync(PageTreeNode pageTreeNode, bool linearizedPdf)
        {
            IndirectObjectDereferencer indirectObjectDereferencer = new();

            List<Page> pages = new();

            foreach(var ior in pageTreeNode.Kids)
            {
                var obj = await indirectObjectDereferencer.GetAsync(_stream, linearizedPdf, (IndirectObjectReference)ior);

                if (obj.Children.First() is Page page)
                {
                    pages.Add(page);
                }
                else if (obj.Children.First() is PageTreeNode ptn)
                {
                    pages.AddRange(await GetSubPagesAsync(ptn, linearizedPdf));
                }
            }

            return pages;
        }
    }
}
