using ZingPDF.Syntax;

namespace ZingPDF.Parsing.Parsers;

public interface IParserResolver
{
    IParser<T> GetParser<T>() where T : IPdfObject;
    IParser<IPdfObject> GetParserFor(Type type);
}
