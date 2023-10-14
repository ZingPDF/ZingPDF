using System.Collections;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.6 - Array objects
    /// </summary>
    internal class Array : PdfObject, IEnumerable<PdfObject>
    {
        private static readonly Array _empty = new(System.Array.Empty<PdfObject>());

        private readonly PdfObject[] _values;

        public Array(PdfObject[] values, long? length = null)
        {
            _values = values ?? throw new ArgumentNullException(nameof(values));
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.ArrayStart);

            foreach(var obj in _values)
            {
                await obj.WriteAsync(stream);
            }

            await stream.WriteCharsAsync(Constants.ArrayEnd);
        }

        public IEnumerator<PdfObject> GetEnumerator() => ((IEnumerable<PdfObject>)_values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        public static Array Empty { get => _empty; }

        public static implicit operator Array(PdfObject[] items) => new(items);
    }
}
