using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
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

        public async Task<IEnumerable<CrossReferenceEntry>> GetAggregateCrossReferencesAsync()
        {
            var trailer = await GetLatestTrailerAsync();
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

        public async Task<Trailer> GetLatestTrailerAsync()
        {
            _ = await new TrailerFinder().FindAsync(_stream);

            return await Parser.For<Trailer>().ParseAsync(_stream);
        }

        public IEnumerable<Page> GetPages(TrailerDictionary trailerDictionary)
        {
            throw new NotImplementedException();
        }

        public async Task<IndirectObject> GetRootPageTreeNodeAsync(TrailerDictionary trailerDictionary)
        {
            if (trailerDictionary is null) throw new ArgumentNullException(nameof(trailerDictionary));

            IndirectObjectDereferencer indirectObjectDereferencer = new();

            var documentCatalog = DocumentCatalog.FromDictionary(
                await indirectObjectDereferencer.GetSingleAsync<Dictionary>(_stream, trailerDictionary.Root));

            return await indirectObjectDereferencer.GetAsync(_stream, documentCatalog.Pages);
        }
    }
}
