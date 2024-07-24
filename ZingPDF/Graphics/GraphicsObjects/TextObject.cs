using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Graphics.GraphicsObjects
{
    internal class TextObject : PdfObject
    {
        private readonly LiteralString _text;
        private readonly FontOptions? _fontOptions;

        public TextObject(LiteralString text, FontOptions? fontOptions = null)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _fontOptions = fontOptions;
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteTextAsync(Operators.TextObjects.BT);

            if (_fontOptions != null)
            {
                await _fontOptions.FontResource.WriteAsync(stream);
                await stream.WriteWhitespaceAsync();

                await _fontOptions.Size.WriteAsync(stream);
                await stream.WriteWhitespaceAsync();

                await stream.WriteTextAsync(Operators.TextState.Tf);
                await stream.WriteWhitespaceAsync();
            }

            await _text.WriteAsync(stream);
            await stream.WriteWhitespaceAsync();

            await stream.WriteTextAsync(Operators.TextShowing.Tj);
            await stream.WriteWhitespaceAsync();

            await stream.WriteTextAsync(Operators.TextObjects.ET);
        }

        public class FontOptions
        {
            public FontOptions(Name fontResource, Integer size)
            {
                FontResource = fontResource ?? throw new ArgumentNullException(nameof(fontResource));
                Size = size;
            }

            public Name FontResource { get; }
            public Integer Size { get; }
        }
    }
}
