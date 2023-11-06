using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Parsing;

namespace ZingPdf.Core.Objects.IndirectObjects
{
    internal class IndirectObjectDereferencer
    {
        private readonly CrossReferenceTable _xrefTable;

        public IndirectObjectDereferencer(CrossReferenceTable xrefTable)
        {
            _xrefTable = xrefTable ?? throw new ArgumentNullException(nameof(xrefTable));
        }

        /// <summary>
        /// Returns the latest Indirect Object matching the given reference.
        /// </summary>
        public async Task<IndirectObject> GetAsync(Stream stream, IndirectObjectReference reference)
        {
            var offset = _xrefTable.IndirectObjectLocations.Last(kvp => kvp.Key == reference.Id.Index).Value;

            stream.Position = offset;

            return await Parser.For<IndirectObject>().ParseAsync(stream);
        }

        /// <summary>
        /// When you know the Indirect Object contains a single object of a specific type, 
        /// this method provides strongly typed access to it.
        /// </summary>
        public async Task<T> GetSingleAsync<T>(Stream stream, IndirectObjectReference reference) where T : PdfObject
            => (T)(await GetAsync(stream, reference)).Children.First();

        public async IAsyncEnumerable<IndirectObject> GetAllAsync(Stream stream)
        {
            foreach (var record in _xrefTable.IndirectObjectLocations.Skip(1))
            {
                stream.Position = record.Value;

                yield return await Parser.For<IndirectObject>().ParseAsync(stream);
            }
        }
    }
}
