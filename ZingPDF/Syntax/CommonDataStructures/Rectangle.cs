using ZingPDF.Extensions;

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

        public int Width => UpperRight.X - LowerLeft.X;
        public int Height => UpperRight.Y - LowerLeft.Y;

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.LeftSquareBracket);

            await stream.WriteIntAsync(LowerLeft.X);
            await stream.WriteWhitespaceAsync();

            await stream.WriteIntAsync(LowerLeft.Y);
            await stream.WriteWhitespaceAsync();

            await stream.WriteIntAsync(UpperRight.X);
            await stream.WriteWhitespaceAsync();

            await stream.WriteIntAsync(UpperRight.Y);

            await stream.WriteCharsAsync(Constants.RightSquareBracket);
        }

        public static Rectangle FromSize(int width, int height) => new(new(0, 0), new(width, height));
    }

    public class Coordinate
    {
        public Coordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }
    }
}
