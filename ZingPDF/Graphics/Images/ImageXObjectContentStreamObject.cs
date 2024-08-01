using ZingPDF.Elements.Drawing;
using ZingPDF.Extensions;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Graphics.Images
{
    internal class ImageXObjectContentStreamObject : ContentStreamObject
    {
        private readonly Name _name;
        private readonly Coordinate _origin;

        public ImageXObjectContentStreamObject(Name name, Coordinate origin)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _origin = origin ?? throw new ArgumentNullException(nameof(origin));
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            // Save graphics state
            await stream.WriteTextAsync(Operators.GeneralGraphicsState.q);
            await stream.WriteWhitespaceAsync();

            // Translate CTM
            await stream.WriteTextAsync($"1 0 0 1 {_origin.X} {_origin.Y} ");

            // Rotate
            // TODO

            // Scale
            // TODO

            // Paint image
            await _name.WriteAsync(stream);
            await stream.WriteWhitespaceAsync();
            await stream.WriteTextAsync(Operators.XObjects.Do);
            await stream.WriteWhitespaceAsync();

            // Restore graphics state
            await stream.WriteTextAsync(Operators.GeneralGraphicsState.Q);
        }
    }
}
