using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Graphics
{
    public class RGBColour : PdfObject
    {
        public RGBColour(Number red, Number green, Number blue)
        {
            if (red < 0 || red > 1) throw new ArgumentOutOfRangeException(nameof(red));
            if (green < 0 || green > 1) throw new ArgumentOutOfRangeException(nameof(green));
            if (blue < 0 || blue > 1) throw new ArgumentOutOfRangeException(nameof(blue));

            Red = red;
            Green = green;
            Blue = blue;
        }

        /// <summary>
        /// Value for the red channel.
        /// </summary>
        public Number Red { get; } = 0;

        /// <summary>
        /// Value for the green channel.
        /// </summary>
        public Number Green { get; } = 0;

        /// <summary>
        /// Value for the blue channel.
        /// </summary>
        public Number Blue { get; } = 0;

        public static RGBColour Black { get => new(0, 0, 0); }
        public static RGBColour White { get => new(1, 1, 1); }
        public static RGBColour PrimaryRed { get => new(1, 0, 0); }
        public static RGBColour PrimaryGreen { get => new(0, 1, 0); }
        public static RGBColour PrimaryBlue { get => new(0, 0, 1); }

        public IReadOnlyList<Number> Values => [Red, Green, Blue];

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteDoubleAsync(Red);
            await stream.WriteWhitespaceAsync();

            await stream.WriteDoubleAsync(Green);
            await stream.WriteWhitespaceAsync();

            await stream.WriteDoubleAsync(Blue);
            await stream.WriteWhitespaceAsync();
        }
    }
}
