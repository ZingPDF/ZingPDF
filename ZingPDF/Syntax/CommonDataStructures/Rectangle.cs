using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Syntax.CommonDataStructures
{
    /// <summary>
    /// PDF 32000-1:2008 7.9.5
    /// </summary>
    public class Rectangle : PdfObject
    {
        public Rectangle(Coordinate lowerLeft, Coordinate upperRight)
        {
            LowerLeft = lowerLeft ?? throw new ArgumentNullException(nameof(lowerLeft));
            UpperRight = upperRight ?? throw new ArgumentNullException(nameof(upperRight));
        }

        public Coordinate LowerLeft { get; }
        public Coordinate UpperRight { get; }

        public RealNumber Width => UpperRight.X - LowerLeft.X;
        public RealNumber Height => UpperRight.Y - LowerLeft.Y;

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.LeftSquareBracket);

            await stream.WriteDoubleAsync(LowerLeft.X);
            await stream.WriteWhitespaceAsync();

            await stream.WriteDoubleAsync(LowerLeft.Y);
            await stream.WriteWhitespaceAsync();

            await stream.WriteDoubleAsync(UpperRight.X);
            await stream.WriteWhitespaceAsync();

            await stream.WriteDoubleAsync(UpperRight.Y);

            await stream.WriteCharsAsync(Constants.RightSquareBracket);
        }

        public static Rectangle FromSize(RealNumber width, RealNumber height) => new(new(0, 0), new(width, height));
    }

    public class Coordinate(RealNumber x, RealNumber y)
    {
        public RealNumber X { get; set; } = x ?? throw new ArgumentNullException(nameof(x));
        public RealNumber Y { get; set; } = y ?? throw new ArgumentNullException(nameof(y));
    }
}
