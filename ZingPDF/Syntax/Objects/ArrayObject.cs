using System.Collections;
using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.6 - Array objects
    /// </summary>
    public class ArrayObject : PdfObject, IEnumerable<IPdfObject>
    {
        private static readonly ArrayObject _empty = new([]);

        private readonly List<IPdfObject> _values = [];

        // For collection initialisation
        internal ArrayObject() { }

        public ArrayObject(IEnumerable<IPdfObject> values)
        {
            _values = values?.ToList() ?? throw new ArgumentNullException(nameof(values));
        }

        /// <summary>
        /// Adds an item to the <see cref="ArrayObject"/>.
        /// </summary>
        public void Add<T>(T item) where T : IPdfObject
            => _values.Add(item);

        public void AddRange(IEnumerable<IPdfObject> items)
            => _values.AddRange(items);

        public void Remove<T>(Predicate<T> match) where T : IPdfObject
        {
            _values.OfType<T>().ToList().RemoveAll(match);
        }

        public T? Get<T>(int index) where T : class, IPdfObject
        {
            return _values.Count > index
                ? (T)_values[index]
                : null;
        }

        public void Clear() => _values.Clear();

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.Characters.LeftSquareBracket);

            for (int i = 0; i < _values.Count; i++)
            {
                IPdfObject obj = _values[i];

                await obj.WriteAsync(stream);

                if (i < _values.Count - 1)
                {
                    await stream.WriteWhitespaceAsync();
                }
            }

            await stream.WriteCharsAsync(Constants.Characters.RightSquareBracket);
        }

        public IEnumerator<IPdfObject> GetEnumerator() => ((IEnumerable<IPdfObject>)_values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        public static ArrayObject Empty { get => _empty; }

        public static implicit operator ArrayObject(IPdfObject[] items) => new(items);

        public IPdfObject this[int index]
        {
            get => Get<IPdfObject>(index) ?? throw new ArgumentOutOfRangeException(nameof(index));
            set => _values[index] = value;
        }
    }
}
