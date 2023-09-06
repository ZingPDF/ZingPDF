using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    internal class Dictionary : PdfObject
    {
        private readonly IDictionary<Name, PdfObject> _dictionary;

        public Dictionary(IDictionary<Name, PdfObject>? dictionary = null)
        {
            _dictionary = dictionary ?? new Dictionary<Name, PdfObject>();
        }

        public override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteTextAsync(Constants.DictionaryStart);

            foreach (var kvp in _dictionary)
            {
                await kvp.Key.WriteAsync(stream);
                await stream.WriteWhitespaceAsync();
                await kvp.Value.WriteAsync(stream);
            }

            await stream.WriteTextAsync(Constants.DictionaryEnd);
        }
    }
}
