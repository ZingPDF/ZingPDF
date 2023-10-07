using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.DataStructures
{
    /// <summary>
    /// PDF 32000-1:2008 7.9.5
    /// </summary>
    internal class Rectangle : PdfObject
    {
        private readonly Coordinate _lowerLeft;
        private readonly Coordinate _upperRight;

        public Rectangle(Coordinate lowerLeft, Coordinate upperRight)
        {
            _lowerLeft = lowerLeft ?? throw new ArgumentNullException(nameof(lowerLeft));
            _upperRight = upperRight ?? throw new ArgumentNullException(nameof(upperRight));
        }

        public override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.ArrayStart);
            
            await stream.WriteIntAsync(_lowerLeft.X);
            await stream.WriteWhitespaceAsync();

            await stream.WriteIntAsync(_lowerLeft.Y);
            await stream.WriteWhitespaceAsync();

            await stream.WriteIntAsync(_upperRight.X);
            await stream.WriteWhitespaceAsync();

            await stream.WriteIntAsync(_upperRight.Y);

            await stream.WriteCharsAsync(Constants.ArrayEnd);
        }
    }

    internal class Coordinate
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
