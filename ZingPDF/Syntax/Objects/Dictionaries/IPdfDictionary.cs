using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

namespace ZingPDF.Syntax.Objects.Dictionaries
{
    public interface IPdfDictionary : IEnumerable<KeyValuePair<Name, IPdfObject>>
    {
        Name? Type { get; }

        T GetAs<T>(Name key) where T : class?, IPdfObject?;

        RequiredProperty<T> GetRequiredProperty<T>(Name key) where T : class, IPdfObject;

        OptionalProperty<T> GetOptionalProperty<T>(Name key) where T : class, IPdfObject;

        OptionalMultiProperty<T1, T2> GetOptionalMultiProperty<T1, T2>(Name key)
            where T1 : class, IPdfObject
            where T2 : class, IPdfObject;

        RequiredMultiProperty<T1, T2> GetRequiredMultiProperty<T1, T2>(Name key)
            where T1 : class, IPdfObject
            where T2 : class, IPdfObject;

        RequiredArrayOrSingle<T> GetArrayOrSingle<T>(Name key) where T : class, IPdfObject;

        OptionalArrayOrSingle<T> GetOptionalArrayOrSingle<T>(Name key) where T : class, IPdfObject;

        void Set<T>(Name key, T? value) where T : class, IPdfObject;

        ICollection<Name> Keys { get; }
        bool ContainsKey(Name key);
    }
}