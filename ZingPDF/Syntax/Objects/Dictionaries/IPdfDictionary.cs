namespace ZingPDF.Syntax.Objects.Dictionaries
{
    public interface IPdfDictionary : IDictionary<Name, IPdfObject>
    {
        Name? Type { get; }
    }
}