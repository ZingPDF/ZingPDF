using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.IndirectObjects
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.10 - Indirect objects
    /// </summary>
    public class IndirectObjectReference : PdfObject
    {
        public IndirectObjectReference(IndirectObjectId id)
        {
            Id = id;
        }

        public IndirectObjectId Id { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            // e.g. 12 0 R

            // Object number
            await stream.WriteIntAsync(Id.Index);
            await stream.WriteWhitespaceAsync();

            // Generation number
            await stream.WriteIntAsync(Id.GenerationNumber);
            await stream.WriteWhitespaceAsync();

            await stream.WriteCharsAsync(Constants.IndirectReference);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode(); // TODO
        }

        public override string ToString() => $"{nameof(IndirectObjectReference)}: {Id.Index} {Id.GenerationNumber} R";
    }
}
