using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Syntax.Objects.Streams
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.8.2 - Stream extent
    /// </summary>
    public class StreamDictionary : Dictionary, IStreamDictionary
    {
        public StreamDictionary(Dictionary dictionary)
            : base(dictionary) { }

        protected StreamDictionary(Dictionary<Name, IPdfObject> streamDictionary, IPdfEditor pdfEditor)
            : base(streamDictionary, pdfEditor) { }

        protected StreamDictionary(
            Name? type,
            Number length,
            ShorthandArrayObject? filter,
            ShorthandArrayObject? decodeParms,
            Dictionary? f,
            ShorthandArrayObject? fFilter,
            ShorthandArrayObject? fDecodeParms,
            Number? dL,
            IPdfEditor pdfEditor
            )
            : base(type, pdfEditor)
        {
            Set(Constants.DictionaryKeys.Stream.Length, length);
            Set(Constants.DictionaryKeys.Stream.Filter, filter);
            Set(Constants.DictionaryKeys.Stream.DecodeParms, decodeParms);
            Set(Constants.DictionaryKeys.Stream.F, f);
            Set(Constants.DictionaryKeys.Stream.FFilter, fFilter);
            Set(Constants.DictionaryKeys.Stream.FDecodeParms, fDecodeParms);
            Set(Constants.DictionaryKeys.Stream.DL, dL);
        }

        public DictionaryProperty<Number> Length => Get<Number>(Constants.DictionaryKeys.Stream.Length)!;
        public DictionaryMultiProperty<Name?, ArrayObject?> Filter => Get<Name?, ArrayObject?>(Constants.DictionaryKeys.Stream.Filter);
        public DictionaryMultiProperty<Dictionary?, ArrayObject?> DecodeParms => Get<Dictionary?, ArrayObject?>(Constants.DictionaryKeys.Stream.DecodeParms);
        public DictionaryProperty<Dictionary?> F => Get<Dictionary?>(Constants.DictionaryKeys.Stream.F);
        public DictionaryMultiProperty<Name?, ArrayObject?> FFilter => Get<Name?, ArrayObject?>(Constants.DictionaryKeys.Stream.FFilter);
        public DictionaryMultiProperty<Dictionary?, ArrayObject?> FDecodeParms => Get<Dictionary?, ArrayObject?>(Constants.DictionaryKeys.Stream.FDecodeParms);
        public DictionaryProperty<Number?> DL => Get<Number?>(Constants.DictionaryKeys.Stream.DL);

        public static StreamDictionary FromDictionary(Dictionary<Name, IPdfObject> streamDictionary, IPdfEditor pdfEditor)
        {
            if (!streamDictionary.ContainsKey(Constants.DictionaryKeys.Stream.Length))
            {
                throw new ArgumentException("Missing stream Length property.");
            }

            return streamDictionary is null
                ? throw new ArgumentNullException(nameof(streamDictionary))
                : new(streamDictionary, pdfEditor);
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
