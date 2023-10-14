using System.Collections;
using System.Diagnostics.CodeAnalysis;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.7 - Dictionary objects
    /// </summary>
    internal class Dictionary : PdfObject, IDictionary<Name, PdfObject>
    {
        private readonly IDictionary<Name, PdfObject> _dictionary = new Dictionary<Name, PdfObject>();

        public Dictionary(IDictionary<Name, PdfObject>? dictionary = null)
        {
            _dictionary = dictionary ?? new Dictionary<Name, PdfObject>();
        }

        public T Get<T>(Name key) where T : PdfObject => (T)_dictionary[key];

        #region IDictionary
        public virtual PdfObject this[Name key] { get => _dictionary[key]; set => _dictionary[key] = value; }

        public virtual void Add(Name key, PdfObject value) => _dictionary.Add(key, value);

        public virtual void Add(KeyValuePair<Name, PdfObject> item) => _dictionary.Add(item);

        public virtual void Clear() => _dictionary.Clear();

        public virtual bool Remove(Name key) => _dictionary.Remove(key);

        public virtual bool Remove(KeyValuePair<Name, PdfObject> item) => _dictionary.Remove(item);

        public ICollection<Name> Keys => _dictionary.Keys;

        public ICollection<PdfObject> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool IsReadOnly => _dictionary.IsReadOnly;

        public bool Contains(KeyValuePair<Name, PdfObject> item) => _dictionary.Contains(item);

        public bool ContainsKey(Name key) => _dictionary.ContainsKey(key);

        public void CopyTo(KeyValuePair<Name, PdfObject>[] array, int arrayIndex) => _dictionary.CopyTo(array, arrayIndex);

        public bool TryGetValue(Name key, [MaybeNullWhen(false)] out PdfObject value) => _dictionary.TryGetValue(key, out value);
        
        public IEnumerator<KeyValuePair<Name, PdfObject>> GetEnumerator() => _dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();
        #endregion

        protected override async Task WriteOutputAsync(Stream stream)
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

        public static implicit operator Dictionary(Dictionary<Name, PdfObject> value) => new(value);
    }
}
