using ZingPDF.Objects.Filters;
using ZingPDF.Objects.ObjectGroups.CrossReferences;
using ZingPDF.Objects.Primitives;
using ZingPDF.Objects.Primitives.IndirectObjects;
using ZingPDF.Objects.Primitives.Streams;

namespace ZingPDF.Objects.ObjectGroups.CrossReferences.CrossReferenceStreams
{
    /// <summary>
    /// ISO 32000-2:2020 7.5.8 - Cross-reference streams
    /// </summary>
    internal class CrossReferenceStream : StreamObject<CrossReferenceStreamDictionary>
    {
        private readonly IEnumerable<CrossReferenceSection> _xrefSections;
        private readonly Integer _size;
        private readonly Integer? _prev;
        private readonly IndirectObjectReference _root;
        private readonly Dictionary? _encrypt;
        private readonly IndirectObjectReference? _info;
        private readonly ArrayObject? _id;

        public CrossReferenceStream(
            IEnumerable<CrossReferenceSection> xrefSections,
            IEnumerable<IFilter>? filters,
            Integer size,
            Integer? prev,
            IndirectObjectReference root,
            Dictionary? encrypt,
            IndirectObjectReference? info,
            ArrayObject? id
            )
            : base(filters)
        {
            _xrefSections = xrefSections ?? throw new ArgumentNullException(nameof(xrefSections));
            _size = size ?? throw new ArgumentNullException(nameof(size));
            _prev = prev;
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _encrypt = encrypt;
            _info = info;
            _id = id;
        }

        protected override Task<Stream> GetSourceDataAsync(CrossReferenceStreamDictionary dictionary)
        {
            var ms = new MemoryStream();

            _xrefSections
                .SelectMany(section => section.Entries)
                .ToList()
                .ForEach(async entry =>
                {
                    await WriteEntryAsync(
                        ms,
                        entry,
                        dictionary.Field1Size,
                        dictionary.Field2Size,
                        dictionary.Field3Size
                        );
                });

            ms.Position = 0;

            return Task.FromResult<Stream>(ms);
        }

        protected override Task<CrossReferenceStreamDictionary> GetSpecialisedDictionaryAsync()
        {
            var index = (ArrayObject)_xrefSections.SelectMany(s => new Integer[] { s.Index.StartIndex, s.Index.Count }).ToArray();

            var allEntries = _xrefSections.SelectMany(x => x.Entries);

            var field1Size = 1; // TODO: consider supporting 0 if all entries are in use
            var field2Size = GetFieldSize(allEntries, entry => entry.Value1);
            var field3Size = GetFieldSize(allEntries, entry => entry.Value2);

            var w = (ArrayObject)new Integer[] { field1Size, field2Size, field3Size };

            return Task.FromResult(CrossReferenceStreamDictionary.CreateNew(index, w, _size, _prev, _root, _encrypt, _info, _id));
        }

        // Method to get the size of the field based on the entries
        private static int GetFieldSize(IEnumerable<CrossReferenceEntry> entries, Func<CrossReferenceEntry, long> getValue)
        {
            // Find the maximum value for the specified field
            long maxValue = entries.Max(getValue);

            // Calculate the minimum number of bytes needed to represent the maximum value
            int size = (int)Math.Ceiling(Math.Log2(maxValue + 1) / 8);

            // Ensure a minimum size of 1 byte
            return Math.Max(size, 1);
        }

        // Method to write a single entry
        private static async Task WriteEntryAsync(Stream stream, CrossReferenceEntry entry, int field1Size, int field2Size, int field3Size)
        {
            // Write entry type
            //stream.WriteByte(entry.InUse ? (byte)1 : (byte)0);
            var field1Bytes = BitConverter.GetBytes(entry.InUse ? 1 : 0);
            await WriteFieldBytesAsync(stream, field1Bytes, field1Size);

            // Write field 2
            var field2Bytes = BitConverter.GetBytes(entry.Value1);
            await WriteFieldBytesAsync(stream, field2Bytes, field2Size);

            // Write field 3
            var field3Bytes = BitConverter.GetBytes(entry.Value2);
            await WriteFieldBytesAsync(stream, field3Bytes, field3Size);
        }

        // Method to write field bytes with padding or truncation as needed
        private static async Task WriteFieldBytesAsync(Stream stream, byte[] fieldBytes, int fieldSize)
        {
            // Ensure the byte array has the correct length
            if (fieldBytes.Length < fieldSize)
            {
                // Pad the byte array with zeros on the left (for big-endian order)
                var paddedBytes = new byte[fieldSize];
                fieldBytes.CopyTo(paddedBytes, fieldSize - fieldBytes.Length);

                await stream.WriteAsync(paddedBytes);
            }
            else if (fieldBytes.Length > fieldSize)
            {
                // Truncate the byte array to the specified length
                await stream.WriteAsync(fieldBytes.AsMemory(0, fieldSize));
            }
            else
            {
                // The byte array already has the correct length
                await stream.WriteAsync(fieldBytes);
            }
        }
    }
}
