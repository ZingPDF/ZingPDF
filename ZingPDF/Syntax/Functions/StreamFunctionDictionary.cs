using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.Functions
{
    internal abstract class StreamFunctionDictionary : FunctionDictionary, IStreamDictionary
    {
        protected StreamFunctionDictionary(Integer functionType) : base(functionType) { }
        protected StreamFunctionDictionary(Dictionary dict) : base(dict) { }

        public Integer Length => Get<Integer>(Constants.DictionaryKeys.Stream.Length)!;
        public IPdfObject? Filter => Get<IPdfObject>(Constants.DictionaryKeys.Stream.Filter);
        public IPdfObject? DecodeParms => Get<IPdfObject>(Constants.DictionaryKeys.Stream.DecodeParms);
        public Dictionary? F => Get<Dictionary>(Constants.DictionaryKeys.Stream.F);
        public IPdfObject? FFilter => Get<IPdfObject>(Constants.DictionaryKeys.Stream.FFilter);
        public IPdfObject? FDecodeParms => Get<IPdfObject>(Constants.DictionaryKeys.Stream.FDecodeParms);
        public Integer? DL => Get<Integer>(Constants.DictionaryKeys.Stream.DL);
    }
}
