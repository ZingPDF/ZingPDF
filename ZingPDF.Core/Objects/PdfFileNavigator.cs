#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using Nito.AsyncEx;
using System.Text;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Logging;
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
    /// <summary>
    /// This class provides access to elements within a PDF file.
    /// </summary>
    internal class PdfFileNavigator : IPdfNavigator
    {
        private readonly Dictionary<IndirectObjectId, IndirectObject> _indirectObjectCache = [];
        private readonly Stream _stream;

        /// <summary>
        /// Contains the object at the offset specified by startxref.
        /// It could be a cross-reference stream (indirect object containing a dictionary and stream)
        /// or an xref keyword followed by the cross reference table.
        /// </summary>
        private AsyncLazy<IPdfObject> _xrefObject;

        private AsyncLazy<LinearizationDictionary?> _linearizationParameters;
        private AsyncLazy<Trailer?> _rootTrailer;
        private AsyncLazy<ITrailerDictionary> _rootTrailerDictionary;
        private AsyncLazy<IndirectObject> _rootPageTreeNode;
        private AsyncLazy<IEnumerable<IndirectObject>> _pages;
        private AsyncLazy<Dictionary<int, CrossReferenceEntry>> _xrefs;

        public PdfFileNavigator(Stream stream)
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
        /// 
        /// </summary>
        public Task<Dictionary<int, CrossReferenceEntry>> GetAggregateCrossReferencesAsync() => _xrefs.Task;

        private void SetupLazyProperties()
        {
            _xrefObject = SetupLazyXrefObject();

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
            ArgumentNullException.ThrowIfNull(reference);

            var indirectObject = await GetOrAddAsync(reference, async () =>
            {
                var xrefs = await GetAggregateCrossReferencesAsync();

                var xref = xrefs[reference.Id.Index];

                var indirectObjectParser = Parser.For<IndirectObject>();

                if (xref.Compressed)
                {
                    Logger.Log(LogLevel.Trace, $"{reference} is compressed within object stream {xref.Value1}");

                    // TODO: must support the `Extends` property

                    var objStreamIndirectObject = await DereferenceIndirectObjectAsync(new IndirectObjectReference(new IndirectObjectId((int)xref.Value1, 0)));
                    var objectStream = (objStreamIndirectObject.Children.First() as IStreamObject<IStreamDictionary>)!;
                    var objectStreamDictionary = (objectStream.Dictionary as ObjectStreamDictionary)!;

                    // TODO: cache decompressed stream data?
                    // Decompress stream, read bytes up to first object.
                    // These bytes contain pairs of integers, identifying each object number and byte offset.          
                    Stream decompressedObjectStream = await objectStream.GetDecompressedDataAsync();
                    var decompressedData = new byte[objectStreamDictionary.First];
                    await decompressedObjectStream.ReadExactlyAsync(decompressedData, 0, objectStreamDictionary.First);

                    // Decode integer pairs
                    var offsets = Encoding.ASCII.GetString(decompressedData).Split([Constants.Whitespace, ..Constants.EndOfLineCharacters]);

                    var indexedOffsets = new int[objectStreamDictionary.N];

                    for (var i = 0; i < objectStreamDictionary.N; i++)
                    {
                        var byteOffset = Convert.ToInt32(offsets[i * 2 + 1]);

                        indexedOffsets[i] = byteOffset;
                    }

                    var objectOffset = indexedOffsets[xref.Value2];

                    // The byte offset of an object is relative to the first object.
                    decompressedObjectStream.Position = objectStreamDictionary.First + objectOffset;

                    var type = (await TokenTypeIdentifier.TryIdentifyAsync(decompressedObjectStream))!;

                    return new IndirectObject(reference.Id, await Parser.For(type).ParseAsync(decompressedObjectStream));
                }
                else
                {
                    _stream.Position = xref.Value1;

                    return await indirectObjectParser.ParseAsync(_stream);
                }

            });

            return indirectObject;
        }

        private async Task<IndirectObject> GetOrAddAsync(
            IndirectObjectReference reference,
            Func<Task<IndirectObject>> ioRetreiver
            )
        {
            if (_indirectObjectCache.TryGetValue(reference.Id, out IndirectObject? indirectObject))
            {
                Logger.Log(LogLevel.Trace, $"{reference} returned from cache");

                return indirectObject;
            }

            Logger.Log(LogLevel.Trace, $"Cache miss: {reference}");

            indirectObject = await ioRetreiver();

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

            List<IndirectObject> pages = [];

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
                Logger.Log(LogLevel.Trace, $"Searching for root trailer");

                var xrefObject = await _xrefObject;

                if (xrefObject is IndirectObject io
                    && io.Children.First() is IStreamObject<IStreamDictionary> so
                    && so.Dictionary is CrossReferenceStreamDictionary dict)
                {
                    Logger.Log(LogLevel.Trace, $"Cross reference stream found instead of trailer");

                    return null;
                }

                if (xrefObject is Keyword k && k == Constants.Xref)
                {
                    var objectFinder = new ObjectFinder();

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

        private AsyncLazy<IPdfObject> SetupLazyXrefObject()
        {
            return new AsyncLazy<IPdfObject>(async () =>
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

                return item;
            });
        }

        private AsyncLazy<ITrailerDictionary> SetupLazyRootTrailerDictionary()
        {
            return new AsyncLazy<ITrailerDictionary>(async () =>
            {
                Logger.Log(LogLevel.Trace, $"Searching for root trailer dictionary");

                var trailer = await GetRootTrailerAsync();
                if (trailer is not null)
                {
                    Logger.Log(LogLevel.Trace, $"Found trailer, returning dictionary");

                    return trailer.Dictionary;
                }

                var xrefObject = await _xrefObject;

                if (xrefObject is IndirectObject io
                    && io.Children.First() is IStreamObject<IStreamDictionary> so
                    && so.Dictionary is CrossReferenceStreamDictionary dict)
                {
                    Logger.Log(LogLevel.Trace, $"Found cross reference stream dictionary");

                    // TODO: when this breaks, uncomment code and add a descriptive comment
                    //dict.ByteOffset = xrefOffset;

                    return dict;
                }

                throw new InvalidOperationException("Unable to find PDF trailer information.");
            });
        }

        private AsyncLazy<LinearizationDictionary?> SetupLazyLinearizationDictionary()
        {
            return new AsyncLazy<LinearizationDictionary?>(async () =>
            {
                Logger.Log(LogLevel.Trace, $"Searching for linearisation dictionary");

                _stream.Position = 0;

                static bool isLinearizationDictionary(IndirectObject o) =>
                    o.Children.FirstOrDefault() is LinearizationDictionary;

                List<PdfObject> items = [];

                var limit = Math.Min(1024, _stream.Length);

                while (_stream.Position < limit)
                {
                    var type = await TokenTypeIdentifier.TryIdentifyAsync(_stream);
                    if (type is null)
                    {
                        // TODO: is this a valid scenario?
                        break;
                    }

                    var item = await Parser.For(type).ParseAsync(_stream);

                    if (item is IndirectObject o && isLinearizationDictionary(o))
                    {
                        Logger.Log(LogLevel.Trace, $"Found linearisation dictionary");

                        return o.Children.First()! as LinearizationDictionary;
                    }
                }

                Logger.Log(LogLevel.Trace, $"No linearisation dictionary found");

                return null;
            });
        }

        private AsyncLazy<IndirectObject> SetupLazyRootPageTreeNode()
        {
            return new AsyncLazy<IndirectObject>(async () =>
            {
                Logger.Log(LogLevel.Trace, $"Searching for root page tree node");

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
                Logger.Log(LogLevel.Trace, $"Aggregating cross references");

                Dictionary<int, CrossReferenceEntry> xrefs = [];

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

                if (linearizationDictionary != null && !isLinearized)
                {
                    Logger.Log(LogLevel.Trace, "Treating file as non-linearised, as it has been updated since linearisation.");
                }

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