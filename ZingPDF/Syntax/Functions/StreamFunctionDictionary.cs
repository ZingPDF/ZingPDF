using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.Functions
{
    internal abstract class StreamFunctionDictionary : FunctionDictionary, IStreamDictionary
    {
        protected StreamFunctionDictionary(Number functionType, IPdfEditor pdfEditor)
            : base(functionType, pdfEditor) { }

        protected StreamFunctionDictionary(Dictionary dict) : base(dict) { }

        public DictionaryProperty<Number> Length => Get<Number>(Constants.DictionaryKeys.Stream.Length);
        public OptionalArrayOrSingle<Name> Filter => GetOptionalArrayOrSingle<Name>(Constants.DictionaryKeys.Stream.Filter);
        public OptionalArrayOrSingle<Dictionary> DecodeParms => GetOptionalArrayOrSingle<Dictionary>(Constants.DictionaryKeys.Stream.DecodeParms);
        public DictionaryProperty<Dictionary?> F => Get<Dictionary?>(Constants.DictionaryKeys.Stream.F);
        public OptionalArrayOrSingle<Name> FFilter => GetOptionalArrayOrSingle<Name>(Constants.DictionaryKeys.Stream.FFilter);
        public OptionalArrayOrSingle<Dictionary> FDecodeParms => GetOptionalArrayOrSingle<Dictionary>(Constants.DictionaryKeys.Stream.FDecodeParms);
        public DictionaryProperty<Number?> DL => Get<Number?>(Constants.DictionaryKeys.Stream.DL);
    }
}
