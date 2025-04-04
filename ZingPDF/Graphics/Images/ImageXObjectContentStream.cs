using ZingPDF.Extensions;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Graphics.Images
{
    internal class ImageXObjectContentStream : ContentStream
    {
        private readonly Name _name;
        private readonly Rectangle _maxBounds;

        public ImageXObjectContentStream(Name name, Rectangle maxBounds)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _maxBounds = maxBounds ?? throw new ArgumentNullException(nameof(maxBounds));
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            this.SaveGraphicsState();

            // Translate CTM
            await stream.WriteTextAsync($"1 0 0 1 {_maxBounds.LowerLeft.X.Value} {_maxBounds.LowerLeft.Y.Value} cm ");

            // Rotate
            // TODO

            // Scale
            await stream.WriteTextAsync($"{_maxBounds.UpperRight.X.Value} 0 0 {_maxBounds.UpperRight.Y.Value} 0 0 cm ");

            // Paint image
            await _name.WriteAsync(stream);
            await stream.WriteWhitespaceAsync();
            await stream.WriteTextAsync(Operators.XObjects.Do);
            await stream.WriteWhitespaceAsync();

            this.RestoreGraphicsState();
        }
    }
}
