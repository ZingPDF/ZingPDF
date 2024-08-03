using ZingPDF.Elements.Drawing;

namespace ZingPDF.Elements
{
    public class Image : IDisposable
    {
        private bool _disposedValue;

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

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    ((IDisposable)ImageData).Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
