namespace ZingPDF.ObjectModel.Objects.Streams
{
    public class StreamDictionary : Dictionary, IStreamDictionary
    {
        protected StreamDictionary(Name type) : base(type) { }
        protected StreamDictionary(Dictionary streamDictionary) : base(streamDictionary) { }

        public Integer Length => Get<Integer>(Constants.DictionaryKeys.Stream.Length)!;
        public IPdfObject? Filter => Get<IPdfObject>(Constants.DictionaryKeys.Stream.Filter);
        public IPdfObject? DecodeParms => Get<IPdfObject>(Constants.DictionaryKeys.Stream.DecodeParms);
        public Dictionary? F => Get<Dictionary>(Constants.DictionaryKeys.Stream.F);
        public IPdfObject? FFilter => Get<IPdfObject>(Constants.DictionaryKeys.Stream.FFilter);
        public IPdfObject? FDecodeParms => Get<IPdfObject>(Constants.DictionaryKeys.Stream.FDecodeParms);
        public Integer? DL => Get<Integer>(Constants.DictionaryKeys.Stream.DL);

        public static StreamDictionary CreateNew(
            Integer length,
            IPdfObject? filter,
            IPdfObject? decodeParms,
            Dictionary? f,
            IPdfObject? fFilter,
            IPdfObject? fDecodeParms,
            Integer? dL
            )
        {
            var dict = new Dictionary<Name, IPdfObject>
            {
                { Constants.DictionaryKeys.Stream.Length, length },
            };

            if (filter != null)
            {
                dict[Constants.DictionaryKeys.Stream.Filter] = filter;
            }

            if (decodeParms != null)
            {
                dict[Constants.DictionaryKeys.Stream.DecodeParms] = decodeParms;
            }

            if (f != null)
            {
                dict[Constants.DictionaryKeys.Stream.F] = f;
            }

            if (fFilter != null)
            {
                dict[Constants.DictionaryKeys.Stream.FFilter] = fFilter;
            }

            if (fDecodeParms != null)
            {
                dict[Constants.DictionaryKeys.Stream.F] = fDecodeParms;
            }

            if (dL != null)
            {
                dict[Constants.DictionaryKeys.Stream.DL] = dL;
            }

            return new(dict);
        }

        public static StreamDictionary FromDictionary(Dictionary streamDictionary)
        {
            return streamDictionary is null
                ? throw new ArgumentNullException(nameof(streamDictionary))
                : new(streamDictionary);
        }
    }
}
