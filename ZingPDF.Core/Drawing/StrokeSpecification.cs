using System;
using System.ComponentModel.DataAnnotations;

namespace ZingPdf.Core.Drawing
{
    public class StrokeSpecification
    {
        [Obsolete("Reserved for deserialisation")]
        public StrokeSpecification() { }

        public StrokeSpecification(RGBAColour colour, int width)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width), "Value must be greater than zero");

            Colour = colour ?? throw new ArgumentNullException(nameof(colour));
            Width = width;
        }

        /// <summary>
        /// Stroke colour.
        /// </summary>
        [Required]
        public RGBAColour Colour { get; set; } = RGBAColour.Black;

        /// <summary>
        /// Stroke width, set in pixels.
        /// </summary>
        [Required, Range(1, int.MaxValue)]
        public int Width { get; set; } = 1;
    }
}
