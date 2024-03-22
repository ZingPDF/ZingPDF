using ZingPDF.Extensions;

namespace ZingPDF.Objects.DataStructures
{
    /// <summary>
    /// PDF 32000-1:2008 7.9.5
    /// </summary>
    public class Rectangle : PdfObject
    {
        private readonly Coordinate _lowerLeft;
        private readonly Coordinate _upperRight;

        public Rectangle(Coordinate lowerLeft, Coordinate upperRight)
        {
            _lowerLeft = lowerLeft ?? throw new ArgumentNullException(nameof(lowerLeft));
            _upperRight = upperRight ?? throw new ArgumentNullException(nameof(upperRight));
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.LeftSquareBracket);

            await stream.WriteIntAsync(_lowerLeft.X);
            await stream.WriteWhitespaceAsync();

            await stream.WriteIntAsync(_lowerLeft.Y);
            await stream.WriteWhitespaceAsync();

            await stream.WriteIntAsync(_upperRight.X);
            await stream.WriteWhitespaceAsync();

            await stream.WriteIntAsync(_upperRight.Y);

            await stream.WriteCharsAsync(Constants.RightSquareBracket);
        }
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
