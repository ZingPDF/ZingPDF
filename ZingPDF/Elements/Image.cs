using ZingPDF.Syntax.CommonDataStructures;

namespace ZingPDF.Elements
{
    public class Image : IDisposable
    {
        private bool _disposedValue;

        public Image(Stream imageData, Rectangle maxBounds, bool preserveAspectRatio = true)
        {
            ImageData = imageData ?? throw new ArgumentNullException(nameof(imageData));
            MaxBounds = maxBounds ?? throw new ArgumentNullException(nameof(maxBounds));
            PreserveAspectRatio = preserveAspectRatio;
        }

        public Stream ImageData { get; }
        public Rectangle MaxBounds { get; }
        public bool PreserveAspectRatio { get; }

        public static Image FromFile(string imagePath, Rectangle maxBounds, bool preserveAspectRatio = true)
        {
            ArgumentNullException.ThrowIfNull(imagePath, nameof(imagePath));
            ArgumentNullException.ThrowIfNull(maxBounds, nameof(maxBounds));

            var inputFileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return new Image(inputFileStream, maxBounds, preserveAspectRatio);
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
