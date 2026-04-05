using System.Collections;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

namespace ZingPDF.Syntax.Objects.Dictionaries
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.7 - Dictionary objects
    /// </summary>
    public class Dictionary(IPdf pdf, ObjectContext context)
        : PdfObject(context), IPdfDictionary
    {
        public Dictionary(Name? type, IPdf pdf, ObjectContext context)
            : this(pdf, context)
        {
            InnerDictionary = [];

            if (type is not null)
            {
                InnerDictionary[Constants.DictionaryKeys.Type] = type;
            }
        }

        public Dictionary(IEnumerable<KeyValuePair<string, IPdfObject>> dictionary, IPdf pdf, ObjectContext context)
            : this(pdf, context)
        {
            ArgumentNullException.ThrowIfNull(dictionary, nameof(dictionary));

            InnerDictionary = dictionary.ToDictionary();
        }

        public Dictionary(IPdfDictionary dictionary)
            : this(dictionary.InnerDictionary, dictionary.Pdf, dictionary.Context)
        {
        }

        public IPdf Pdf { get; } = pdf;
        public Dictionary<string, IPdfObject> InnerDictionary { get; } = [];

        public Name? Type => GetAs<Name>(Constants.DictionaryKeys.Type);
        public Name Subtype => GetAs<Name>(Constants.DictionaryKeys.Subtype);

        public T GetAs<T>(string key) where T : class?, IPdfObject?
            => (InnerDictionary.TryGetValue(key, out IPdfObject? value) ? value as T : null)!;

        public RequiredProperty<T> GetRequiredProperty<T>(string key) where T : class, IPdfObject
            => new(key, this, Pdf);

        public OptionalProperty<T> GetOptionalProperty<T>(string key) where T : class, IPdfObject
            => new(key, this, Pdf);

        public RequiredMultiProperty<T1, T2> GetRequiredMultiProperty<T1, T2>(string key)
            where T1 : class, IPdfObject
            where T2 : class, IPdfObject
            => new(key, this, Pdf);

        public OptionalMultiProperty<T1, T2> GetOptionalMultiProperty<T1, T2>(string key)
            where T1 : class, IPdfObject
            where T2 : class, IPdfObject
            => new(key, this, Pdf);

        public RequiredArrayOrSingle<T> GetArrayOrSingle<T>(string key) where T : class, IPdfObject
            => new(key, this, Pdf);

        public OptionalArrayOrSingle<T> GetOptionalArrayOrSingle<T>(string key) where T : class, IPdfObject
            => new(key, this, Pdf);

        public void Set<T>(string key, T? value) where T : class, IPdfObject
        {
            if (value is null)
            {
                InnerDictionary.Remove(key);
                return;
            }

            InnerDictionary[key] = value;
        }

        public void Unset(string key)
        {
            InnerDictionary.Remove(key);
        }

        public ICollection<string> Keys => InnerDictionary.Keys;

        public bool ContainsKey(string key) => InnerDictionary.ContainsKey(key);

        public IEnumerator<KeyValuePair<string, IPdfObject>> GetEnumerator() => InnerDictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteTextAsync(Constants.DictionaryStart);

            foreach (var kvp in InnerDictionary)
            {
                if (kvp.Value is null)
                {
                    continue;
                }

                await ((Name)kvp.Key).WriteAsync(stream);
                await stream.WriteWhitespaceAsync();
                await kvp.Value.WriteAsync(stream);
            }

            await stream.WriteTextAsync(Constants.DictionaryEnd);
        }

        public override object Clone()
        {
            var copy = this.ToDictionary(
                entry => entry.Key,
                entry => (IPdfObject)entry.Value.Clone()
            );

            return new Dictionary(copy, Pdf, Context);
        }
    }
}
