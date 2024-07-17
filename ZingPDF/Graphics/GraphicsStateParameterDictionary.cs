using ZingPDF.ObjectModel;
using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.Graphics
{
    internal class GraphicsStateParameterDictionary : Dictionary
    {
        public GraphicsStateParameterDictionary() : base(Constants.DictionaryTypes.ExtGState) { }

        private GraphicsStateParameterDictionary(Dictionary dictionary) : base(dictionary)
        {
        }

        /// <summary>
        /// (Optional; PDF 1.3) The line width (see 8.4.3.2, "Line width").
        /// </summary>
        public RealNumber? LW => Get<RealNumber>(Constants.DictionaryKeys.GraphicsStateParameter.LW);

        /// <summary>
        ///(Optional; PDF 1.3) The line cap style (see 8.4.3.3, "Line cap style").
        /// </summary>
        public Integer? LC => Get<Integer>(Constants.DictionaryKeys.GraphicsStateParameter.LC);

        /// <summary>
        /// (Optional; PDF 1.3) The line join style (see 8.4.3.4, "Line join style").
        /// </summary>
        public Integer? LJ => Get<Integer>(Constants.DictionaryKeys.GraphicsStateParameter.LJ);

        /// <summary>
        /// (Optional; PDF 1.3) The miter limit (see 8.4.3.5, "Miter limit").
        /// </summary>
        public RealNumber? ML => Get<RealNumber>(Constants.DictionaryKeys.GraphicsStateParameter.ML);

        /// <summary>
        /// (Optional; PDF 1.3) The line dash pattern, expressed as an array of the form [dashArray dashPhase], 
        /// where dashArray shall be itself an array and dashPhase shall be a number (see 8.4.3.6, "Line dash pattern").
        /// </summary>
        public ArrayObject? D => Get<ArrayObject>(Constants.DictionaryKeys.GraphicsStateParameter.D);

        /// <summary>
        /// (Optional; PDF 1.3) The name of the rendering intent (see 8.6.5.8, "Rendering intents").
        /// </summary>
        public Name? RI => Get<Name>(Constants.DictionaryKeys.GraphicsStateParameter.RI);

        /// <summary>
        /// (Optional) A flag specifying whether to apply overprint (see 8.6.7, "Overprint control"). 
        /// In PDF 1.2 and earlier, there is a single overprint parameter that applies to all painting operations. 
        /// Beginning with PDF 1.3, two separate overprint parameters were defined: one for stroking and one for 
        /// all other painting operations. Specifying an OP entry shall set both parameters unless there is also 
        /// an op entry in the same graphics state parameter dictionary, in which case the OP entry shall set only 
        /// the overprint parameter for stroking.
        /// </summary>
        public BooleanObject? OP => Get<BooleanObject>(Constants.DictionaryKeys.GraphicsStateParameter.OP);

        /// <summary>
        /// (Optional; PDF 1.3) A flag specifying whether to apply overprint (see 8.6.7, "Overprint control") 
        /// for painting operations other than stroking. If this entry is absent, the OP entry, if any, shall 
        /// also set this parameter.
        /// </summary>
        public BooleanObject? op => Get<BooleanObject>(Constants.DictionaryKeys.GraphicsStateParameter.op);

        /// <summary>
        /// (Optional; PDF 1.3) The overprint mode (see 8.6.7, "Overprint control").
        /// </summary>
        public Integer? OPM => Get<Integer>(Constants.DictionaryKeys.GraphicsStateParameter.OPM);

        /// <summary>
        /// (Optional; PDF 1.3) An array of the form [font size], where font shall be an indirect reference 
        /// to a font dictionary and size shall be a number expressed in text space units. These two objects 
        /// correspond to the operands of the Tf operator (see 9.3, "Text state parameters and operators"); 
        /// however, the first operand shall be an indirect object reference instead of a resource name.
        /// </summary>
        public ArrayObject? Font => Get<ArrayObject>(Constants.DictionaryKeys.GraphicsStateParameter.Font);

        /// <summary>
        /// (Optional) The black-generation function, which maps the interval [0.0 1.0] to the interval [0.0 1.0] 
        /// (see 10.4.2.4, "Conversion from DeviceRGB to DeviceCMYK").
        /// </summary>
        public IPdfObject? BG => Get<IPdfObject>(Constants.DictionaryKeys.GraphicsStateParameter.BG);

        /// <summary>
        /// (Optional; PDF 1.3) Same as BG except that the value may also be the name Default, denoting 
        /// the black-generation function that was in effect at the start of the page. If both BG and BG2 
        /// are present in the same graphics state parameter dictionary, BG2 shall take precedence.
        /// </summary>
        public IPdfObject? BG2 => Get<IPdfObject>(Constants.DictionaryKeys.GraphicsStateParameter.BG2);

        /// <summary>
        /// (Optional) The undercolour-removal function, which maps the interval [0.0 1.0] to the 
        /// interval [−1.0 1.0] (see 10.4.2.4, "Conversion from DeviceRGB to DeviceCMYK").
        /// </summary>
        public IPdfObject? UCR => Get<IPdfObject>(Constants.DictionaryKeys.GraphicsStateParameter.UCR);

        /// <summary>
        /// (Optional; PDF 1.3) Same as UCR except that the value may also be the name Default, denoting 
        /// the undercolour-removal function that was in effect at the start of the page. If both UCR and 
        /// UCR2 are present in the same graphics state parameter dictionary, UCR2 shall take precedence.
        /// </summary>
        public IPdfObject? UCR2 => Get<IPdfObject>(Constants.DictionaryKeys.GraphicsStateParameter.UCR);

        /// <summary>
        /// (Optional, deprecated in PDF 2.0) The transfer function, which maps the interval [0.0 1.0] 
        /// to the interval [0.0 1.0] (see 10.5, "Transfer functions"). The value shall be either a single 
        /// function (which applies to all process colourants) or an array of four functions (which apply 
        /// to the process colourants individually). The name Identity may be used to represent the Identity function.
        /// </summary>
        public IPdfObject? TR => Get<IPdfObject>(Constants.DictionaryKeys.GraphicsStateParameter.TR);

        /// <summary>
        /// (Optional; PDF 1.3, deprecated in PDF 2.0) Same as TR except that the value may also be the 
        /// name Default, denoting the transfer function that was in effect at the start of the page. 
        /// If both TR and TR2 are present in the same graphics state parameter dictionary, TR2 shall take precedence.
        /// </summary>
        public IPdfObject? TR2 => Get<IPdfObject>(Constants.DictionaryKeys.GraphicsStateParameter.TR2);

        /// <summary>
        /// (Optional) The halftone dictionary or stream (see 10.6, "Halftones") or the name Default, 
        /// denoting the halftone that was in effect at the start of the page.
        /// </summary>
        public IPdfObject? HT => Get<IPdfObject>(Constants.DictionaryKeys.GraphicsStateParameter.HT);

        /// <summary>
        /// (Optional; PDF 1.3) The flatness tolerance (see 10.7.2, "Flatness tolerance").
        /// </summary>
        public RealNumber? FL => Get<RealNumber>(Constants.DictionaryKeys.GraphicsStateParameter.FL);

        /// <summary>
        /// (Optional; PDF 1.3) The smoothness tolerance (see 10.7.3, "Smoothness tolerance").
        /// </summary>
        public RealNumber? SM => Get<RealNumber>(Constants.DictionaryKeys.GraphicsStateParameter.SM);

        /// <summary>
        /// (Optional) A flag specifying whether to apply automatic stroke adjustment (see 10.7.5, "Automatic stroke adjustment").
        /// </summary>
        public BooleanObject? SA => Get<BooleanObject>(Constants.DictionaryKeys.GraphicsStateParameter.SA);

        /// <summary>
        /// (Optional; PDF 1.4; array is deprecated in PDF 2.0) The current blend mode that shall be used 
        /// in the transparent imaging model (see 11.3.5, "Blend mode").
        /// </summary>
        public IPdfObject? BM => Get<IPdfObject>(Constants.DictionaryKeys.GraphicsStateParameter.BM);

        /// <summary>
        /// <para>(Optional; PDF 1.4) The current soft mask, specifying the mask shape or mask opacity 
        /// values that shall be used in the transparent imaging model (see 11.3.7.2, "Source shape and 
        /// opacity" and 11.6.4.3, "Mask shape and opacity").</para>
        /// <para>Although the current soft mask is sometimes referred to as a "soft clip", altering it 
        /// with the gs operator completely replaces the old value with the new one, rather than intersecting 
        /// the two as is done with the current clipping path parameter (see 8.5.4, "Clipping path operators").</para>
        /// </summary>
        public IPdfObject? SMask => Get<IPdfObject>(Constants.DictionaryKeys.GraphicsStateParameter.SMask);

        /// <summary>
        /// (Optional; PDF 1.4) The current stroking alpha constant, specifying the constant shape or constant 
        /// opacity value that shall be used for stroking operations in the transparent imaging model 
        /// (see 11.3.7.2, "Source shape and opacity" and 11.6.4.4, "Constant shape and opacity").
        /// </summary>
        public RealNumber? CA => Get<RealNumber>(Constants.DictionaryKeys.GraphicsStateParameter.CA);

        /// <summary>
        /// (Optional; PDF 1.4) Same as CA, but for nonstroking operations.
        /// </summary>
        public RealNumber? ca => Get<RealNumber>(Constants.DictionaryKeys.GraphicsStateParameter.ca);

        /// <summary>
        /// (Optional; PDF 1.4) The alpha source flag ("alpha is shape"), specifying whether the current 
        /// soft mask and alpha constant shall be interpreted as shape values (true) or opacity values (false). 
        /// This flag also governs the interpretation of the SMask entry, if any, in an image dictionary 
        /// (see 8.9.5, "Image dictionaries").
        /// </summary>
        public BooleanObject? AIS => Get<BooleanObject>(Constants.DictionaryKeys.GraphicsStateParameter.AIS);

        /// <summary>
        /// (Optional; PDF 1.4) The text knockout flag, shall determine the behaviour of overlapping glyphs 
        /// within a text object in the transparent imaging model (see 9.3.8, "Text knockout"). This flag 
        /// controls the behavior of glyphs obtained from any font type, including Type 3.
        /// </summary>
        public BooleanObject? TK => Get<BooleanObject>(Constants.DictionaryKeys.GraphicsStateParameter.TK);

        /// <summary>
        /// <para>(Optional; PDF 2.0) This graphics state parameter controls whether black point compensation is performed while doing CIE-based colour conversions. It shall be set to either OFF, ON or Default. The semantics of Default are up to the PDF processor. See 8.6.5.9, "Use of black point compensation".</para>
        /// <para>The default value is: Default.</para>
        /// </summary>
        public Name? UseBlackPtComp => Get<Name>(Constants.DictionaryKeys.GraphicsStateParameter.UseBlackPtComp);
    }
}
