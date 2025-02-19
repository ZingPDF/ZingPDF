using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Syntax.Objects.Streams
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.8.2 - Stream extent
    /// </summary>
    public class StreamDictionary : Dictionary, IStreamDictionary
    {
        protected StreamDictionary(
            Name? type,
            Integer length,
            ShorthandArrayObject? filter,
            ShorthandArrayObject? decodeParms,
            Dictionary? f,
            ShorthandArrayObject? fFilter,
            ShorthandArrayObject? fDecodeParms,
            Integer? dL
            )
            : base(type)
        {
            Set(Constants.DictionaryKeys.Stream.Length, length);
            Set(Constants.DictionaryKeys.Stream.Filter, filter);
            Set(Constants.DictionaryKeys.Stream.DecodeParms, decodeParms);
            Set(Constants.DictionaryKeys.Stream.F, f);
            Set(Constants.DictionaryKeys.Stream.FFilter, fFilter);
            Set(Constants.DictionaryKeys.Stream.FDecodeParms, fDecodeParms);
            Set(Constants.DictionaryKeys.Stream.DL, dL);
        }

        protected StreamDictionary(Dictionary streamDictionary) : base(streamDictionary) { }

        public AsyncProperty<Integer> Length => Get<Integer>(Constants.DictionaryKeys.Stream.Length)!;
        public AsyncProperty<ShorthandArrayObject>? Filter => Get<ShorthandArrayObject>(Constants.DictionaryKeys.Stream.Filter);
        public AsyncProperty<ShorthandArrayObject>? DecodeParms => Get<ShorthandArrayObject>(Constants.DictionaryKeys.Stream.DecodeParms);
        public AsyncProperty<Dictionary>? F => Get<Dictionary>(Constants.DictionaryKeys.Stream.F);
        public AsyncProperty<ShorthandArrayObject>? FFilter => Get<ShorthandArrayObject>(Constants.DictionaryKeys.Stream.FFilter);
        public AsyncProperty<ShorthandArrayObject>? FDecodeParms => Get<ShorthandArrayObject>(Constants.DictionaryKeys.Stream.FDecodeParms);
        public AsyncProperty<Integer>? DL => Get<Integer>(Constants.DictionaryKeys.Stream.DL);

        public static StreamDictionary FromDictionary(Dictionary streamDictionary)
        {
            if (!streamDictionary.ContainsKey(Constants.DictionaryKeys.Stream.Length))
            {
                throw new ArgumentException("Missing stream Length property.");
            }

            return streamDictionary is null
                ? throw new ArgumentNullException(nameof(streamDictionary))
                : new(streamDictionary);
        }

        //public void SetStreamProperties(Dictionary streamDictionary)
        //{
        //    Set(Constants.DictionaryKeys.Stream.Length, streamDictionary[Constants.DictionaryKeys.Stream.Length]);
        //    Set(Constants.DictionaryKeys.Stream.Filter, streamDictionary.TryGetValue(Constants.DictionaryKeys.Stream.Filter, out var item) ? item : null);
        //    Set(Constants.DictionaryKeys.Stream.DecodeParms, streamDictionary.TryGetValue(Constants.DictionaryKeys.Stream.DecodeParms, out item) ? item : null);
        //    Set(Constants.DictionaryKeys.Stream.F, streamDictionary.TryGetValue(Constants.DictionaryKeys.Stream.F, out item) ? item : null);
        //    Set(Constants.DictionaryKeys.Stream.FFilter, streamDictionary.TryGetValue(Constants.DictionaryKeys.Stream.FFilter, out item) ? item : null);
        //    Set(Constants.DictionaryKeys.Stream.FDecodeParms, streamDictionary.TryGetValue(Constants.DictionaryKeys.Stream.FDecodeParms, out item) ? item : null);
        //    Set(Constants.DictionaryKeys.Stream.DL, streamDictionary.TryGetValue(Constants.DictionaryKeys.Stream.DL, out item) ? item : null);
        //}
    }
}
