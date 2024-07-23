namespace ZingPDF.Syntax.Objects.IndirectObjects
{
    public class IndirectObjectId
    {
        public IndirectObjectId(int index, ushort generationNumber)
        {
            Index = index;
            GenerationNumber = generationNumber;
        }

        public int Index { get; }
        public ushort GenerationNumber { get; internal set; }

        public IndirectObjectReference Reference { get => new(this); }

        public override bool Equals(object? obj)
        {
            return obj is IndirectObjectId id &&
                Index == id.Index &&
                   GenerationNumber == id.GenerationNumber;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, GenerationNumber);
        }

        public override string ToString() => $"{nameof(IndirectObjectId)}: {{{Index}, {GenerationNumber}}}";

        public static bool operator ==(IndirectObjectId left, IndirectObjectId right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(IndirectObjectId left, IndirectObjectId right) => !(left == right);
    }
}
