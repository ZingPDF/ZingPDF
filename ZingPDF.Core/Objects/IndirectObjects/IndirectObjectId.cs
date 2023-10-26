namespace ZingPdf.Core.Objects.IndirectObjects
{
    internal class IndirectObjectId
    {
        public IndirectObjectId(int index, ushort generationNumber)
        {
            Index = index;
            GenerationNumber = generationNumber;
        }

        public int Index { get; }
        public ushort GenerationNumber { get; }

        public IndirectObjectReference Reference { get => new(this); }

        public override string ToString()
        {
            return $"{nameof(IndirectObjectId)} {{{Index}, {GenerationNumber}}}";
        }
    }
}
