using System.Collections;
using ZingPDF.Extensions;

namespace ZingPDF.ObjectModel.Objects
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.6 - Array objects
    /// </summary>
    public class ArrayObject : PdfObject, IEnumerable<IPdfObject>
    {
        private static readonly ArrayObject _empty = new([]);

        private readonly List<IPdfObject> _values = [];

        public ArrayObject(IPdfObject[] values)
        {
            _values = values?.ToList() ?? throw new ArgumentNullException(nameof(values));
        }

        /// <summary>
        /// Adds an item to the <see cref="ArrayObject"/>.
        /// </summary>
        public void Add<T>(T item) where T : PdfObject
            => _values.Add(item);

        public void Remove<T>(Predicate<T> match) where T : IPdfObject
        {
            _values.OfType<T>().ToList().RemoveAll(match);
        }

        public T? Get<T>(int index) where T : PdfObject
            => _values[index] as T;

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.LeftSquareBracket);

            for (int i = 0; i < _values.Count; i++)
            {
                IPdfObject obj = _values[i];

                await obj.WriteAsync(stream);

                if (i < _values.Count - 1)
                {
                    await stream.WriteWhitespaceAsync();
                }
            }

            await stream.WriteCharsAsync(Constants.RightSquareBracket);
        }

        public IEnumerator<IPdfObject> GetEnumerator() => ((IEnumerable<IPdfObject>)_values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        public static ArrayObject Empty { get => _empty; }

        public static implicit operator ArrayObject(IPdfObject[] items) => new(items);
    }
}
