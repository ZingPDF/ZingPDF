using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.IndirectObjects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf
{
    /// <summary>
    /// The body, xref table and trailer for a document.
    /// Each incremental update adds a new PdfIncrement.
    /// </summary>
    internal class PdfIncrement : PdfObject
    {
        private readonly IndirectObjectReference _documentCatalogReference;
        private readonly IndirectObjectReference? _infoReference;
        private readonly ArrayObject? _id;

        internal PdfIncrement(
            IEnumerable<IndirectObject> body,
            CrossReferenceTable crossReferenceTable,
            IndirectObjectReference documentCatalogReference,
            IndirectObjectReference? infoReference = null,
            ArrayObject? id = null
            )
        {
            Body = body?.ToList() ?? throw new ArgumentNullException(nameof(body));
            CrossReferenceTable = crossReferenceTable ?? throw new ArgumentNullException(nameof(crossReferenceTable));

            Trailer = new Trailer(
                documentCatalogReference,
                null,
                body.Count() + 1
                );

            _documentCatalogReference = documentCatalogReference ?? throw new ArgumentNullException(nameof(documentCatalogReference));
            _infoReference = infoReference;
            _id = id;
        }

        /// <summary>
        /// A collection of <see cref="IndirectObject"/> instances which make up the increment's body.
        /// </summary>
        public List<IndirectObject> Body { get; }

        /// <summary>
        /// The increment's cross reference table.
        /// </summary>
        public CrossReferenceTable CrossReferenceTable { get; }

        /// <summary>
        /// The increment's trailer.
        /// </summary>
        public Trailer Trailer { get; private set; }

        /// <summary>
        /// Append an object to the increment.
        /// </summary>
        /// <remarks>
        /// This will add the object to the increment's body, and an entry to its cross reference table.
        /// </remarks>
        /// <param name="indirectObject"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Add(IndirectObject indirectObject)
        {
            if (indirectObject is null) throw new ArgumentNullException(nameof(indirectObject));
            
            Body.Add(indirectObject);
            CrossReferenceTable.Add(indirectObject.Id.Reference);
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            foreach (var item in Body)
            {
                await item.WriteAsync(stream);
            }

            CrossReferenceTable.UpdateByteOffsets(Body);

            await CrossReferenceTable.WriteAsync(stream);

            Trailer = new Trailer(
                _documentCatalogReference,
                CrossReferenceTable.ByteOffset!.Value,
                Body.Count + 1,
                _infoReference,
                _id
                );

            await Trailer.WriteAsync(stream);
        }
    }
}
