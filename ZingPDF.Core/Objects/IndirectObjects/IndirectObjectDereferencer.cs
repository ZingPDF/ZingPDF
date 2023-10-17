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

        public async Task<IndirectObject> GetAsync(Stream stream, IndirectObjectReference reference)
        {
            var offset = _xrefTable.IndirectObjectLocations[reference.Id.Index];

            stream.Position = offset;

            return await Parser.For<IndirectObject>().ParseAsync(stream);
        }

        public async Task<T> GetSingle<T>(Stream stream, IndirectObjectReference reference) where T : PdfObject
            => (T)(await GetAsync(stream, reference)).Children.First();
    }
}
