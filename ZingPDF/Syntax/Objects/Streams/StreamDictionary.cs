namespace ZingPDF.Syntax.Objects.Streams
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.8.2 - Stream extent
    /// </summary>
    public class StreamDictionary : Dictionary, IStreamDictionary
    {
        protected StreamDictionary(Name? type) : base(type) { }
        protected StreamDictionary(Dictionary streamDictionary) : base(streamDictionary) { }

        public Integer Length => Get<Integer>(Constants.DictionaryKeys.Stream.Length)!;
        public IPdfObject? Filter => Get<IPdfObject>(Constants.DictionaryKeys.Stream.Filter);
        public IPdfObject? DecodeParms => Get<IPdfObject>(Constants.DictionaryKeys.Stream.DecodeParms);
        public Dictionary? F => Get<Dictionary>(Constants.DictionaryKeys.Stream.F);
        public IPdfObject? FFilter => Get<IPdfObject>(Constants.DictionaryKeys.Stream.FFilter);
        public IPdfObject? FDecodeParms => Get<IPdfObject>(Constants.DictionaryKeys.Stream.FDecodeParms);
        public Integer? DL => Get<Integer>(Constants.DictionaryKeys.Stream.DL);

        public static StreamDictionary FromDictionary(Dictionary streamDictionary)
        {
            return streamDictionary is null
                ? throw new ArgumentNullException(nameof(streamDictionary))
                : new(streamDictionary);
        }
    }
}
