using System.Collections;
using System.Diagnostics.CodeAnalysis;
using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects.Dictionaries
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.7 - Dictionary objects
    /// </summary>
    public class Dictionary : PdfObject, IPdfDictionary
    {
        private readonly Dictionary<Name, IPdfObject> _dictionary;

        public Dictionary(Name? type)
        {
            _dictionary = [];

            if (type is not null)
            {
                _dictionary[Constants.DictionaryKeys.Type] = type;
            }
        }

        public Dictionary(IEnumerable<KeyValuePair<Name, IPdfObject>> dictionary)
        {
            _dictionary = dictionary?.ToDictionary() ?? throw new ArgumentNullException(nameof(dictionary));
        }

        public Name? Type => (Name)this[Constants.DictionaryKeys.Type];

        public T? GetAs<T>(Name key) where T : class, IPdfObject => ContainsKey(key) ? (T)this[key] : null;

        /// <summary>
        /// Strongly typed access to the underlying <see cref="PdfObject"/>.
        /// </summary>
        /// <remarks>
        /// Will return null if the specified key does not exist or the value is not assignable to the requested type.
        /// </remarks>
        protected AsyncProperty<T>? Get<T>(Name key) where T : class, IPdfObject
        {
            if (_dictionary.TryGetValue(key, out IPdfObject? value))
            {
                return new AsyncProperty<T>(value);
            }

            return null;
        }

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

        protected void Set<T>(Name key, T? value) where T : class, IPdfObject
        {
            if (value is null)
            {
                return;
            }

            _dictionary[key] = value;
        }

        #region IDictionary
        public void Add(Name key, IPdfObject value) => ((IDictionary<Name, IPdfObject>)_dictionary).Add(key, value);
        public bool ContainsKey(Name key) => ((IDictionary<Name, IPdfObject>)_dictionary).ContainsKey(key);
        public bool Remove(Name key) => ((IDictionary<Name, IPdfObject>)_dictionary).Remove(key);
        public bool TryGetValue(Name key, [MaybeNullWhen(false)] out IPdfObject value) => ((IDictionary<Name, IPdfObject>)_dictionary).TryGetValue(key, out value);
        public void Add(KeyValuePair<Name, IPdfObject> item) => ((ICollection<KeyValuePair<Name, IPdfObject>>)_dictionary).Add(item);
        public void Clear() => ((ICollection<KeyValuePair<Name, IPdfObject>>)_dictionary).Clear();
        public bool Contains(KeyValuePair<Name, IPdfObject> item) => ((ICollection<KeyValuePair<Name, IPdfObject>>)_dictionary).Contains(item);
        public void CopyTo(KeyValuePair<Name, IPdfObject>[] array, int arrayIndex) => ((ICollection<KeyValuePair<Name, IPdfObject>>)_dictionary).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<Name, IPdfObject> item) => ((ICollection<KeyValuePair<Name, IPdfObject>>)_dictionary).Remove(item);
        public IEnumerator<KeyValuePair<Name, IPdfObject>> GetEnumerator() => ((IEnumerable<KeyValuePair<Name, IPdfObject>>)_dictionary).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_dictionary).GetEnumerator();
        public ICollection<Name> Keys => ((IDictionary<Name, IPdfObject>)_dictionary).Keys;
        public ICollection<IPdfObject> Values => ((IDictionary<Name, IPdfObject>)_dictionary).Values;
        public int Count => ((ICollection<KeyValuePair<Name, IPdfObject>>)_dictionary).Count;
        public bool IsReadOnly => ((ICollection<KeyValuePair<Name, IPdfObject>>)_dictionary).IsReadOnly;
        public IPdfObject this[Name key] { get => ((IDictionary<Name, IPdfObject>)_dictionary)[key]; set => ((IDictionary<Name, IPdfObject>)_dictionary)[key] = value; }
        #endregion IDictionary

        public static Dictionary Empty => new((Name?)null);

        public static implicit operator Dictionary(Dictionary<Name, IPdfObject> value) => new(value);
        public static implicit operator Dictionary<Name, IPdfObject>(Dictionary? value) => new(value ?? Empty);
    }
}
