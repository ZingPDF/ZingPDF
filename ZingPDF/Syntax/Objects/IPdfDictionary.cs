namespace ZingPDF.Syntax.Objects
{
    public interface IPdfDictionary : IDictionary<Name, IPdfObject>
    {
        Name? Type { get; }

        T? Get<T>(Name key) where T : class, IPdfObject;
    }
}