using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.Parsing
{
    internal class IndirectObjectDereferencer
    {
        /// <summary>
        /// Returns the latest Indirect Object matching the given reference.
        /// </summary>
        public async Task<IndirectObject> GetAsync(Stream stream, bool linearizedPdf, IndirectObjectReference reference)
        {
            var pdfTraversal = new StreamPdfTraversal(stream);
            var xrefs = await pdfTraversal.GetAggregateCrossReferencesAsync(linearizedPdf);
            var offset = xrefs.ElementAt(reference.Id.Index).IndirectObjectByteOffset;

            stream.Position = offset;

            return await Parser.For<IndirectObject>().ParseAsync(stream);
        }

        /// <summary>
        /// When you know the Indirect Object contains a single object of a specific type, 
        /// this method provides strongly typed access to it.
        /// </summary>
        public async Task<T> GetSingleAsync<T>(Stream stream, bool linearizedPdf, IndirectObjectReference reference) where T : PdfObject
            => (T)(await GetAsync(stream, linearizedPdf, reference)).Children.First();

        public async IAsyncEnumerable<IndirectObject> GetAllAsync(Stream stream, bool linearizedPdf)
        {
            var pdfTraversal = new StreamPdfTraversal(stream);
            var xrefs = await pdfTraversal.GetAggregateCrossReferencesAsync(linearizedPdf);

            foreach (var record in xrefs)
            {
                stream.Position = record.IndirectObjectByteOffset;

                yield return await Parser.For<IndirectObject>().ParseAsync(stream);
            }
        }
    }
}
