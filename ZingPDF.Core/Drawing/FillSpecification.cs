using System;
using System.ComponentModel.DataAnnotations;

namespace ZingPdf.Core.Drawing
{
    public class FillSpecification
    {
        [Obsolete("Reserved for deserialisation")]
        public FillSpecification() { }

        public FillSpecification(RGBAColour colour)
        {
            Colour = colour ?? throw new ArgumentNullException(nameof(colour));
        }
        /// <summary>
        /// Fill colour.
        /// </summary>
        [Required]
        public RGBAColour Colour { get; set; } = RGBAColour.Black;
    }
}
