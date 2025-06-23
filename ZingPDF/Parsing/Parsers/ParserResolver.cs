using System.Collections.Concurrent;
using ZingPDF.Syntax;

namespace ZingPDF.Parsing.Parsers;

public class ParserResolver : IParserResolver
{
    private readonly ConcurrentDictionary<Type, IParser<IPdfObject>> _parserCache = new();
    private readonly IServiceProvider _serviceProvider;

    public ParserResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IParser<T> GetParser<T>() where T : IPdfObject => (IParser<T>)GetParserFor(typeof(T));

    public IParser<IPdfObject> GetParserFor(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return _parserCache.GetOrAdd(type, ResolveParser);
    }

    private IParser<IPdfObject> ResolveParser(Type type)
    {
        var parserType = typeof(IParser<>).MakeGenericType(type);
        var parser = _serviceProvider.GetService(parserType);

        if (parser is IParser<IPdfObject> covariantParser)
            return covariantParser;

        throw new InvalidOperationException($"No parser registered for type {type.FullName}");
    }

}
