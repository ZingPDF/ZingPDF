using System.Collections;
using System.Diagnostics.CodeAnalysis;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.7 - Dictionary objects
    /// </summary>
    public class Dictionary : PdfObject, IDictionary<Name, IPdfObject>
    {
        private readonly IDictionary<Name, IPdfObject> _dictionary = new Dictionary<Name, IPdfObject>();

        public Dictionary(IDictionary<Name, IPdfObject>? dictionary = null)
        {
            _dictionary = dictionary ?? new Dictionary<Name, IPdfObject>();
        }

        /// <summary>
        /// Strongly typed access to the underlying <see cref="PdfObject"/>.
        /// </summary>
        /// <remarks>
        /// Will return null if the specified key does not exist or the value is not assignable to the requested type.
        /// </remarks>
        public T? Get<T>(Name key) where T : class, IPdfObject
            => _dictionary.TryGetValue(key, out IPdfObject? value) ? value as T
            : null;

        public void Set<T>(Name key, T pdfObject) where T : IPdfObject
        {
            _dictionary[key] = pdfObject;
        }

        #region IDictionary
        public virtual IPdfObject this[Name key] { get => _dictionary[key]; set => _dictionary[key] = value; }

        public virtual void Add(Name key, IPdfObject value) => _dictionary.Add(key, value);

        public virtual void Add(KeyValuePair<Name, IPdfObject> item) => _dictionary.Add(item);

        public virtual void Clear() => _dictionary.Clear();

        public virtual bool Remove(Name key) => _dictionary.Remove(key);

        public virtual bool Remove(KeyValuePair<Name, IPdfObject> item) => _dictionary.Remove(item);

        public ICollection<Name> Keys => _dictionary.Keys;

        public ICollection<IPdfObject> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool IsReadOnly => _dictionary.IsReadOnly;

        public bool Contains(KeyValuePair<Name, IPdfObject> item) => _dictionary.Contains(item);

        public bool ContainsKey(Name key) => _dictionary.ContainsKey(key);

        public void CopyTo(KeyValuePair<Name, IPdfObject>[] array, int arrayIndex) => _dictionary.CopyTo(array, arrayIndex);

        public bool TryGetValue(Name key, [MaybeNullWhen(false)] out IPdfObject value) => _dictionary.TryGetValue(key, out value);
        
        public IEnumerator<KeyValuePair<Name, IPdfObject>> GetEnumerator() => _dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();
        #endregion

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteTextAsync(Constants.DictionaryStart);

            foreach (var kvp in _dictionary)
            {
                if (kvp.Value is null)
                {
                    continue;
                }

                await kvp.Key.WriteAsync(stream);
                await stream.WriteWhitespaceAsync();
                await kvp.Value.WriteAsync(stream);
            }

            await stream.WriteTextAsync(Constants.DictionaryEnd);
        }

        public static implicit operator Dictionary(Dictionary<Name, IPdfObject> value) => new(value);
    }
}
