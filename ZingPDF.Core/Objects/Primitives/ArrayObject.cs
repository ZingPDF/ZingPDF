using System.Collections;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.6 - Array objects
    /// </summary>
    public class ArrayObject : PdfObject, IEnumerable<PdfObject>
    {
        private static readonly ArrayObject _empty = new(Array.Empty<PdfObject>());

        private readonly List<PdfObject> _values = new();

        public ArrayObject(PdfObject[] values)
        {
            _values = values?.ToList() ?? throw new ArgumentNullException(nameof(values));
        }

        /// <summary>
        /// Adds an item to the <see cref="ArrayObject"/>.
        /// </summary>
        public void Add<T>(T item) where T : PdfObject
            => _values.Add(item);

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.ArrayStart);

            for (int i = 0; i < _values.Count; i++)
            {
                PdfObject obj = _values[i];

                await obj.WriteAsync(stream);

                if (i < _values.Count - 1)
                {
                    await stream.WriteWhitespaceAsync();
                }
            }

            await stream.WriteCharsAsync(Constants.ArrayEnd);
        }

        public IEnumerator<PdfObject> GetEnumerator() => ((IEnumerable<PdfObject>)_values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        public static ArrayObject Empty { get => _empty; }

        public static implicit operator ArrayObject(PdfObject[] items) => new(items);
    }
}
