using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects
{
    internal class IndirectObjectReference : PdfObject
    {
        public IndirectObjectReference(int id, int generation)
        {
            Id = id;
            Generation = generation;
        }

        public int Id { get; }
        public int Generation { get; }

        public override async Task WriteOutputAsync(Stream stream)
        {
            // e.g. 12 0 R

            // Object number
            await stream.WriteIntAsync(Id);
            await stream.WriteWhitespaceAsync();

            // Generation number
            await stream.WriteIntAsync(Generation);
            await stream.WriteWhitespaceAsync();

            await stream.WriteTextAsync(Constants.IndirectReference);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode(); // TODO
        }
    }
}
