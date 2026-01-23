using Microsoft.Extensions.DependencyInjection;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Parsing;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Parsing.Parsers.DataStructures;
using ZingPDF.Parsing.Parsers.FileStructure;
using ZingPDF.Parsing.Parsers.FileStructure.CrossReferences;
using ZingPDF.Parsing.Parsers.Objects;
using ZingPDF.Parsing.Parsers.Objects.Dictionaries;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Encryption;
using ZingPDF.Syntax.FileStructure;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Extensions;

public static class PdfServiceCollectionExtensions
{
    public static IServiceCollection AddContext(this IServiceCollection services, IPdf pdf)
    {
        services
            .AddScoped((s) => pdf)
            .AddScoped<IPdfObjectCollection, PdfObjectCollection>()
            .AddScoped<IPdfEncryptionProvider, PdfEncryptionProvider>();

        return services;
    }

    public static IServiceCollection AddTextExtractor(this IServiceCollection services)
    {
        services.AddScoped<ITextExtractor, TextExtractor>();

        return services;
    }

    public static IServiceCollection AddParsers(this IServiceCollection services)
    {
        services.AddScoped<IDictionaryIdentifier, DictionaryIdentifier>();
        services.AddScoped<ITokenTypeIdentifier, TokenTypeIdentifier>();

        services.AddScoped<IDocumentVersionParser, DocumentVersionParser>();

        services.AddScoped<IParser<PdfObjectGroup>, PdfObjectGroupParser>();
        services.AddScoped<IParser<IndirectObject>, IndirectObjectParser>();
        services.AddScoped<IParser<Dictionary>, StandardDictionaryParser>();
        services.AddScoped<IParser<ArrayObject>, ArrayParser>();
        services.AddScoped<IParser<Header>, HeaderParser>();
        services.AddScoped<IParser<Keyword>, KeywordParser>();
        services.AddScoped<IParser<Comment>, CommentParser>();
        services.AddScoped<IParser<Name>, NameParser>();
        services.AddScoped<IParser<BooleanObject>, BooleanObjectParser>();
        services.AddScoped<IParser<Number>, NumberParser>();
        services.AddScoped<IParser<IndirectObjectReference>, IndirectObjectReferenceParser>();
        services.AddScoped<IParser<PdfString>, PdfStringParser>();
        services.AddScoped<IParser<CrossReferenceTable>, CrossReferenceTableParser>();
        services.AddScoped<IParser<CrossReferenceSection>, CrossReferenceSectionParser>();
        services.AddScoped<IParser<CrossReferenceSectionIndex>, CrossReferenceSectionIndexParser>();
        services.AddScoped<IParser<CrossReferenceEntry>, CrossReferenceEntryParser>();
        services.AddScoped<IParser<StreamObject<CrossReferenceStreamDictionary>>, CrossReferenceStreamParser>();
        services.AddScoped<IParser<Date>, DateParser>();
        services.AddScoped<IParser<Trailer>, TrailerParser>();
        services.AddScoped<IParser<ContentStream>, ContentStreamParser>();

        services.AddScoped<IParserResolver, ParserResolver>();

        return services;
    }
}
