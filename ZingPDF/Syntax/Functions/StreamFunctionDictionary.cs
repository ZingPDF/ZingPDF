using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.Functions
{
    internal abstract class StreamFunctionDictionary : FunctionDictionary, IStreamDictionary
    {
        protected StreamFunctionDictionary(Number functionType, IPdfEditor pdfEditor)
            : base(functionType, pdfEditor) { }

        protected StreamFunctionDictionary(Dictionary dict) : base(dict) { }

        public AsyncProperty<Number> Length => Get<Number>(Constants.DictionaryKeys.Stream.Length)!;
        public AsyncMultiProperty<Name, ArrayObject>? Filter => Get<Name, ArrayObject>(Constants.DictionaryKeys.Stream.Filter);
        public AsyncMultiProperty<Dictionary, ArrayObject>? DecodeParms => Get<Dictionary, ArrayObject>(Constants.DictionaryKeys.Stream.DecodeParms);
        public AsyncProperty<Dictionary>? F => Get<Dictionary>(Constants.DictionaryKeys.Stream.F);
        public AsyncMultiProperty<Name, ArrayObject>? FFilter => Get<Name, ArrayObject>(Constants.DictionaryKeys.Stream.FFilter);
        public AsyncMultiProperty<Dictionary, ArrayObject>? FDecodeParms => Get<Dictionary, ArrayObject>(Constants.DictionaryKeys.Stream.FDecodeParms);
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
