using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.DataStructures;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;
using ZingPdf.Core.Parsing.DataStructureParsers;
using ZingPdf.Core.Parsing.ObjectGroupParsers;
using ZingPdf.Core.Parsing.ObjectGroupParsers.CrossReferenceTableParsing;
using ZingPdf.Core.Parsing.PrimitiveParsers;

namespace ZingPdf.Core.Parsing
{
    internal static class Parser
    {
        private static readonly PdfObjectGroupParser _pdfObjectGroupParser = new();
        private static readonly IndirectObjectParser _indirectObjectParser = new();
        private static readonly KeywordParser _keywordParser = new();
        private static readonly NameParser _nameParser = new();
        private static readonly DictionaryParser _dictionaryParser = new();
        private static readonly ArrayParser _arrayParser = new();
        private static readonly BooleanObjectParser _booleanObjectParser = new();
        private static readonly IntegerParser _integerParser = new();
        private static readonly RealNumberParser _realNumberParser = new();
        private static readonly IndirectObjectReferenceParser _indirectObjectReferenceParser = new();
        private static readonly LiteralStringParser _literalStringParser = new();
        private static readonly HexadecimalStringParser _hexadecimalStringParser = new();
        private static readonly CrossReferenceTableParser _xrefTableParser = new();
        private static readonly CrossReferenceSectionParser _xrefSectionParser = new();
        private static readonly CrossReferenceSectionIndexParser _xrefSectionIndexParser = new();
        private static readonly CrossReferenceEntryParser _xrefEntryParser = new();
        private static readonly DateParser _dateParser = new();
        private static readonly StreamObjectParser _streamParser = new();
        private static readonly TrailerParser _trailerParser = new();

        public static IPdfObjectParser<PdfObject> For(Type pdfObjectType)
            => GetParserForType(pdfObjectType);

        public static IPdfObjectParser<T> For<T>() where T : PdfObject
            => (IPdfObjectParser<T>)GetParserForType(typeof(T));

        private static IPdfObjectParser<PdfObject> GetParserForType(Type type)
        {
            return type switch
            {
                Type t when t == typeof(PdfObjectGroup) => _pdfObjectGroupParser,
                Type t when t == typeof(IndirectObject) => _indirectObjectParser,
                Type t when t == typeof(Keyword) => _keywordParser,
                Type t when t == typeof(Name) => _nameParser,
                Type t when t == typeof(Dictionary) => _dictionaryParser,
                Type t when t == typeof(ArrayObject) => _arrayParser,
                Type t when t == typeof(BooleanObject) => _booleanObjectParser,
                Type t when t == typeof(Integer) => _integerParser,
                Type t when t == typeof(RealNumber) => _realNumberParser,
                Type t when t == typeof(IndirectObjectReference) => _indirectObjectReferenceParser,
                Type t when t == typeof(LiteralString) => _literalStringParser,
                Type t when t == typeof(HexadecimalString) => _hexadecimalStringParser,
                Type t when t == typeof(CrossReferenceTable) => _xrefTableParser,
                Type t when t == typeof(CrossReferenceSection) => _xrefSectionParser,
                Type t when t == typeof(CrossReferenceSectionIndex) => _xrefSectionIndexParser,
                Type t when t == typeof(CrossReferenceEntry) => _xrefEntryParser,
                Type t when t == typeof(Date) => _dateParser,
                Type t when t == typeof(StreamObject) => _streamParser,
                Type t when t == typeof(Trailer) => _trailerParser,
                _ => throw new ParserException()
            };
        }
    }
}
