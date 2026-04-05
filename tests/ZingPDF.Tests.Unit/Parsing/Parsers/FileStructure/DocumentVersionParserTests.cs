using FakeItEasy;
using FluentAssertions;
using MorseCode.ITask;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Extensions;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Parsing.Parsers.FileStructure;
using ZingPDF.Parsing.Parsers.FileStructure.CrossReferences;
using ZingPDF.Parsing.Parsers.Objects;
using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;
using Xunit;

namespace ZingPDF.Tests.Unit.Parsing.Parsers.FileStructure;

public class DocumentVersionParserTests
{
    [Fact]
    public async Task ParseAtAsync_WhenOffsetPointsInsideXrefSubsection_FallsBackToNearbyXrefTable()
    {
        using var stream = "xref\n5 1\n0000000000 00000 n\ntrailer\n<</Size 6>>\nstartxref\n0\n%%EOF".ToStream();
        var tableParser = A.Fake<IParser<CrossReferenceTable>>();
        var trailerParser = A.Fake<IParser<Trailer>>();
        var xrefStreamParser = A.Fake<IParser<StreamObject<CrossReferenceStreamDictionary>>>();

        var context = ObjectContext.WithOrigin(ObjectOrigin.None);
        var table = new CrossReferenceTable([new CrossReferenceSection(5, [new CrossReferenceEntry(0, 0, true, false, context)], context)], context);
        var trailerDictionary = new TrailerDictionary(new Dictionary(
            [
                new KeyValuePair<string, IPdfObject>(Constants.DictionaryKeys.Trailer.Size, (Number)6)
            ],
            A.Dummy<IPdf>(),
            context));
        var trailer = new Trailer(trailerDictionary, 0, context);

        A.CallTo(() => tableParser.ParseAsync(A<Stream>._, A<ObjectContext>._)).Returns(Task.FromResult(table).AsITask());
        A.CallTo(() => trailerParser.ParseAsync(A<Stream>._, A<ObjectContext>._)).Returns(Task.FromResult(trailer).AsITask());

        var parser = new DocumentVersionParser(
            new KeywordParser(),
            new NumberParser(),
            tableParser,
            trailerParser,
            xrefStreamParser);

        VersionInformation version = await parser.ParseAtAsync(stream, 5);

        version.CrossReferenceTable.Should().BeSameAs(table);
        version.Trailer.Should().BeSameAs(trailer);
        version.CrossReferenceStream.Should().BeNull();

        A.CallTo(() => tableParser.ParseAsync(A<Stream>._, A<ObjectContext>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => xrefStreamParser.ParseAsync(A<Stream>._, A<ObjectContext>._)).MustNotHaveHappened();
    }
}
