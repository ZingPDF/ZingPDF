using ZingPDF.Parsing.Parsers.DataStructures;
using ZingPDF.Parsing.Parsers.FileStructure;
using ZingPDF.Parsing.Parsers.FileStructure.CrossReferences;
using ZingPDF.Parsing.Parsers.Objects;
using ZingPDF.Parsing.Parsers.Objects.Dictionaries;
using ZingPDF.Parsing.Parsers.Objects.LiteralStrings;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.FileStructure;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers
{
    public class Parser
    {
        public Parser(IPdfContext pdfContext)
        {
            DocumentVersions = new DocumentVersionParser(pdfContext);
            IndirectObjects = new IndirectObjectParser(pdfContext);
            Dictionaries = new ComplexDictionaryParser(pdfContext);
            Arrays = new ArrayParser(pdfContext);
            Headers = new HeaderParser(pdfContext);
            Keywords = new KeywordParser(pdfContext);
            Comments = new CommentParser(pdfContext);
            Names = new NameParser(pdfContext);
            Booleans = new BooleanObjectParser(pdfContext);
            Numbers = new NumberParser(pdfContext);
            IndirectObjectReferences = new IndirectObjectReferenceParser(pdfContext);
            LiteralStrings = new LiteralStringParser(pdfContext);
            HexadecimalStrings = new HexadecimalStringParser(pdfContext);
            CrossReferenceTables = new CrossReferenceTableParser(pdfContext);
            CrossReferenceSections = new CrossReferenceSectionParser(pdfContext);
            CrossReferenceSectionIndexes = new CrossReferenceSectionIndexParser(pdfContext);
            CrossReferenceEntries = new CrossReferenceEntryParser(pdfContext);
            Dates = new DateParser(pdfContext);
            Trailers = new TrailerParser(pdfContext);
            ContentStreamParser = new ContentStreamParser(pdfContext);
        }

        internal DocumentVersionParser DocumentVersions { get; }
        internal IndirectObjectParser IndirectObjects { get; }
        internal ComplexDictionaryParser Dictionaries { get; }
        internal ArrayParser Arrays { get; }
        internal HeaderParser Headers { get; }
        internal KeywordParser Keywords { get; }
        internal CommentParser Comments { get; }
        internal NameParser Names { get; }
        internal BooleanObjectParser Booleans { get; }
        internal NumberParser Numbers { get; }
        internal IndirectObjectReferenceParser IndirectObjectReferences { get; }
        internal LiteralStringParser LiteralStrings { get; }
        internal HexadecimalStringParser HexadecimalStrings { get; }
        internal CrossReferenceTableParser CrossReferenceTables { get; }
        internal CrossReferenceSectionParser CrossReferenceSections { get; }
        internal CrossReferenceSectionIndexParser CrossReferenceSectionIndexes { get; }
        internal CrossReferenceEntryParser CrossReferenceEntries { get; }
        internal DateParser Dates { get; }
        internal TrailerParser Trailers { get; }
        internal ContentStreamParser ContentStreamParser { get; }

        public IObjectParser<IPdfObject> For(Type pdfObjectType)
            => GetParserForType(pdfObjectType);

        private IObjectParser<IPdfObject> GetParserForType(Type type)
        {
            return type switch
            {
                Type t when t == typeof(IndirectObject) => IndirectObjects,
                Type t when t == typeof(Dictionary) => Dictionaries,
                Type t when t == typeof(ArrayObject) => Arrays,
                Type t when t == typeof(Header) => Headers,
                Type t when t == typeof(Keyword) => Keywords,
                Type t when t == typeof(Comment) => Comments,
                Type t when t == typeof(Name) => Names,
                Type t when t == typeof(BooleanObject) => Booleans,
                Type t when t == typeof(Number) => Numbers,
                Type t when t == typeof(IndirectObjectReference) => IndirectObjectReferences,
                Type t when t == typeof(LiteralString) => LiteralStrings,
                Type t when t == typeof(HexadecimalString) => HexadecimalStrings,
                Type t when t == typeof(CrossReferenceTable) => CrossReferenceTables,
                Type t when t == typeof(CrossReferenceSection) => CrossReferenceSections,
                Type t when t == typeof(CrossReferenceSectionIndex) => CrossReferenceSectionIndexes,
                Type t when t == typeof(CrossReferenceEntry) => CrossReferenceEntries,
                Type t when t == typeof(Date) => Dates,
                Type t when t == typeof(Trailer) => Trailers,
                _ => throw new ParserException()
            };
        }
    }
}
