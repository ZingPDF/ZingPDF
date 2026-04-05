using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

namespace ZingPDF.Syntax.Objects.Streams
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.8.2 - Stream extent
    /// </summary>
    public class StreamDictionary : Dictionary, IStreamDictionary
    {
        public StreamDictionary(IPdf pdf, ObjectContext context)
            : base(pdf, context) { }

        public StreamDictionary(IPdfDictionary dictionary)
            : base(dictionary) { }

        protected StreamDictionary(Dictionary<string, IPdfObject> streamDictionary, IPdf pdf, ObjectContext context)
            : base(streamDictionary, pdf, context) { }

        protected StreamDictionary(
            Name? type,
            Number length,
            ShorthandArrayObject? filter,
            ShorthandArrayObject? decodeParms,
            Dictionary? f,
            ShorthandArrayObject? fFilter,
            ShorthandArrayObject? fDecodeParms,
            Number? dL,
            IPdf pdf,
            ObjectContext context
            )
            : base(type, pdf, context)
        {
            Set(Constants.DictionaryKeys.Stream.Length, length);
            Set(Constants.DictionaryKeys.Stream.Filter, filter);
            Set(Constants.DictionaryKeys.Stream.DecodeParms, decodeParms);
            Set(Constants.DictionaryKeys.Stream.F, f);
            Set(Constants.DictionaryKeys.Stream.FFilter, fFilter);
            Set(Constants.DictionaryKeys.Stream.FDecodeParms, fDecodeParms);
            Set(Constants.DictionaryKeys.Stream.DL, dL);
        }

        public RequiredProperty<Number> Length => GetRequiredProperty<Number>(Constants.DictionaryKeys.Stream.Length);
        public OptionalArrayOrSingle<Name> Filter => GetOptionalArrayOrSingle<Name>(Constants.DictionaryKeys.Stream.Filter);
        public OptionalArrayOrSingle<Dictionary> DecodeParms => GetOptionalArrayOrSingle<Dictionary>(Constants.DictionaryKeys.Stream.DecodeParms);
        public OptionalProperty<Dictionary> F => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.Stream.F);
        public OptionalArrayOrSingle<Name> FFilter => GetOptionalArrayOrSingle<Name>(Constants.DictionaryKeys.Stream.FFilter);
        public OptionalArrayOrSingle<Dictionary> FDecodeParms => GetOptionalArrayOrSingle<Dictionary>(Constants.DictionaryKeys.Stream.FDecodeParms);
        public OptionalProperty<Number> DL => GetOptionalProperty<Number>(Constants.DictionaryKeys.Stream.DL);

        public static StreamDictionary FromDictionary(Dictionary<string, IPdfObject> streamDictionary, IPdf pdf, ObjectContext context)
        {
            ArgumentNullException.ThrowIfNull(streamDictionary);

            if (!streamDictionary.ContainsKey(Constants.DictionaryKeys.Stream.Length))
            {
                throw new ArgumentException("Missing stream Length property.");
            }

            return new(streamDictionary, pdf, context);
        }

        public static StreamDictionary FromDictionary(IPdfDictionary streamDictionary)
        {
            return new(streamDictionary);
        }
    }
}
