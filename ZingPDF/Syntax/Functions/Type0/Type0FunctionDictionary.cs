using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Syntax.Functions.Type0
{
    /// <summary>
    /// ISO 32000-2:2020 - 7.10.2 Type 0 (sampled) functions
    /// </summary>
    internal class Type0FunctionDictionary : StreamFunctionDictionary
    {
        public Type0FunctionDictionary(IPdfEditor pdfEditor)
            : base(Constants.FunctionTypes.Zero, pdfEditor) { }

        public Type0FunctionDictionary(Dictionary dict) : base(dict) { }

        /// <summary>
        /// (Required) An array of m positive integers that shall specify the number of 
        /// samples in each input dimension of the sample table.
        /// </summary>
        public ArrayObject Size => GetAs<ArrayObject>(Constants.DictionaryKeys.Function.Type0.Size)!;

        /// <summary>
        /// (Required) The number of bits that shall represent each sample. 
        /// (If the function has multiple output values, each one shall occupy BitsPerSample bits.) 
        /// Valid values shall be 1, 2, 4, 8, 12, 16, 24, and 32.
        /// </summary>
        public Number BitsPerSample => GetAs<Number>(Constants.DictionaryKeys.Function.Type0.BitsPerSample)!;

        /// <summary>
        /// (Optional) The order of interpolation between samples. 
        /// Valid values shall be 1 and 3, specifying linear and cubic spline interpolation, respectively. 
        /// Default value: 1.
        /// </summary>
        public AsyncProperty<Number>? Order => Get<Number>(Constants.DictionaryKeys.Function.Type0.Order);

        /// <summary>
        /// (Optional) An array of 2 × m numbers specifying the linear mapping of input values into 
        /// the domain of the function’s sample table. Default value: [0 (Size0 -1) 0 (Size1 - 1)…].
        /// </summary>
        public AsyncProperty<ArrayObject>? Encode => Get<ArrayObject>(Constants.DictionaryKeys.Function.Type0.Encode);

        /// <summary>
        /// (Optional) An array of 2 × n numbers specifying the linear mapping of sample values into 
        /// the range appropriate for the function’s output values. Default value: same as the value of Range.
        /// </summary>
        public AsyncProperty<ArrayObject>? Decode => Get<ArrayObject>(Constants.DictionaryKeys.Function.Type0.Decode);
    }
}
