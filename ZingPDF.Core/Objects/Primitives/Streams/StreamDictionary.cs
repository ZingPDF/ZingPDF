namespace ZingPdf.Core.Objects.Primitives.Streams
{
    internal class StreamDictionary : Dictionary, IStreamDictionary
    {
        public static class DictionaryKeys
        {
            public const string Length = "Length";
            public const string Filter = "Filter";
            public const string DecodeParms = "DecodeParms";
            public const string F = "F";
            public const string FFilter = "FFilter";
            public const string FDecodeParms = "FDecodeParms";
            public const string DL = "DL";
        }

        private StreamDictionary(Dictionary streamDictionary) : base(streamDictionary) { }

        public Integer Length { get => Get<Integer>(DictionaryKeys.Length)!; }
        public PdfObject? Filter { get => Get<PdfObject>(DictionaryKeys.Filter); }
        public PdfObject? DecodeParms { get => Get<PdfObject>(DictionaryKeys.DecodeParms); }
        public Dictionary? F { get => Get<Dictionary>(DictionaryKeys.DecodeParms); }
        public PdfObject? FFilter { get => Get<PdfObject>(DictionaryKeys.FFilter); }
        public PdfObject? FDecodeParms { get => Get<PdfObject>(DictionaryKeys.FFilter); }
        public Integer? DL { get => Get<Integer>(DictionaryKeys.DL); }

        public static StreamDictionary FromDictionary(Dictionary streamDictionary)
        {
            if (streamDictionary is null) throw new ArgumentNullException(nameof(streamDictionary));

            return new(streamDictionary);
        }
    }
}
