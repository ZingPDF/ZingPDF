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

        public void SetStreamProperties(Dictionary streamDictionary)
        {
            Set(Constants.DictionaryKeys.Stream.Length, streamDictionary[Constants.DictionaryKeys.Stream.Length]);
            Set(Constants.DictionaryKeys.Stream.Filter, streamDictionary.TryGetValue(Constants.DictionaryKeys.Stream.Filter, out var item) ? item : null);
            Set(Constants.DictionaryKeys.Stream.DecodeParms, streamDictionary.TryGetValue(Constants.DictionaryKeys.Stream.DecodeParms, out item) ? item : null);
            Set(Constants.DictionaryKeys.Stream.F, streamDictionary.TryGetValue(Constants.DictionaryKeys.Stream.F, out item) ? item : null);
            Set(Constants.DictionaryKeys.Stream.FFilter, streamDictionary.TryGetValue(Constants.DictionaryKeys.Stream.FFilter, out item) ? item : null);
            Set(Constants.DictionaryKeys.Stream.FDecodeParms, streamDictionary.TryGetValue(Constants.DictionaryKeys.Stream.FDecodeParms, out item) ? item : null);
            Set(Constants.DictionaryKeys.Stream.DL, streamDictionary.TryGetValue(Constants.DictionaryKeys.Stream.DL, out item) ? item : null);
        }
    }
}
