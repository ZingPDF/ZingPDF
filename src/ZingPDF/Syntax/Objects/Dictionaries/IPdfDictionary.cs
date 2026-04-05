using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

namespace ZingPDF.Syntax.Objects.Dictionaries
{
    /// <summary>
    /// To simplify entry retrieval, we internally use string as a key instead of a Name object. 
    /// </summary>
    public interface IPdfDictionary : IPdfObject, IEnumerable<KeyValuePair<string, IPdfObject>>
    {
        IPdf Pdf { get; }
        Dictionary<string, IPdfObject> InnerDictionary { get; }

        Name? Type { get; }

        T GetAs<T>(string key) where T : class?, IPdfObject?;

        RequiredProperty<T> GetRequiredProperty<T>(string key) where T : class, IPdfObject;

        OptionalProperty<T> GetOptionalProperty<T>(string key) where T : class, IPdfObject;

        OptionalMultiProperty<T1, T2> GetOptionalMultiProperty<T1, T2>(string key)
            where T1 : class, IPdfObject
            where T2 : class, IPdfObject;

        RequiredMultiProperty<T1, T2> GetRequiredMultiProperty<T1, T2>(string key)
            where T1 : class, IPdfObject
            where T2 : class, IPdfObject;

        RequiredArrayOrSingle<T> GetArrayOrSingle<T>(string key) where T : class, IPdfObject;

        OptionalArrayOrSingle<T> GetOptionalArrayOrSingle<T>(string key) where T : class, IPdfObject;

        void Set<T>(string key, T? value) where T : class, IPdfObject;
        void Unset(string key);

        ICollection<string> Keys { get; }

        bool ContainsKey(string key);
    }
}