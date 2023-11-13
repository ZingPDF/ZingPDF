using System;
using System.ComponentModel.DataAnnotations;

namespace ZingPdf.Core.Drawing
{
    public class TextSpecification
    {
        [Obsolete("Reserved for deserialisation")]
        public TextSpecification() { }

        public TextSpecification(string fontFamily, int fontSize, RGBAColour colour, string text)
        {
            if (string.IsNullOrWhiteSpace(fontFamily)) throw new ArgumentException("Null or blank argument", nameof(fontFamily));
            if (fontSize < 1) throw new ArgumentOutOfRangeException(nameof(fontSize), "Value must be greater than zero");
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Null or blank argument", nameof(text));

            FontFamily = fontFamily;
            FontSize = fontSize;
            Colour = colour ?? throw new ArgumentNullException(nameof(colour));
            Text = text;
        }

        [Required]
        public string FontFamily { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int FontSize { get; set; }

        [Required]
        public RGBAColour Colour { get; set; } = RGBAColour.Black;

        [Required]
        public string Text { get; set; }
    }
}
