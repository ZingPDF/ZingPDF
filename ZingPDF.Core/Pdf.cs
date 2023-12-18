using ZingPdf.Core.Drawing;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.DataStructures;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferences;
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
        private readonly Stream _pdfContentStream;
        private readonly PdfNavigator _pdfNavigator;

        private readonly IncrementalUpdateManager _incrementalUpdateManager;

        /// <summary>
        /// Private constructor for creating a PDF from a content stream.
        /// </summary>
        private Pdf(Stream contentStream)
        {
            _pdfContentStream = contentStream ?? throw new ArgumentNullException(nameof(contentStream));

            _pdfNavigator = new PdfNavigator(contentStream);
            _incrementalUpdateManager = new IncrementalUpdateManager(_pdfNavigator);
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

            var id = HexadecimalString.FromHexStringValue(Guid.NewGuid().ToString("N"));

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
            if (!outputStream.CanWrite)
            {
                throw new ArgumentException("Provided stream must be writable", nameof(outputStream));
            }

            saveOptions ??= PdfSaveOptions.Default;

            _pdfContentStream.Position = 0;

            // Copy original PDf to output.
            await _pdfContentStream.CopyToAsync(outputStream);
            await outputStream.WriteNewLineAsync();

            await _incrementalUpdateManager.SaveAsync(_pdfContentStream, outputStream);
        }

        public async Task<int> GetPageCountAsync()
        {
            var rootPageTreeNodeIndirectObject = await _pdfNavigator.GetRootPageTreeNodeAsync();

            var rootPageTreeNode = PageTreeNode.FromDictionary((rootPageTreeNodeIndirectObject.Children.First() as Dictionary)!);

            return rootPageTreeNode.PageCount;
        }

        public async Task<Page> GetPageAsync(int pageNumber)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));

            // TODO: check if there's a more efficient way to do this.
            var pages = await _pdfNavigator.GetPagesAsync();

            if (pageNumber > pages.Count()) throw new ArgumentOutOfRangeException(nameof(pageNumber));

            // TODO: make sure the page has the required 'MediaBox' property, whether inherited or explicitly specified.

            return (pages.ElementAt(pageNumber - 1).Children.First() as Page)!;
        }

        public async Task AppendPageAsync()
        {
            var rootPageTreeNodeIndirectObject = await _pdfNavigator.GetRootPageTreeNodeAsync();

            var page = Page.CreateNew(rootPageTreeNodeIndirectObject.Id.Reference, new Page.PageCreationOptions { MediaBox = new Rectangle(new(0, 0), new(200, 200)) });

            var pageIndirectObject = await _incrementalUpdateManager.AddNewObjectAsync(page);

            var rootPageTreeNode = PageTreeNode.FromDictionary((rootPageTreeNodeIndirectObject.Children.First() as Dictionary)!);

            // TODO: For now, to simplify adding pages,
            // new pages are appended to the root page tree node.
            // Determine if there's a better way, like ensuring a balanced tree.
            rootPageTreeNode.Kids.Add(pageIndirectObject.Id.Reference);

            rootPageTreeNode.PageCount++;

            _incrementalUpdateManager.UpdateObject(rootPageTreeNodeIndirectObject);
        }

        public void InsertPage(int pageNumber)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));

            throw new NotImplementedException();
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

            _incrementalUpdateManager.DeleteObject(pageIndirectObject.Id);
            _incrementalUpdateManager.UpdateObject(new IndirectObject(parentIndirectObject.Id, parent));
        }

        public async Task SetPageRotationAsync(int pageNumber, Rotation rotation)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
            if (rotation is null) throw new ArgumentNullException(nameof(rotation));

            // TODO: check if there's a more efficient way to do this.
            var pages = await _pdfNavigator.GetPagesAsync();

            var page = pages.ElementAt(pageNumber - 1);

            (page.Children.First() as Page)!.Rotate = rotation;

            _incrementalUpdateManager.UpdateObject(page);
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
            ((IDisposable)_pdfContentStream).Dispose();
        }
    }
}
