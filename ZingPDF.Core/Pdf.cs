using ZingPdf.Core.Drawing;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.DataStructures;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferences;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferences.CrossReferenceStreams;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Pages;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// This class is disposable. Disposing will dispose the underlying <see cref="Stream"/>.
    /// </remarks>
    public class Pdf : IDisposable
    {
        private readonly Stream _pdfInputStream;
        private readonly PdfNavigator _pdfNavigator;

        private readonly CrossReferenceGenerator _crossReferenceGenerator = new();

        private readonly List<IndirectObject> _newObjects = new();
        private readonly Dictionary<IndirectObjectId, IndirectObject> _updatedObjects = new();
        private readonly List<IndirectObjectId> _deletedObjects = new();

        /// <summary>
        /// Private constructor for creating a PDF from a content stream.
        /// </summary>
        private Pdf(Stream contentStream)
        {
            _pdfInputStream = contentStream ?? throw new ArgumentNullException(nameof(contentStream));

            _pdfNavigator = new PdfNavigator(contentStream);
        }

        /// <summary>
        /// Create a new PDF. The new document will contain a single blank page by default.
        /// </summary>
        /// <returns>A <see cref="Pdf"/> instance.</returns>
        public static Pdf Create()
        {
            var documentCatalogId = new IndirectObjectId(1, 0);
            var pageTreeNodeId = new IndirectObjectId(2, 0);
            var pageId = new IndirectObjectId(3, 0);

            var page = new IndirectObject(pageId, Page.CreateNew(pageTreeNodeId.Reference));
            var rootPageTreeNode = new IndirectObject(pageTreeNodeId, PageTreeNode.CreateNew(new[] { page.Id.Reference }));

            var documentCatalog = new IndirectObject(documentCatalogId, DocumentCatalog.CreateNew(pageTreeNodeId.Reference));

            var ms = new MemoryStream();

            new Header().WriteAsync(ms).Wait();

            documentCatalog.WriteAsync(ms).Wait();
            rootPageTreeNode.WriteAsync(ms).Wait();
            page.WriteAsync(ms).Wait();

            var xrefTable = new CrossReferenceTable(
                new[] { new CrossReferenceSection(0, new[] {
                    CrossReferenceEntry.RootFreeEntry,
                    new CrossReferenceEntry(documentCatalog.ByteOffset!.Value, 0, inUse: true, compressed : false),
                    new CrossReferenceEntry(rootPageTreeNode.ByteOffset!.Value, 0, inUse: true, compressed : false),
                    new CrossReferenceEntry(page.ByteOffset!.Value, 0, inUse: true, compressed : false),
                })
            });

            xrefTable.WriteAsync(ms).Wait();

            HexadecimalString id = Guid.NewGuid().ToString("N");

            var trailerDictionary = TrailerDictionary.CreateNew(
                size: 4,
                prev: null,
                root: documentCatalog.Id.Reference,
                encrypt: null,
                info: null,
                id: new ArrayObject(new[] { id, id })
                );

            var trailer = new Trailer(trailerDictionary, xrefTable.ByteOffset!.Value);
            trailer.WriteAsync(ms).Wait();

            ms.Position = 0;

            return new(ms);
        }

        /// <summary>
        /// Load a PDF from a stream.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> which contains the PDF data.</param>
        /// <returns>A <see cref="Pdf"/> instance.</returns>
        /// <example>
        /// <![CDATA[
        /// using var inputFileStream = new FileStream("example.pdf", FileMode.Open);
        /// 
        /// var pdf = Pdf.Load(inputFileStream);
        /// ]]>
        /// </example>
        /// <remarks>
        /// This method does not parse or validate the PDF, it simply provides a <see cref="Pdf"/> 
        /// instance linked to the provided <see cref="Stream"/>.
        /// Further operations will access the stream efficiently as required.<para></para>
        /// </remarks>
        /// <exception cref="ArgumentException"></exception>
        public static Pdf Load(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable", nameof(stream));
            }

            return new(stream);
        }

        public async Task SaveAsync(Stream outputStream, PdfSaveOptions? saveOptions = null)
        {
            if (outputStream is null) throw new ArgumentNullException(nameof(outputStream));
            if (!outputStream.CanWrite) throw new ArgumentException("Provided output stream must be writable", nameof(outputStream));

            saveOptions ??= PdfSaveOptions.Default;

            _pdfInputStream.Position = 0;

            // Copy original PDf to output.
            await _pdfInputStream.CopyToAsync(outputStream);
            await outputStream.WriteNewLineAsync();

            foreach (var entry in _newObjects.Concat(_updatedObjects.Values))
            {
                await entry.WriteAsync(outputStream);
            }

            var trailerDictionary = await _pdfNavigator.GetRootTrailerDictionaryAsync();

            var size = trailerDictionary.Size + _newObjects.Count;

            var newOrUpdatedObjects = _updatedObjects.Values.Concat(_newObjects).ToList();

            if (_pdfNavigator.UsingXrefStreams)
            {
                IndirectObjectId newObjectId = await GetFreeIndexAsync();

                var xrefStreamIndirectObject = new DummyIndirectObject(newObjectId, outputStream.Position);

                newOrUpdatedObjects.Add(xrefStreamIndirectObject);

                var xrefSections = _crossReferenceGenerator.Generate(newOrUpdatedObjects, _deletedObjects);

                // +1 because the new xref stream should be included in the count
                size++;

                var xrefStreamDict = trailerDictionary as CrossReferenceStreamDictionary
                    ?? throw new InvalidOperationException("Internal Error: {59D30CD9-D2DB-4418-B59E-033538307C68}");

                var prev = trailerDictionary.ByteOffset;

                var xrefStream = new CrossReferenceStream(
                    xrefSections,
                    null,
                    //new[] { new FlateDecodeFilter(filterParams: null) },
                    //new[] { new ASCIIHexDecodeFilter() },
                    size,
                    prev,
                    xrefStreamDict.Root,
                    xrefStreamDict.Encrypt,
                    xrefStreamDict.Info,
                    xrefStreamDict.ID
                    );

                xrefStreamIndirectObject.Children.Clear();
                xrefStreamIndirectObject.Children.Add(xrefStream);

                await xrefStreamIndirectObject.WriteAsync(outputStream);

                await new Keyword(Constants.StartXref).WriteAsync(outputStream);
                await outputStream.WriteNewLineAsync();

                await new Integer(xrefStreamIndirectObject.ByteOffset!.Value).WriteAsync(outputStream);
                await outputStream.WriteNewLineAsync();

                await new Keyword(Constants.Eof).WriteAsync(outputStream);
            }
            else
            {
                var xrefSections = _crossReferenceGenerator.Generate(newOrUpdatedObjects, _deletedObjects);

                var xrefTable = new CrossReferenceTable(xrefSections);
                await xrefTable.WriteAsync(outputStream);

                var latestTrailer = (await _pdfNavigator.GetRootTrailerAsync())!;
                var prev = latestTrailer.XrefTableByteOffset;

                var trailer = new Trailer(
                    TrailerDictionary.CreateNew(
                        size,
                        prev,
                        latestTrailer.Dictionary.Root,
                        latestTrailer.Dictionary.Encrypt,
                        latestTrailer.Dictionary.Info,
                        latestTrailer.Dictionary.ID
                        ),
                    xrefTable.ByteOffset!.Value
                    );

                await trailer.WriteAsync(outputStream);
            }

            // TODO: account for the use of features which should increase the pdf version

            // TODO: do we need to amend metadata to change PDF Producer?

            await outputStream.FlushAsync();

            _newObjects.Clear();
            _updatedObjects.Clear();
            _deletedObjects.Clear();
        }

        public async Task<int> GetPageCountAsync()
        {
            var rootPageTreeNodeIndirectObject = await _pdfNavigator.GetRootPageTreeNodeAsync();

            var rootPageTreeNode = PageTreeNode.FromDictionary((rootPageTreeNodeIndirectObject.Children.First() as Dictionary)!);

            return rootPageTreeNode.PageCount;
        }

        /// <summary>
        /// Get the <see cref="Page"/> at the specified number.<para></para>
        /// </summary>
        /// <param name="pageNumber">The page number to return. Pages start at number 1 for the first page.</param>
        /// <returns>a <see cref="Page"/> instance representing the page at the specified number.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public async Task<Page> GetPageAsync(int pageNumber)
        {
            if (pageNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber));
            }

            // TODO: check if there's a more efficient way to do this.
            var pages = await _pdfNavigator.GetPagesAsync();

            if (pageNumber > pages.Count())
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber));
            }

            return (pages.ElementAt(pageNumber - 1).Children.First() as Page)!;
        }

        // TODO: This just appends a blank page. Do we need to accept a Page object here?
        // If so, how would a user consume it, Page creation requires knowledge of the parent.
        // Or maybe we accept page creation options.
        public async Task AppendPageAsync()
        {
            var rootPageTreeNodeIndirectObject = await _pdfNavigator.GetRootPageTreeNodeAsync();

            var page = Page.CreateNew(rootPageTreeNodeIndirectObject.Id.Reference, new Page.PageCreationOptions { MediaBox = new Rectangle(new(0, 0), new(200, 200)) });

            var pageIndirectObject = await AddNewObjectAsync(page);

            var rootPageTreeNode = PageTreeNode.FromDictionary((rootPageTreeNodeIndirectObject.Children.First() as Dictionary)!);

            // TODO: For now, to simplify adding pages,
            // new pages are appended to the root page tree node.
            // Determine if there's a better way, like ensuring a balanced tree.
            rootPageTreeNode.Kids.Add(pageIndirectObject.Id.Reference);

            rootPageTreeNode.PageCount++;

            UpdateObject(rootPageTreeNodeIndirectObject);
        }

        public async Task InsertPageAsync(int pageNumber)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));

            var count = await GetPageCountAsync();

            if (pageNumber > count)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} must be less than or equal to the total number of pages. To add a page to the end of the PDF, use {nameof(AppendPageAsync)}");
            }

            // TODO: check if there's a more efficient way to do this.
            var pages = await _pdfNavigator.GetPagesAsync();

            // Find the page, find its parent, insert new page into kids property
            var pageAtNumberIndirectObject = pages.ElementAt(pageNumber - 1);
            var pageAtNumber = (pageAtNumberIndirectObject.Children.First() as Page)!;
            var parentPageTreeNodeIndirectObject = await _pdfNavigator.DereferenceIndirectObjectAsync(pageAtNumber.Parent);
            var parentPageTreeNode = (parentPageTreeNodeIndirectObject.Children.First() as PageTreeNode)!;

            var kidsIndex = parentPageTreeNode.Kids.ToList().IndexOf(pageAtNumberIndirectObject.Id.Reference);

            var page = Page.CreateNew(
                parentPageTreeNodeIndirectObject.Id.Reference,
                new Page.PageCreationOptions { MediaBox = new Rectangle(new(0, 0), new(200, 200)) }
                );

            var newPageIndirectObject = await AddNewObjectAsync(page);

            var newKids = parentPageTreeNode.Kids.ToList();
            newKids.Insert(kidsIndex, newPageIndirectObject.Id.Reference);

            parentPageTreeNode.Kids = newKids.ToArray();
            parentPageTreeNode.PageCount++;

            UpdateObject(parentPageTreeNodeIndirectObject);
        }

        public void ReplacePage(int pageNumber, Page page)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
            if (page is null) throw new ArgumentNullException(nameof(page));

            throw new NotImplementedException();
        }

        public async Task DeletePageAsync(int pageNumber)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));

            // TODO: check if there's a more efficient way to do this.
            var pages = await _pdfNavigator.GetPagesAsync();

            var pageIndirectObject = pages.ElementAt(pageNumber - 1);
            var page = (pageIndirectObject.Children.First() as Page)!;
            var parentIndirectObject = await _pdfNavigator.DereferenceIndirectObjectAsync(page.Parent);
            var parent = (parentIndirectObject.Children.First() as PageTreeNode)!;

            parent.Kids = parent.Kids.Cast<IndirectObjectReference>().Where(x => x.Id != pageIndirectObject.Id).ToArray();
            parent.PageCount--;

            DeleteObject(pageIndirectObject.Id);
            UpdateObject(new IndirectObject(parentIndirectObject.Id, parent));
        }

        public async Task SetPageRotationAsync(int pageNumber, Rotation rotation)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
            if (rotation is null) throw new ArgumentNullException(nameof(rotation));

            // TODO: check if there's a more efficient way to do this.
            var pages = await _pdfNavigator.GetPagesAsync();

            var page = pages.ElementAt(pageNumber - 1);

            (page.Children.First() as Page)!.Rotate = rotation;

            UpdateObject(page);
        }

        public void Draw(
            int pageNumber,
            IEnumerable<Drawing.Path> paths,
            IEnumerable<Text> text,
            IEnumerable<Image> imageOperations,
            CoordinateSystem coordinateSystem = CoordinateSystem.BottomUp
            )
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));

            throw new NotImplementedException();
        }

        public void CompleteForm(IDictionary<string, string> values)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, string?> GetFields()
        {
            throw new NotImplementedException();
        }

        public void AddWatermark()
        {
            throw new NotImplementedException();
        }

        public void Compress(int dpi = 144, int quality = 75)
        {
            throw new NotImplementedException();
        }

        public void Encrypt()
        {
            throw new NotImplementedException();
        }

        public void Decrypt()
        {
            throw new NotImplementedException();
        }

        public void Sign()
        {
            throw new NotImplementedException();
        }

        public void AppendPdf(Stream stream)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            ((IDisposable)_pdfInputStream).Dispose();
        }

        #region Incremental Updates

        private async Task<IndirectObject> AddNewObjectAsync(PdfObject pdfObject)
        {
            if (pdfObject is null) throw new ArgumentNullException(nameof(pdfObject));

            IndirectObjectId newObjectId = await GetFreeIndexAsync();

            var indirectObject = new IndirectObject(newObjectId, pdfObject);

            _newObjects.Add(indirectObject);

            return indirectObject;
        }

        private void UpdateObject(IndirectObject indirectObject)
        {
            if (indirectObject is null) throw new ArgumentNullException(nameof(indirectObject));

            _updatedObjects[indirectObject.Id] = indirectObject;
        }

        private void DeleteObject(IndirectObjectId indirectObjectId)
        {
            if (indirectObjectId is null) throw new ArgumentNullException(nameof(indirectObjectId));

            indirectObjectId.GenerationNumber++;

            _deletedObjects.Add(indirectObjectId);
        }

        private async Task<IndirectObjectId> GetFreeIndexAsync()
        {
            // Concatenate unsaved entries with existing objects
            var xrefs = (await _pdfNavigator.GetAggregateCrossReferencesAsync())
                .Concat(_newObjects.ToDictionary(e => e.Id.Index, e => new CrossReferenceEntry(0, 0, inUse: true, compressed: false)));

            IndirectObjectId newObjectId;
            var free = xrefs.FirstOrDefault(x => !x.Value.InUse);
            if (free.Key != 0)
            {
                newObjectId = new IndirectObjectId(free.Key, free.Value.Value2);
            }
            else
            {
                newObjectId = new IndirectObjectId(xrefs.Count() + 1, 0);
            }

            return newObjectId;
        }

        private class DummyIndirectObject : IndirectObject
        {
            public DummyIndirectObject(IndirectObjectId id, long byteOffset)
                : base(id)
            {
                ByteOffset = byteOffset;
            }
        }

        #endregion Incremental Updates
    }
}
