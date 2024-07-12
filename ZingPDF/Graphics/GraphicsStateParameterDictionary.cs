using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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


    }
}
