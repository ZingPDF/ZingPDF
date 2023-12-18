#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using Nito.AsyncEx;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferences;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferences.CrossReferenceStreams;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Pages;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;
using ZingPdf.Core.Objects.Primitives.Streams;
using ZingPdf.Core.Parsing;

namespace ZingPdf.Core.Objects
{
    internal class PdfNavigator
    {
        private readonly Dictionary<IndirectObjectId, IndirectObject> _indirectObjectCache = new();
        private readonly Stream _stream;

        private AsyncLazy<LinearizationDictionary?> _linearizationParameters;
        private AsyncLazy<Trailer?> _rootTrailer;
        private AsyncLazy<ITrailerDictionary> _rootTrailerDictionary;
        private AsyncLazy<IndirectObject> _rootPageTreeNode;
        private AsyncLazy<IEnumerable<IndirectObject>> _pages;
        private AsyncLazy<Dictionary<int, CrossReferenceEntry>> _xrefs;

        public PdfNavigator(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));

            SetupLazyProperties();
        }

        public bool UsingXrefTables { get; private set; }
        public bool UsingXrefStreams { get; private set; }

        public Task<LinearizationDictionary?> GetLinearizationDictionaryAsync() => _linearizationParameters.Task;

        /// <summary>
        /// Get the latest trailer.
        /// </summary>
        /// <remarks>
        /// A PDF which stores its cross reference information in streams will not have a trailer.<para></para>
        /// In such a file, the cross reference stream dictionary contains the trailer data.<para></para>
        /// </remarks>
        public Task<Trailer?> GetRootTrailerAsync() => _rootTrailer.Task;

        /// <summary>
        /// Get the latest trailer dictionary.
        /// </summary>
        /// <remarks>
        /// For PDFs which store their cross reference information in streams, this method will return the cross reference stream dictionary.<para></para>
        /// </remarks>
        public Task<ITrailerDictionary> GetRootTrailerDictionaryAsync() => _rootTrailerDictionary.Task;

        public Task<IEnumerable<IndirectObject>> GetPagesAsync() => _pages.Task;

        public Task<IndirectObject> GetRootPageTreeNodeAsync() => _rootPageTreeNode.Task;

        /// <summary>
        /// This class caches compute-heavy and I/O-heavy values.<para></para>
        /// Call this method to clear the cache after file changes.
        /// </summary>
        public void ClearCache()
        {
            SetupLazyProperties();
        }

        /// <summary>
        /// 
        /// </summary>
        public Task<Dictionary<int, CrossReferenceEntry>> GetAggregateCrossReferencesAsync() => _xrefs.Task;

        private void SetupLazyProperties()
        {
            _linearizationParameters = SetupLazyLinearizationDictionary();
            _rootTrailer = SetupLazyRootTrailer();
            _rootTrailerDictionary = SetupLazyRootTrailerDictionary();
            _rootPageTreeNode = SetupLazyRootPageTreeNode();
            _pages = SetupLazyPages();
            _xrefs = SetupLazyXrefs();
        }

        /// <summary>
        /// Returns the latest Indirect Object matching the given reference.
        /// </summary>
        public async Task<IndirectObject> DereferenceIndirectObjectAsync(IndirectObjectReference reference)
        {
            if (reference is null) throw new ArgumentNullException(nameof(reference));

            if (_indirectObjectCache.TryGetValue(reference.Id, out IndirectObject? indirectObject))
            {
                return indirectObject;
            }

            var xrefs = await GetAggregateCrossReferencesAsync();

            var xref = xrefs[reference.Id.Index];

            var indirectObjectParser = Parser.For<IndirectObject>();

            if (xref.Compressed)
            {
                // Just parsing the whole object stream for now.
                // I started to write code to just parse the requested object.
                // TODO: compare performance of these 2 techniques.

                var objStreamIndirectObject = await DereferenceIndirectObjectAsync(new IndirectObjectReference(new IndirectObjectId((int)xref.Value1, 0)));
                var objectStream = (objStreamIndirectObject.Children.First() as IStreamObject<IStreamDictionary>)!;
                var objectStreamDictionary = (objectStream.Dictionary as ObjectStreamDictionary)!;

                var data = await objectStream.GetDecompressedDataAsync();

                //var offsets = Encoding.ASCII.GetString(data[..objectStreamDict.First]).Split(Constants.Whitespace);

                //Dictionary<int, int> indexedOffsets = new();

                //for(var i = 0; i < objectStreamDict.N; i += 2)
                //{
                //    var objectNumber = Convert.ToInt32(offsets[i]);
                //    var byteOffset = Convert.ToInt32(offsets[i + 1]);

                //    indexedOffsets.Add(objectNumber, byteOffset);
                //}

                //var objectOffset = indexedOffsets[reference.Id.Index];

                using var ss = new SubStream(data, objectStreamDictionary.First, data.Length);
                ss.Position = 0;
                var allObjects = await Parser.For<PdfObjectGroup>().ParseAsync(ss);

                indirectObject = new IndirectObject(reference.Id, allObjects.Objects[xref.Value2]);
            }
            else
            {
                _stream.Position = xref.Value1;

                indirectObject = await indirectObjectParser.ParseAsync(_stream);
            }

            _indirectObjectCache.TryAdd(reference.Id, indirectObject);

            return indirectObject;
        }

        /// <summary>
        /// When you know the Indirect Object contains a single object of a specific type, 
        /// this method provides strongly typed access to it.
        /// </summary>
        private async Task<T> DereferenceIndirectObjectAsync<T>(IndirectObjectReference reference) where T : PdfObject
            => (T)(await DereferenceIndirectObjectAsync(reference)).Children.First();

        /// <summary>
        /// Recursively get all descendant subpages from the supplied <see cref="PageTreeNode"/>.
        /// </summary>
        private async Task<IEnumerable<IndirectObject>> GetSubPagesAsync(PageTreeNode pageTreeNode)
        {
            // TODO: check page ordering, should mimic whatever Acrobat Reader infers

            List<IndirectObject> pages = new();

            foreach (var refObj in pageTreeNode.Kids)
            {
                var ior = (IndirectObjectReference)refObj;

                var obj = await DereferenceIndirectObjectAsync(ior);

                if (obj.Children.First() is Page)
                {
                    pages.Add(obj);
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
                    && io.Children.First() is IStreamObject<IStreamDictionary> so
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
                    && io.Children.First() is IStreamObject<IStreamDictionary> so
                    && so.Dictionary is CrossReferenceStreamDictionary dict)
                {
                    dict.ByteOffset = xrefOffset;

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

                while (_stream.Position < Math.Min(1024, _stream.Length))
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
                var trailerDictionary = await GetRootTrailerDictionaryAsync();

                var documentCatalog = DocumentCatalog.FromDictionary(
                    await DereferenceIndirectObjectAsync<Dictionary>(trailerDictionary.Root));

                var xrefs = await GetAggregateCrossReferencesAsync();
                var xref = xrefs[documentCatalog.Pages.Id.Index];

                return await DereferenceIndirectObjectAsync(documentCatalog.Pages);
            });
        }

        private AsyncLazy<IEnumerable<IndirectObject>> SetupLazyPages()
        {
            return new AsyncLazy<IEnumerable<IndirectObject>>(async () =>
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
            // The offset specified after the startxref keyword will either be an xref table, or stream.
            var type = await TokenTypeIdentifier.TryIdentifyAsync(_stream)
                ?? throw new InvalidOperationException("Unable to find cross reference table or stream. PDF may be corrupt.");

            var item = await Parser.For(type).ParseAsync(_stream);

            if (item is IndirectObject io
                && io.Children.First() is IStreamObject<IStreamDictionary> streamObject
                && streamObject.Dictionary is CrossReferenceStreamDictionary)
            {
                UsingXrefStreams = true;
                await ParseCrossReferenceStreamAsync(streamObject, xrefs);
            }
            else if (item is Keyword k && k == Constants.Xref)
            {
                UsingXrefTables = true;
                await ParseCrossReferenceTableAsync(xrefs);
            }
            else
            {
                throw new InvalidOperationException("Unable to find PDF cross references.");
            }
        }

        private async Task ParseCrossReferenceStreamAsync(IStreamObject<IStreamDictionary> crossReferenceStream, Dictionary<int, CrossReferenceEntry> xrefs)
        {
            var xrefStreamDictionary = (crossReferenceStream.Dictionary as CrossReferenceStreamDictionary)!;

            // Get the indices for each subsection
            List<CrossReferenceSectionIndex> xrefIndices = new();
            if (xrefStreamDictionary.Index is null)
            {
                // Index defaults to a start index of zero, and the size for the count.
                xrefIndices.Add(new CrossReferenceSectionIndex(0, xrefStreamDictionary.Size));
            }
            else
            {
                // Index contains a pair of integers for each subsection
                // representing the start index and count
                for (var i = 0; i < xrefStreamDictionary.Index.Count(); i += 2)
                {
                    xrefIndices.Add(
                        new CrossReferenceSectionIndex(
                            xrefStreamDictionary.Index.Get<Integer>(i)!,
                            xrefStreamDictionary.Index.Get<Integer>(i + 1)!
                            )
                        );
                }
            }

            var xrefData = await (await crossReferenceStream.GetDecompressedDataAsync()).ReadToEndAsync();
            var entrySize = xrefStreamDictionary.W.Sum(x => (x as Integer)!);

            var field1Size = xrefStreamDictionary.W.Get<Integer>(0)!;
            var field2Size = xrefStreamDictionary.W.Get<Integer>(1)!;
            var field3Size = xrefStreamDictionary.W.Get<Integer>(2)!;

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

                    xrefs.TryAdd(index.StartIndex + j, new CrossReferenceEntry(field2, (ushort)field3, inUse: entryType != 0, compressed: entryType == 2));
                }
            }

            if (xrefStreamDictionary.Prev is not null)
            {
                _stream.Position = xrefStreamDictionary.Prev;

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

                    xrefs.TryAdd(i, entry);
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

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.