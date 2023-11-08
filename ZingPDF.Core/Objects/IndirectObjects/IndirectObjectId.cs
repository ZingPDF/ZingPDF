namespace ZingPdf.Core.Objects.IndirectObjects
{
    public class IndirectObjectId
    {
        public IndirectObjectId(int index, ushort generationNumber)
        {
            Index = index;
            GenerationNumber = generationNumber;
        }

        public int Index { get; }
        public ushort GenerationNumber { get; }

        public IndirectObjectReference Reference { get => new(this); }

        public override string ToString() => $"{nameof(IndirectObjectId)}: {{{Index}, {GenerationNumber}}}";
    }
}
