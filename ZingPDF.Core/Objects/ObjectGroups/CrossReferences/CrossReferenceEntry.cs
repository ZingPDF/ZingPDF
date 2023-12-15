using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.ObjectGroups.CrossReferences
{
    internal class CrossReferenceEntry : PdfObject
    {
        private static readonly CrossReferenceEntry _rootFreeEntry = new(0, 65535, inUse: false, compressed: false);

        /// <summary>
        /// Creates a <see cref="CrossReferenceEntry"/> instance.
        /// </summary>
        /// <param name="value1">
        /// For 'in use' objects, this value is the byte offset of the object.<para></para>
        /// For 'free' objects, this value is the object number of the next free object.<para></para>
        /// For 'compressed' objects, this value is the object number of the object stream in which the object is stored.
        /// </param>
        /// <param name="value2">
        /// For 'in use' and 'free' objects, this is the object generation number.<para></para>
        /// For 'compressed' objects, this is the index of the object within the containing object stream.
        /// </param>
        /// <param name="inUse">Indicates whether the entry is in use, or free to be reused</param>
        public CrossReferenceEntry(long value1, ushort value2, bool inUse, bool compressed)
        {
            Value1 = value1;
            Value2 = value2;
            InUse = inUse;
            Compressed = compressed;
        }

        /// <summary>
        /// For 'in use' objects, this value is the byte offset of the object.<para></para>
        /// For 'free' objects, this value is the object number of the next free object.<para></para>
        /// For 'compressed' objects, this value is the object number of the object stream in which the object is stored.
        /// </summary>
        public long Value1 { get; internal set; }

        /// <summary>
        /// For 'in use' and 'free' objects, this is the object generation number.<para></para>
        /// For 'compressed' objects, this is the index of the object within the containing object stream.
        /// </summary>
        public ushort Value2 { get; }

        public bool InUse { get; }

        /// <summary>
        /// Indicates whether the entry refers to a compressed object.
        /// When true, the object is contained within an object stream.<para></para>
        /// <see cref="Value1"/> contains the object number of the stream.<para></para>
        /// <see cref="Value2"/> contains the index of the object within the object stream.
        /// </summary>
        public bool Compressed { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            // Each xref entry is a single line representing an indirect object
            //      0000000017 00000 n
            //               |     | |
            // byte offset __|     | |
            // gen number _________| |
            // free(f) in-use(n)_____|

            // For free entries the first number represents the object number of the next free object
            //           0000000007 00001 f
            //                    |     | |
            // next free object __|     | |
            // gen number ______________| |
            // free(f) in-use(n)__________|

            await stream.WriteLeftPaddedAsync(Value1, 10);
            await stream.WriteWhitespaceAsync();

            await stream.WriteLeftPaddedAsync(Value2, 5);
            await stream.WriteWhitespaceAsync();

            await stream.WriteTextAsync(InUse ? "n" : "f");
            await stream.WriteNewLineAsync();
        }

        public static CrossReferenceEntry RootFreeEntry => _rootFreeEntry;
    }
}
