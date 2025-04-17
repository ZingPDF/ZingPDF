using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

namespace ZingPDF.Syntax.Objects.Dictionaries
{
    public interface IPdfDictionary : IEnumerable<KeyValuePair<Name, IPdfObject>>
    {
        Name? Type { get; }

        /// <summary>
        /// Strongly typed access to the underlying <see cref="PdfObject"/>.
        /// </summary>
        /// <remarks>
        /// Will return null if the specified key does not exist or the value is not assignable to the requested type.
        /// </remarks>
        T GetAs<T>(Name key) where T : class?, IPdfObject?;

        /// <summary>
        /// Strongly typed access to the underlying <see cref="PdfObject"/>.
        /// </summary>
        /// <remarks>
        /// Returns a <see cref="DictionaryProperty{T}"/> which can be used to access the value asynchronously.
        /// </remarks>
        DictionaryProperty<T> Get<T>(Name key) where T : class?, IPdfObject?;

        /// <summary>
        /// Strongly typed access to the underlying <see cref="PdfObject"/>.
        /// </summary>
        /// <remarks>
        /// Returns a <see cref="DictionaryMultiProperty{T1, T2}"/> which can be used to access the value asynchronously.
        /// </remarks>
        DictionaryMultiProperty<T1, T2> Get<T1, T2>(Name key)
            where T1 : class?, IPdfObject?
            where T2 : class?, IPdfObject?;

        void Set<T>(Name key, T? value) where T : class, IPdfObject;

        ICollection<Name> Keys { get; }
        bool ContainsKey(Name key);
    }
}