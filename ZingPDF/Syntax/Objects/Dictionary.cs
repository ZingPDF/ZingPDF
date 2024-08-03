using System.Collections;
using System.Diagnostics.CodeAnalysis;
using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.7 - Dictionary objects
    /// </summary>
    public class Dictionary : PdfObject, IReadOnlyDictionary<Name, IPdfObject>
    {
        private readonly Dictionary<Name, IPdfObject> _dictionary;

        public Dictionary(Name? type)
        {
            _dictionary = [];

            if (type != null)
            {
                _dictionary[Constants.DictionaryKeys.Type] = type;
            }
        }

        public Dictionary(IEnumerable<KeyValuePair<Name, IPdfObject>> dictionary)
        {
            _dictionary = dictionary?.ToDictionary() ?? throw new ArgumentNullException(nameof(dictionary));
        }

        public Name? Type => Get<Name>(Constants.DictionaryKeys.Type);

        #region IReadOnlyDictionary

        public IPdfObject this[Name key] => _dictionary[key];
        public IEnumerable<Name> Keys => _dictionary.Keys;
        public IEnumerable<IPdfObject> Values => _dictionary.Values;
        public int Count => _dictionary.Count;
        public bool ContainsKey(Name key) => _dictionary.ContainsKey(key);
        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();
        public IEnumerator<KeyValuePair<Name, IPdfObject>> GetEnumerator() => _dictionary.GetEnumerator();
        public bool TryGetValue(Name key, [MaybeNullWhen(false)] out IPdfObject value) => _dictionary.TryGetValue(key, out value);     

        #endregion

        /// <summary>
        /// Strongly typed access to the underlying <see cref="PdfObject"/>.
        /// </summary>
        /// <remarks>
        /// Will return null if the specified key does not exist or the value is not assignable to the requested type.
        /// </remarks>
        public T? Get<T>(Name key) where T : class, IPdfObject
            => _dictionary.TryGetValue(key, out IPdfObject? value) ? value as T
            : null;

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

        public static Dictionary Empty => new((Name?)null);

        public static implicit operator Dictionary(Dictionary<Name, IPdfObject> value) => new(value);
    }
}
