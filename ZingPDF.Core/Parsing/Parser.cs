using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Parsing.ObjectParsers;
using ZingPdf.Core.Parsing.PrimitiveParsers;

namespace ZingPdf.Core.Parsing
{
    internal static class Parser
    {
        private static readonly IPdfObjectParser<Name> _nameParser = new NameParser();
        private static readonly IPdfObjectParser<Dictionary> _dictionaryParser = new DictionaryParser();
        private static readonly IPdfObjectParser<Trailer> _trailerParser = new TrailerParser();
        private static readonly IPdfObjectParser<Integer> _integerParser = new IntegerParser();
        private static readonly IPdfObjectParser<IndirectObjectReference> _indirectObjectReferenceParser = new IndirectObjectReferenceParser();

        public static IPdfObjectParser<PdfObject> For(Type pdfObjectType)
            => GetParserForType(pdfObjectType);

        public static IPdfObjectParser<T> For<T>() where T : PdfObject
            => (IPdfObjectParser<T>)GetParserForType(typeof(T));

        private static IPdfObjectParser<PdfObject> GetParserForType(Type type)
        {
            return type switch
            {
                Type t when t == typeof(Name) => _nameParser,
                Type t when t == typeof(Dictionary) => _dictionaryParser,
                Type t when t == typeof(Trailer) => _trailerParser,
                Type t when t == typeof(Integer) => _integerParser,
                Type t when t == typeof(IndirectObjectReference) => _indirectObjectReferenceParser,
                _ => throw new ParserException()
            };
        }
    }
}
