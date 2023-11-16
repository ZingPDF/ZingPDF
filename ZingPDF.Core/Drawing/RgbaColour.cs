namespace ZingPdf.Core.Drawing
{
    public class RGBAColour
    {
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
        public byte Red { get; } = 0;

        /// <summary>
        /// Value for the green channel.
        /// </summary>
        public byte Green { get; } = 0;

        /// <summary>
        /// Value for the blue channel.
        /// </summary>
        public byte Blue { get; } = 0;

        /// <summary>
        /// Value for the alpha channel.
        /// </summary>
        public byte Alpha { get; } = 255;

        public static RGBAColour Black { get => new(0, 0, 0); }
        public static RGBAColour White { get => new(255, 255, 255); }
        public static RGBAColour PrimaryRed { get => new(255, 0, 0); }
        public static RGBAColour PrimaryGreen { get => new(0, 255, 0); }
        public static RGBAColour PrimaryBlue { get => new(0, 0, 255); }
    }
}
