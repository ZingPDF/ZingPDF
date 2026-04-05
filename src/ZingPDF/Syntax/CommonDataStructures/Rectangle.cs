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
        private Rectangle(Coordinate lowerLeft, Coordinate upperRight, ObjectContext context)
            : base(context)
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

        public static implicit operator Rectangle(Size size) => FromSize(size);

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

        public static Rectangle FromDimensions(double width, double height)
            => new(new(0, 0), new(width, height), ObjectContext.UserCreated);

        public static Rectangle FromDimensions(double width, double height, ObjectContext context)
            => new(new(0, 0), new(width, height), context);

        public static Rectangle FromSize(Size size)
            => new(new(0, 0), new(size.Width, size.Height), ObjectContext.UserCreated);

        public static Rectangle FromSize(Size size, ObjectContext context)
            => new(new(0, 0), new(size.Width, size.Height), context);

        public static Rectangle FromCoordinates(Coordinate lowerLeft, Coordinate upperRight)
            => new(lowerLeft, upperRight, ObjectContext.UserCreated);

        public static Rectangle FromCoordinates(Coordinate lowerLeft, Coordinate upperRight, ObjectContext context)
            => new(lowerLeft, upperRight, context);

        public override object Clone() => FromSize(Size, Context);
    }
}
