using ZingPDF.Syntax;

namespace ZingPDF.Parsing.Parsers.Objects.Dictionaries
{
    public interface IDictionaryIdentifier
    {
        Task<Type?> IdentifyAsync(Dictionary<string, IPdfObject> dictionary);
    }
}