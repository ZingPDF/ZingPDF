using ZingPDF.Elements.Drawing;

namespace ZingPDF.Elements
{
    public class Image : IDisposable
    {
        public Image(Stream imageData, Coordinate origin)
        {
            ImageData = imageData ?? throw new ArgumentNullException(nameof(imageData));
            Origin = origin ?? throw new ArgumentNullException(nameof(origin));
        }

        public Stream ImageData { get; }
        public Coordinate Origin { get; }

        public static Image FromFile(string imagePath, Coordinate? origin = null)
        {
            origin ??= Coordinate.Zero;

            var inputFileStream = new FileStream(imagePath, FileMode.Open);

            return new Image(inputFileStream, origin);
        }

        public void Dispose()
        {
            ((IDisposable)ImageData).Dispose();
        }
    }
}
