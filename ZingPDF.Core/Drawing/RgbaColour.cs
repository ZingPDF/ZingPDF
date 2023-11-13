using System;
using System.ComponentModel.DataAnnotations;

namespace ZingPdf.Core.Drawing
{
    public class RGBAColour
    {
        [Obsolete("Reserved for deserialisation")]
        public RGBAColour() { }

        public RGBAColour(byte red, byte green, byte blue)
            :this(red, green, blue, 255) { }

        public RGBAColour(byte red, byte green, byte blue, byte alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        /// <summary>
        /// Value for the red channel.
        /// </summary>
        [Required, Range(0, 255)]
        public byte Red { get; set; } = 0;

        /// <summary>
        /// Value for the green channel.
        /// </summary>
        [Required, Range(0, 255)]
        public byte Green { get; set; } = 0;

        /// <summary>
        /// Value for the blue channel.
        /// </summary>
        [Required, Range(0, 255)]
        public byte Blue { get; set; } = 0;

        /// <summary>
        /// Value for the alpha channel.
        /// </summary>
        [Range(0, 255)]
        public byte Alpha { get; set; } = 255;

        public static RGBAColour Black { get => new RGBAColour(0, 0, 0); }
        public static RGBAColour White { get => new RGBAColour(255, 255, 255); }
        public static RGBAColour PrimaryRed { get => new RGBAColour(255, 0, 0); }
        public static RGBAColour PrimaryGreen { get => new RGBAColour(0, 255, 0); }
        public static RGBAColour PrimaryBlue { get => new RGBAColour(0, 0, 255); }
    }
}
