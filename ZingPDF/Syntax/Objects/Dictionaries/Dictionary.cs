using System.Collections;
using ZingPDF.Extensions;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

namespace ZingPDF.Syntax.Objects.Dictionaries
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.7 - Dictionary objects
    /// </summary>
    public class Dictionary : PdfObject, IPdfDictionary
    {
        private readonly Dictionary<Name, IPdfObject> _dictionary;
        private readonly IPdfEditor _pdfEditor;

        public Dictionary(IPdfEditor pdfEditor)
        {
            _dictionary = [];

            _pdfEditor = pdfEditor;
        }

        public Dictionary(Name type, IPdfEditor pdfEditor)
        {
            _dictionary = [];

            if (type is not null)
            {
                _dictionary[Constants.DictionaryKeys.Type] = type;
            }

            _pdfEditor = pdfEditor;
        }

        public Dictionary(IEnumerable<KeyValuePair<Name, IPdfObject>> dictionary, IPdfEditor pdfEditor)
        {
            _dictionary = dictionary?.ToDictionary() ?? throw new ArgumentNullException(nameof(dictionary));
            _pdfEditor = pdfEditor;
        }

        public Dictionary(Dictionary dictionary)
        {
            _dictionary = dictionary._dictionary;
            _pdfEditor = dictionary._pdfEditor;
        }

        public Name? Type => (Name)_dictionary[Constants.DictionaryKeys.Type];

        public T GetAs<T>(Name key) where T : class?, IPdfObject?
            => (_dictionary.TryGetValue(key, out IPdfObject? value) ? value as T : null)!;

        public DictionaryProperty<T> Get<T>(Name key) where T : class?, IPdfObject?
        {
            return new DictionaryProperty<T>(key, this, _pdfEditor);
        }

        public DictionaryMultiProperty<T1, T2> Get<T1, T2>(Name key)
            where T1 : class?, IPdfObject?
            where T2 : class?, IPdfObject?
        {
            return new DictionaryMultiProperty<T1, T2>(key, this, _pdfEditor);
        }

        public ArrayOrSingle<T> GetArrayOrSingle<T>(Name key) where T : class, IPdfObject
            => new(key, this, _pdfEditor);

        public OptionalArrayOrSingle<T> GetOptionalArrayOrSingle<T>(Name key) where T : class?, IPdfObject?
            => new(key, this, _pdfEditor);

        public void Set<T>(Name key, T? value) where T : class, IPdfObject
        {
            if (value is null)
            {
                _dictionary.Remove(key);
                return;
            }

            _dictionary[key] = value;
        }

        public ICollection<Name> Keys => _dictionary.Keys;
        public bool ContainsKey(Name key) => _dictionary.ContainsKey(key);
        public IEnumerator<KeyValuePair<Name, IPdfObject>> GetEnumerator() => _dictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
    }
}
