namespace ZingPDF.Syntax.Objects.Dictionaries
{
    public interface IPdfDictionary : IDictionary<Name, IPdfObject>
    {
        Name? Type { get; }
        void Set<T>(Name key, T? value) where T : class, IPdfObject;
    }
}