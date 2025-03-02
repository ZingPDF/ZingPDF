using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.Functions
{
    internal abstract class StreamFunctionDictionary : FunctionDictionary, IStreamDictionary
    {
        protected StreamFunctionDictionary(Number functionType) : base(functionType) { }
        protected StreamFunctionDictionary(Dictionary dict) : base(dict) { }

        public AsyncProperty<Number> Length => Get<Number>(Constants.DictionaryKeys.Stream.Length)!;
        public AsyncProperty<ShorthandArrayObject>? Filter => Get<ShorthandArrayObject>(Constants.DictionaryKeys.Stream.Filter);
        public AsyncProperty<ShorthandArrayObject>? DecodeParms => Get<ShorthandArrayObject>(Constants.DictionaryKeys.Stream.DecodeParms);
        public AsyncProperty<Dictionary>? F => Get<Dictionary>(Constants.DictionaryKeys.Stream.F);
        public AsyncProperty<ShorthandArrayObject>? FFilter => Get<ShorthandArrayObject>(Constants.DictionaryKeys.Stream.FFilter);
        public AsyncProperty<ShorthandArrayObject>? FDecodeParms => Get<ShorthandArrayObject>(Constants.DictionaryKeys.Stream.FDecodeParms);
        public AsyncProperty<Number>? DL => Get<Number>(Constants.DictionaryKeys.Stream.DL);

        //public void SetStreamProperties(Dictionary streamDictionary)
        //{
        //    Set(Constants.DictionaryKeys.Stream.Length, streamDictionary[Constants.DictionaryKeys.Stream.Length]);
        //    Set(Constants.DictionaryKeys.Stream.Filter, streamDictionary[Constants.DictionaryKeys.Stream.Filter]);
        //    Set(Constants.DictionaryKeys.Stream.DecodeParms, streamDictionary[Constants.DictionaryKeys.Stream.DecodeParms]);
        //    Set(Constants.DictionaryKeys.Stream.F, streamDictionary[Constants.DictionaryKeys.Stream.F]);
        //    Set(Constants.DictionaryKeys.Stream.FFilter, streamDictionary[Constants.DictionaryKeys.Stream.FFilter]);
        //    Set(Constants.DictionaryKeys.Stream.FDecodeParms, streamDictionary[Constants.DictionaryKeys.Stream.FDecodeParms]);
        //    Set(Constants.DictionaryKeys.Stream.DL, streamDictionary[Constants.DictionaryKeys.Stream.DL]);
        //}
    }
}
