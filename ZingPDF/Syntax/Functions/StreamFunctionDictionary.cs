using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.Functions
{
    internal abstract class StreamFunctionDictionary : FunctionDictionary, IStreamDictionary
    {
        protected StreamFunctionDictionary(Integer functionType) : base(functionType) { }
        protected StreamFunctionDictionary(Dictionary dict) : base(dict) { }

        public IPdfObject Length => Get<IPdfObject>(Constants.DictionaryKeys.Stream.Length)!;
        public IPdfObject? Filter => Get<IPdfObject>(Constants.DictionaryKeys.Stream.Filter);
        public IPdfObject? DecodeParms => Get<IPdfObject>(Constants.DictionaryKeys.Stream.DecodeParms);
        public Dictionary? F => Get<Dictionary>(Constants.DictionaryKeys.Stream.F);
        public IPdfObject? FFilter => Get<IPdfObject>(Constants.DictionaryKeys.Stream.FFilter);
        public IPdfObject? FDecodeParms => Get<IPdfObject>(Constants.DictionaryKeys.Stream.FDecodeParms);
        public Integer? DL => Get<Integer>(Constants.DictionaryKeys.Stream.DL);

        public void SetStreamProperties(Dictionary streamDictionary)
        {
            Set(Constants.DictionaryKeys.Stream.Length, streamDictionary[Constants.DictionaryKeys.Stream.Length]);
            Set(Constants.DictionaryKeys.Stream.Filter, streamDictionary[Constants.DictionaryKeys.Stream.Filter]);
            Set(Constants.DictionaryKeys.Stream.DecodeParms, streamDictionary[Constants.DictionaryKeys.Stream.DecodeParms]);
            Set(Constants.DictionaryKeys.Stream.F, streamDictionary[Constants.DictionaryKeys.Stream.F]);
            Set(Constants.DictionaryKeys.Stream.FFilter, streamDictionary[Constants.DictionaryKeys.Stream.FFilter]);
            Set(Constants.DictionaryKeys.Stream.FDecodeParms, streamDictionary[Constants.DictionaryKeys.Stream.FDecodeParms]);
            Set(Constants.DictionaryKeys.Stream.DL, streamDictionary[Constants.DictionaryKeys.Stream.DL]);
        }
    }
}
