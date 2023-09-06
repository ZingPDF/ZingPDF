using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// PDF 32000-1:2008 7.3.6
    /// </summary>
    internal class Array : PdfObject
    {
        private static readonly Array _empty = new(System.Array.Empty<PdfObject>());

        private readonly PdfObject[] _values;

        public Array(PdfObject[] values)
        {
            _values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteTextAsync(Constants.ArrayStart);

            foreach(var obj in _values)
            {
                await obj.WriteAsync(stream);
            }

            await stream.WriteTextAsync(Constants.ArrayEnd);
        }

        public static Array Empty { get => _empty; }
    }
}
