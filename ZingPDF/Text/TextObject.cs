using ZingPDF.Extensions;
using ZingPDF.Graphics.GraphicsObjects;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Text
{
    internal class TextObject : PdfObject
    {
        private readonly LiteralString _text;
        private readonly FontOptions _fontOptions;

        public TextObject(LiteralString text, FontOptions fontOptions)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _fontOptions = fontOptions ?? throw new ArgumentNullException(nameof(fontOptions));
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            // Begin text object
            await stream.WriteTextAsync(Operators.TextObjects.BT);
            await stream.WriteWhitespaceAsync();

            // Set text font and size
            await _fontOptions.FontResource.WriteAsync(stream);
            await stream.WriteWhitespaceAsync();
            await _fontOptions.Size.WriteAsync(stream);
            await stream.WriteWhitespaceAsync();
            await stream.WriteTextAsync(Operators.TextState.Tf);
            await stream.WriteWhitespaceAsync();

            // Position text
            await stream.WriteIntAsync(1);
            await stream.WriteWhitespaceAsync();
            await stream.WriteIntAsync(14);
            await stream.WriteWhitespaceAsync();
            await stream.WriteTextAsync(Operators.TextPositioning.Td);
            await stream.WriteWhitespaceAsync();

            // Show text
            await _text.WriteAsync(stream);
            await stream.WriteWhitespaceAsync();
            await stream.WriteTextAsync(Operators.TextShowing.Tj);
            await stream.WriteWhitespaceAsync();

            // End text object
            await stream.WriteTextAsync(Operators.TextObjects.ET);
        }

        public class FontOptions(Name fontResource, Integer size)
        {
            public Name FontResource { get; } = fontResource ?? throw new ArgumentNullException(nameof(fontResource));
            public Integer Size { get; } = size ?? throw new ArgumentNullException(nameof(size));
        }
    }
}
