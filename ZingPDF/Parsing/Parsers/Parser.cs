using ZingPDF.Parsing.Parsers.DataStructures;
using ZingPDF.Parsing.Parsers.FileStructure;
using ZingPDF.Parsing.Parsers.FileStructure.CrossReferences;
using ZingPDF.Parsing.Parsers.Objects;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.FileStructure;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers
{
    internal static class Parser
    {
        private static readonly PdfObjectGroupParser _pdfObjectGroupParser = new();
        private static readonly HeaderParser _headerParser = new();
        private static readonly KeywordParser _keywordParser = new();
        private static readonly CommentParser _commentParser = new();
        private static readonly NameParser _nameParser = new();
        private static readonly DictionaryParser _dictionaryParser = new();
        private static readonly ArrayParser _arrayParser = new();
        private static readonly BooleanObjectParser _booleanObjectParser = new();
        private static readonly NumberParser _numberParser = new();
        private static readonly IndirectObjectReferenceParser _indirectObjectReferenceParser = new();
        private static readonly LiteralStringParser _literalStringParser = new();
        private static readonly HexadecimalStringParser _hexadecimalStringParser = new();
        private static readonly CrossReferenceTableParser _xrefTableParser = new();
        private static readonly CrossReferenceSectionParser _xrefSectionParser = new();
        private static readonly CrossReferenceSectionIndexParser _xrefSectionIndexParser = new();
        private static readonly CrossReferenceEntryParser _xrefEntryParser = new();
        private static readonly DateParser _dateParser = new();
        private static readonly TrailerParser _trailerParser = new();

        internal static PdfObjectGroupParser PdfObjectGroups => _pdfObjectGroupParser;
        internal static HeaderParser Headers => _headerParser;
        internal static KeywordParser Keywords => _keywordParser;
        internal static CommentParser Comments => _commentParser;
        internal static NameParser Names => _nameParser;
        internal static DictionaryParser Dictionaries => _dictionaryParser;
        internal static ArrayParser Arrays => _arrayParser;
        internal static BooleanObjectParser Booleans => _booleanObjectParser;
        internal static NumberParser Numbers => _numberParser;
        internal static IndirectObjectReferenceParser IndirectObjectReferences => _indirectObjectReferenceParser;
        internal static LiteralStringParser LiteralStrings => _literalStringParser;
        internal static HexadecimalStringParser HexadecimalStrings => _hexadecimalStringParser;
        internal static CrossReferenceTableParser XrefTables => _xrefTableParser;
        internal static CrossReferenceSectionParser XrefSections => _xrefSectionParser;
        internal static CrossReferenceSectionIndexParser XrefSectionIndexes => _xrefSectionIndexParser;
        internal static CrossReferenceEntryParser XrefEntries => _xrefEntryParser;
        internal static DateParser Dates => _dateParser;
        internal static TrailerParser Trailers => _trailerParser;

        public static IObjectParser<IPdfObject> For(Type pdfObjectType, IIndirectObjectDictionary? indirectObjectDictionary = null)
            => GetParserForType(pdfObjectType, indirectObjectDictionary);

        public static IObjectParser<T> For<T>(IIndirectObjectDictionary? indirectObjectDictionary = null) where T : IPdfObject
            => (IObjectParser<T>)GetParserForType(typeof(T), indirectObjectDictionary);

        private static IObjectParser<IPdfObject> GetParserForType(Type type, IIndirectObjectDictionary? indirectObjectDictionary)
        {
            return type switch
            {
                Type t when t == typeof(IndirectObject) => new IndirectObjectParser(indirectObjectDictionary!),
                Type t when t == typeof(PdfObjectGroup) => _pdfObjectGroupParser,
                Type t when t == typeof(Header) => _headerParser,
                Type t when t == typeof(Keyword) => _keywordParser,
                Type t when t == typeof(Comment) => _commentParser,
                Type t when t == typeof(Name) => _nameParser,
                Type t when t == typeof(Dictionary) => _dictionaryParser,
                Type t when t == typeof(ArrayObject) => _arrayParser,
                Type t when t == typeof(BooleanObject) => _booleanObjectParser,
                Type t when t == typeof(Number) => _numberParser,
                Type t when t == typeof(IndirectObjectReference) => _indirectObjectReferenceParser,
                Type t when t == typeof(LiteralString) => _literalStringParser,
                Type t when t == typeof(HexadecimalString) => _hexadecimalStringParser,
                Type t when t == typeof(CrossReferenceTable) => _xrefTableParser,
                Type t when t == typeof(CrossReferenceSection) => _xrefSectionParser,
                Type t when t == typeof(CrossReferenceSectionIndex) => _xrefSectionIndexParser,
                Type t when t == typeof(CrossReferenceEntry) => _xrefEntryParser,
                Type t when t == typeof(Date) => _dateParser,
                Type t when t == typeof(StreamObject<IStreamDictionary>) => new StreamObjectParser(indirectObjectDictionary!),
                Type t when t == typeof(Trailer) => _trailerParser,
                _ => throw new ParserException()
            };
        }
    }
}
