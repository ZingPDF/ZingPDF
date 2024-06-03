using ZingPDF.Extensions;

namespace ZingPDF.ObjectModel.Objects.IndirectObjects
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

        public override bool Equals(object? obj)
        {
            return obj is IndirectObjectReference ior && Id == ior.Id;
        }

        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString() => $"{nameof(IndirectObjectReference)}: {Id.Index} {Id.GenerationNumber} R";

        public static bool operator ==(IndirectObjectReference left, IndirectObjectReference right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(IndirectObjectReference left, IndirectObjectReference right) => !(left == right);
    }
}
