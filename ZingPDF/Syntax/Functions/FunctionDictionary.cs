using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Syntax.Functions;

internal abstract class FunctionDictionary : Dictionary
{
    protected FunctionDictionary(Number functionType, IPdfEditor pdfEditor)
        : base(pdfEditor)
    {
        ArgumentNullException.ThrowIfNull(functionType);

        Set(Constants.DictionaryKeys.Function.FunctionType, functionType);
    }

    protected FunctionDictionary(Dictionary dict) : base(dict) { }

    /// <summary>
    /// <para>(Required) The function type:</para>
    /// <para>0 Sampled function</para>
    /// <para>2 Exponential interpolation function</para>
    /// <para>3 Stitching function</para>
    /// <para>4 PostScript calculator function</para>
    /// </summary>
    public DictionaryProperty<Number> FunctionType => Get<Number>(Constants.DictionaryKeys.Function.FunctionType);

    /// <summary>
    /// (Required) An array of 2 × m numbers, where m shall be the number of input values. 
    /// For each i from 0 to m - 1, Domain2i shall be less than or equal to Domain2i+1, 
    /// and the ith input value, xi, shall lie in the interval Domain2i ≤ xi ≤ Domain2i+1. 
    /// Input values outside the declared domain shall be clipped to the nearest boundary value.
    /// </summary>
    public DictionaryProperty<ArrayObject> Domain => Get<ArrayObject>(Constants.DictionaryKeys.Function.Domain);

    /// <summary>
    /// (Required for Type 0 and Type 4 functions, optional otherwise; see below) 
    /// An array of 2 × n numbers, where n shall be the number of output values. 
    /// For each j from 0 to n - 1, Range2j shall be less than or equal to Range2j+1, 
    /// and the jth output value, yj, shall lie in the interval Range2j ≤ yj ≤ Range2j+1. 
    /// Output values outside the declared range shall be clipped to the nearest boundary value. 
    /// If this entry is absent, no clipping shall be done (subject to implementation limits).
    /// </summary>
    public DictionaryProperty<ArrayObject?> Range => Get<ArrayObject?>(Constants.DictionaryKeys.Function.FunctionType);
}
