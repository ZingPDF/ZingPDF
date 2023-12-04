using Nito.AsyncEx;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Pages;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;
using ZingPdf.Core.Objects.Primitives.Streams;
using ZingPdf.Core.Parsing;

namespace ZingPdf.Core.Objects
{
    internal class StreamPdfTraversal : IPdfTraversal
    {
        private readonly Stream _stream;

        private readonly AsyncLazy<LinearizationDictionary?> _linearizationParameters;
        private readonly AsyncLazy<Trailer?> _rootTrailer;
        private readonly AsyncLazy<ITrailerDictionary> _rootTrailerDictionary;
        private readonly AsyncLazy<IndirectObject> _rootPageTreeNode;
        private readonly AsyncLazy<IEnumerable<Page>> _pages;
        private readonly AsyncLazy<Dictionary<int, CrossReferenceEntry>> _xrefs;

        public StreamPdfTraversal(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));

            _linearizationParameters = SetupLazyLinearizationDictionary();
            _rootTrailer = SetupLazyRootTrailer();
            _rootTrailerDictionary = SetupLazyRootTrailerDictionary();
            _rootPageTreeNode = SetupLazyRootPageTreeNode();
            _pages = SetupLazyPages();
            _xrefs = SetupLazyXrefs();
        }

        // TODO: need a way to reset lazy properties after file saving

        public Task<LinearizationDictionary?> GetLinearizationDictionaryAsync() => _linearizationParameters.Task;
        public Task<Trailer?> GetRootTrailerAsync() => _rootTrailer.Task;
        public Task<ITrailerDictionary> GetRootTrailerDictionaryAsync() => _rootTrailerDictionary.Task;
        public Task<IEnumerable<Page>> GetPagesAsync() => _pages.Task;
        public Task<IndirectObject> GetRootPageTreeNodeAsync() => _rootPageTreeNode.Task;
        public Task<Dictionary<int, CrossReferenceEntry>> GetAggregateCrossReferencesAsync() => _xrefs.Task;

        /// <summary>
        /// Recursively get all descendant subpages from the supplied <see cref="PageTreeNode"/>.
        /// </summary>
        private async Task<IEnumerable<Page>> GetSubPagesAsync(PageTreeNode pageTreeNode)
        {
            // TODO: check page ordering, should mimic whatever Acrobat Reader infers

            IndirectObjectDereferencer indirectObjectDereferencer = new();

            List<Page> pages = new();

            foreach (var ior in pageTreeNode.Kids)
            {
                var obj = await indirectObjectDereferencer.GetAsync(_stream, (IndirectObjectReference)ior);

                if (obj.Children.First() is Page page)
                {
                    pages.Add(page);
                }
                else if (obj.Children.First() is PageTreeNode ptn)
                {
                    pages.AddRange(await GetSubPagesAsync(ptn));
                }
            }

            return pages;
        }

        private AsyncLazy<Trailer?> SetupLazyRootTrailer()
        {
            return new AsyncLazy<Trailer?>(async () =>
            {
                var objectFinder = new ObjectFinder();

                // First, find the startxref keyword
                var offset = await objectFinder.FindAsync(_stream, Constants.StartXref, forwards: false)
                    ?? throw new InvalidOperationException($"{Constants.StartXref} not found.");

                _stream.Position = offset;

                _ = await Parser.For<Keyword>().ParseAsync(_stream);
                var xrefOffset = await Parser.For<Integer>().ParseAsync(_stream);

                _stream.Position = xrefOffset;

                var type = await TokenTypeIdentifier.TryIdentifyAsync(_stream);

                var item = await Parser.For(type).ParseAsync(_stream);

                if (item is IndirectObject io
                    && io.Children.First() is StreamObject so
                    && so.Dictionary is CrossReferenceStreamDictionary dict)
                {
                    return null;
                }

                if (item is Keyword k && k == Constants.Xref)
                {
                    var trailerOffset = await objectFinder.FindAsync(_stream, Constants.Trailer, forwards: false);

                    if (trailerOffset is not null)
                    {
                        _stream.Position = trailerOffset.Value;

                        var trailer = await Parser.For<Trailer>().ParseAsync(_stream);

                        return trailer;
                    }
                }

                throw new InvalidOperationException("Unable to find PDF trailer information.");
            });
        }

        private AsyncLazy<ITrailerDictionary> SetupLazyRootTrailerDictionary()
        {
            return new AsyncLazy<ITrailerDictionary>(async () =>
            {
                var trailer = await GetRootTrailerAsync();
                if (trailer is not null)
                {
                    return trailer.Dictionary;
                }

                // TODO: we're doing this startxref search twice. (in GetRootTrailerAsync too)

                var objectFinder = new ObjectFinder();

                // First, find the startxref keyword
                var offset = await objectFinder.FindAsync(_stream, Constants.StartXref, forwards: false)
                    ?? throw new InvalidOperationException($"{Constants.StartXref} not found.");

                _stream.Position = offset;

                _ = await Parser.For<Keyword>().ParseAsync(_stream);
                var xrefOffset = await Parser.For<Integer>().ParseAsync(_stream);

                _stream.Position = xrefOffset;

                var type = await TokenTypeIdentifier.TryIdentifyAsync(_stream);

                var item = await Parser.For(type).ParseAsync(_stream);

                if (item is IndirectObject io
                    && io.Children.First() is StreamObject so
                    && so.Dictionary is CrossReferenceStreamDictionary dict)
                {
                    return dict;
                }

                throw new InvalidOperationException("Unable to find PDF trailer information.");
            });
        }

        private AsyncLazy<LinearizationDictionary?> SetupLazyLinearizationDictionary()
        {
            return new AsyncLazy<LinearizationDictionary?>(async () =>
            {
                _stream.Position = 0;

                static bool isLinearizationDictionary(IndirectObject o) =>
                    o.Children.FirstOrDefault() is LinearizationDictionary;

                List<PdfObject> items = new();

                while (_stream.Position <= Math.Min(1024, _stream.Length))
                {
                    var type = await TokenTypeIdentifier.TryIdentifyAsync(_stream);

                    var item = await Parser.For(type).ParseAsync(_stream);

                    if (item is IndirectObject o && isLinearizationDictionary(o))
                    {
                        return o.Children.First()! as LinearizationDictionary;
                    }
                }

                return null;
            });
        }

        private AsyncLazy<IndirectObject> SetupLazyRootPageTreeNode()
        {
            return new AsyncLazy<IndirectObject>(async () =>
            {
                IndirectObjectDereferencer indirectObjectDereferencer = new();

                var trailerDictionary = await GetRootTrailerDictionaryAsync();

                var documentCatalog = DocumentCatalog.FromDictionary(
                    await indirectObjectDereferencer.GetSingleAsync<Dictionary>(_stream, trailerDictionary.Root));

                return await indirectObjectDereferencer.GetAsync(_stream, documentCatalog.Pages);
            });
        }

        private AsyncLazy<IEnumerable<Page>> SetupLazyPages()
        {
            return new AsyncLazy<IEnumerable<Page>>(async () =>
            {
                var rootPageTreeNodeIndirectObject = await GetRootPageTreeNodeAsync();

                var rootPageTreeNode = PageTreeNode.FromDictionary((rootPageTreeNodeIndirectObject.Children.First() as Dictionary)!);

                return await GetSubPagesAsync(rootPageTreeNode);
            });
        }

        private AsyncLazy<Dictionary<int, CrossReferenceEntry>> SetupLazyXrefs()
        {
            return new AsyncLazy<Dictionary<int, CrossReferenceEntry>>(async () =>
            {
                Dictionary<int, CrossReferenceEntry> xrefs = new();

                // To aggregate all cross references
                // - check if PDF is linearized
                // - - presence of linearization dictionary in first 1024 bytes
                // - - dictionary L value is identical to stream length
                // - search for startxref keyword
                // - - for linearized files, search from top
                // - - for non-linearized files, search from bottom
                // - following the startxref keyword is a byte offset
                // - go to offset and identify
                // - - if we find the keyword xref, it's a table
                // - - if we find an indirect object containing a stream, it's an xref stream

                var objectFinder = new ObjectFinder();

                // PDF is linearized if there is a linearization dictionary, AND
                // the length value (L) is identical to the length of the stream.
                // A mismatch indicates the file has had at least one incremental update applied,
                // and should be considered to not be linearized.
                LinearizationDictionary? linearizationDictionary = await GetLinearizationDictionaryAsync();
                var isLinearized = linearizationDictionary != null && linearizationDictionary.L == _stream.Length;

                // First, find the startxref keyword
                var offset = await objectFinder.FindAsync(_stream, Constants.StartXref, forwards: isLinearized)
                    ?? throw new InvalidOperationException($"{Constants.StartXref} not found.");

                _stream.Position = offset;
                await _stream.AdvanceBeyondNextAsync(Constants.StartXref);

                var xrefOffset = await Parser.For<Integer>().ParseAsync(_stream);

                _stream.Position = xrefOffset;

                await ParseCrossReferencesAsync(xrefs);

                return xrefs;
            });
        }

        private async Task ParseCrossReferencesAsync(Dictionary<int, CrossReferenceEntry> xrefs)
        {
            //Console.WriteLine($"Executing ParseCrossReferencesAsync. Current xrefs count: {xrefs.Count}");

            // The offset specified after the startxref keyword will either be an xref table, or stream.
            var type = await TokenTypeIdentifier.TryIdentifyAsync(_stream)
                ?? throw new InvalidOperationException("Unable to find cross reference table or stream. PDF may be corrupt.");

            var item = await Parser.For(type).ParseAsync(_stream);

            if (item is IndirectObject io
                && io.Children.First() is StreamObject streamObject
                && streamObject.Dictionary is CrossReferenceStreamDictionary)
            {
                await ParseCrossReferenceStreamAsync(streamObject, xrefs);
            }
            else if (item is Keyword k && k == Constants.Xref)
            {
                await ParseCrossReferenceTableAsync(xrefs);
            }
            else
            {
                throw new InvalidOperationException("Unable to find PDF cross references.");
            }
        }

        private async Task ParseCrossReferenceStreamAsync(StreamObject crossReferenceStream, Dictionary<int, CrossReferenceEntry> xrefs)
        {
            var streamDictionary = (crossReferenceStream.Dictionary as CrossReferenceStreamDictionary)!;

            // Get the indices for each subsection
            List<CrossReferenceSectionIndex> xrefIndices = new();
            if (streamDictionary.Index is null)
            {
                // Index defaults to a start index of zero, and the size for the count.
                xrefIndices.Add(new CrossReferenceSectionIndex(0, streamDictionary.Size));
            }
            else
            {
                // Index contains a pair of integers for each subsection
                // representing the start index and count
                for (var i = 0; i < streamDictionary.Index.Count(); i += 2)
                {
                    xrefIndices.Add(
                        new CrossReferenceSectionIndex(
                            streamDictionary.Index.Get<Integer>(i)!,
                            streamDictionary.Index.Get<Integer>(i + 1)!
                            )
                        );
                }
            }

            var xrefData = await crossReferenceStream.DecodeAsync();
            var entrySize = streamDictionary.W.Sum(x => (x as Integer)!);

            var field1Size = streamDictionary.W.Get<Integer>(0)!;
            var field2Size = streamDictionary.W.Get<Integer>(1)!;
            var field3Size = streamDictionary.W.Get<Integer>(2)!;

            for (int i = 0; i < xrefIndices.Count; i++)
            {
                CrossReferenceSectionIndex? index = xrefIndices[i];

                var sectionOffset = index.StartIndex * entrySize;

                for (var j = 0; j < index.Count; j++)
                {
                    var entryOffset = (i + j) * entrySize;
                    var entryData = xrefData[entryOffset..(entryOffset + entrySize)];

                    // Default entry type is 1 ('in use' object)
                    var entryType = (byte)1;

                    if (field1Size != 0)
                    {
                        entryType = entryData[0];
                    }

                    int field2 = ExtractField(entryData, field1Size, field2Size);
                    int field3 = ExtractField(entryData, field1Size + field2Size, field3Size);

                    xrefs[index.StartIndex + j] = new CrossReferenceEntry(field2, (ushort)field3, inUse: entryType != 0, compressed: entryType == 2);
                }
            }

            if (streamDictionary.Prev is not null)
            {
                _stream.Position = streamDictionary.Prev;

                await ParseCrossReferencesAsync(xrefs);
            }
        }

        private async Task ParseCrossReferenceTableAsync(Dictionary<int, CrossReferenceEntry> xrefs)
        {
            var xrefTable = await Parser.For<CrossReferenceTable>().ParseAsync(_stream);

            foreach (var section in xrefTable.Sections)
            {
                var maxIndex = section.Index.StartIndex + section.Entries.Count;

                for (var i = section.Index.StartIndex; i < maxIndex; i++)
                {
                    var entry = section.Entries[i];

                    xrefs[i] = entry;
                }
            }

            var trailer = await Parser.For<Trailer>().ParseAsync(_stream);

            if (trailer.Dictionary.Prev is not null)
            {
                _stream.Position = trailer.Dictionary.Prev;

                await ParseCrossReferencesAsync(xrefs);
            }
        }

        /// <summary>
        /// Function to extract multi-byte fields
        /// </summary>
        /// <remarks>
        /// The field is stored in big-endian order, where the most significant byte is at the lowest memory address.
        /// The function iterates over each byte in the field, masks out the lower 8 bits,
        /// and left-shifts the value by an appropriate amount based on its position in the field.
        /// The result is accumulated to reconstruct the final field value.
        /// </remarks>
        private static int ExtractField(byte[] data, int startIndex, int fieldSize)
        {
            int fieldValue = 0;

            for (var i = 0; i < fieldSize; i++)
            {
                fieldValue += (data[i + startIndex] & 0x00FF) << ((fieldSize - i - 1) * 8);
            }

            return fieldValue;
        }
    }
}
