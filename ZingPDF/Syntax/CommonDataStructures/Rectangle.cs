using ZingPDF.Elements.Drawing;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Syntax.CommonDataStructures
{
    /// <summary>
    /// PDF 32000-1:2008 7.9.5
    /// </summary>
    public class Rectangle : PdfObject
    {
        public Rectangle(Number lowerLeftX, Number lowerLeftY, Number upperRightX, Number upperRightY)
            : this(new Coordinate(lowerLeftX, lowerLeftY), new Coordinate(upperRightX, upperRightY))
        {
        }

        public Rectangle(Coordinate lowerLeft, Coordinate upperRight)
        {
            ArgumentNullException.ThrowIfNull(lowerLeft, nameof(lowerLeft));
            ArgumentNullException.ThrowIfNull(upperRight, nameof(upperRight));

            LowerLeft = lowerLeft;
            UpperRight = upperRight;
        }

        public Coordinate LowerLeft { get; }
        public Coordinate UpperRight { get; }

        public Number Width => UpperRight.X - LowerLeft.X;
        public Number Height => UpperRight.Y - LowerLeft.Y;

        public Size Size => new(Width, Height);

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.Characters.LeftSquareBracket);

            await stream.WriteDoubleAsync(LowerLeft.X);
            await stream.WriteWhitespaceAsync();

            await stream.WriteDoubleAsync(LowerLeft.Y);
            await stream.WriteWhitespaceAsync();

            await stream.WriteDoubleAsync(UpperRight.X);
            await stream.WriteWhitespaceAsync();

            await stream.WriteDoubleAsync(UpperRight.Y);

            await stream.WriteCharsAsync(Constants.Characters.RightSquareBracket);
        }

        public static Rectangle FromDimensions(Number width, Number height) => new(new(0, 0), new(width, height));
        public static Rectangle FromSize(Size size) => new(new(0, 0), new(size.Width, size.Height));
    }
}
