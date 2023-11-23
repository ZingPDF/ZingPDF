using ZingPdf.Core.Drawing;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.DataStructures;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Pages;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;
using ZingPdf.Core.Parsing;

namespace ZingPdf.Core
{
    public class Pdf
    {
        private static readonly double _defaultPdfVersion = 2;

        private readonly IncrementalUpdateManager _incrementalUpdateManager = new();
        private readonly Stream _stream;
        private readonly LinearizationParameters? _linearizationParameters;
        private readonly double _pdfVersion;

        private Pdf(Stream stream, double? pdfVersion = null, LinearizationParameters? linearizationParameters = null)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _pdfVersion = pdfVersion ?? _defaultPdfVersion;
            _linearizationParameters = linearizationParameters;
        }

        /// <summary>
        /// Indicates whether the PDF is linearized.
        /// </summary>
        public bool Linearized { get => _linearizationParameters is not null; }

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
                    new CrossReferenceEntry(0, 65535, inUse: false),
                    new CrossReferenceEntry(documentCatalog.ByteOffset!.Value, 0, inUse: true),
                    new CrossReferenceEntry(rootPageTreeNode.ByteOffset!.Value, 0, inUse: true),
                    new CrossReferenceEntry(page.ByteOffset!.Value, 0, inUse: true),
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

            new Trailer(trailerDictionary, xrefTable.ByteOffset!.Value).WriteAsync(ms).Wait();

            ms.Position = 0;

            return new(ms);
        }

        public static async Task<Pdf> LoadAsync(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable", nameof(stream));
            }

            stream.Position = 0;

            // Read the first 1024 bytes to determine if the PDF is linearized.
            using var tempStream = await stream.RangeAsync(0, 1024);

            var pdfObjectGroup = await Parser.For<PdfObjectGroup>().ParseAsync(tempStream);

            var header = pdfObjectGroup.Get<Header>(0);

            static bool isLinearizationDictionary(IndirectObject o) =>
                o.Children.FirstOrDefault() is LinearizationParameters;

            var linearizationDictionaryIndirectObject = pdfObjectGroup.Objects
                .OfType<IndirectObject>()
                .FirstOrDefault(isLinearizationDictionary);

            var linearizationDictionary = linearizationDictionaryIndirectObject?.Children.First()! as LinearizationParameters;

            //if ((linearizationDictionary?.L ?? -1) != stream.Length)
            //{
            //    // TODO: Figure out how to deal with invalid length
            //    linearizationDictionary = null;
            //}

            // TODO: header version can be overridden

            return new(stream, header.PdfVersion, linearizationDictionary);
        }

        public async Task SaveAsync(Stream outputStream, PdfSaveOptions? saveOptions = null)
        {
            if (!outputStream.CanWrite)
            {
                throw new ArgumentException("Provided stream must be writable", nameof(outputStream));
            }

            saveOptions ??= PdfSaveOptions.Default;

            _stream.Position = 0;

            // Copy original PDf to output.
            await _stream.CopyToAsync(outputStream);
            await outputStream.WriteNewLineAsync();

            await _incrementalUpdateManager.SaveAsync(Linearized, _stream, outputStream);
        }

        public async Task<int> GetPageCountAsync()
        {
            var pdfTraversal = new StreamPdfTraversal(_stream);

            var trailer = await pdfTraversal.GetLatestTrailerAsync(Linearized);
            var rootPageTreeNodeIndirectObject = await pdfTraversal.GetRootPageTreeNodeAsync(trailer.Dictionary, Linearized);

            var rootPageTreeNode = PageTreeNode.FromDictionary((rootPageTreeNodeIndirectObject.Children.First() as Dictionary)!);

            return rootPageTreeNode.PageCount;
        }

        public async Task<Page> GetPageAsync(int pageNumber)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));

            var pdfTraversal = new StreamPdfTraversal(_stream);

            var trailer = await pdfTraversal.GetLatestTrailerAsync(Linearized);

            var pages = await pdfTraversal.GetPagesAsync(trailer.Dictionary, Linearized);

            if (pageNumber > pages.Count()) throw new ArgumentOutOfRangeException(nameof(pageNumber));

            // TODO: make sure the page has the required 'MediaBox' property, whether inherited or explicitly specified.

            return pages.ElementAt(pageNumber - 1);
        }

        public async Task AppendPageAsync()
        {
            var pdfTraversal = new StreamPdfTraversal(_stream);

            var trailer = await pdfTraversal.GetLatestTrailerAsync(Linearized);
            var rootPageTreeNodeIndirectObject = await pdfTraversal.GetRootPageTreeNodeAsync(trailer.Dictionary, Linearized);

            var page = Page.CreateNew(rootPageTreeNodeIndirectObject.Id.Reference);

            var pageIndirectObject = await _incrementalUpdateManager.AddNewObjectAsync(page, Linearized, _stream);

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

        public void DeletePage(int pageNumber)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));

            throw new NotImplementedException();
        }

        public void SetPageRotation(int pageNumber, Rotation rotation)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
            if (rotation is null) throw new ArgumentNullException(nameof(rotation));

            throw new NotImplementedException();
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
    }
}
