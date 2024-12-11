using FakeItEasy;
using FluentAssertions;
using System.Text;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects;

public class DictionaryParserTests
{
    [Fact]
    public async Task ParseEmptyAsync()
    {
        using var input = "<< >>".ToStream();

        var output = await new DictionaryParser().ParseAsync(input, A.Dummy<IIndirectObjectDictionary>());

        output.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseSimpleNestedDictionary_CorrectCounts()
    {
        var contentString = "<</Resources <<>>>>";

        using var input = contentString.ToStream();

        var output = await new DictionaryParser().ParseAsync(input, A.Dummy<IIndirectObjectDictionary>());

        output.Should().NotBeNull().And.HaveCount(1);

        var nestedDictionary = output.Get<Dictionary>("Resources");

        nestedDictionary.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task ParseSimpleNestedDictionary_CorrectStreamPosition()
    {
        var contentString = "<</Resources <<>>>>";

        using var input = contentString.ToStream();

        var output = await new DictionaryParser().ParseAsync(input, A.Dummy<IIndirectObjectDictionary>());

        input.Position.Should().Be(
            contentString.Length,
            because: "the parser should move the stream past the dictionary-end delimiter"
            );
    }

    [Fact]
    public async Task ParseSimpleNestedDictionary_WithWhitespace_CorrectCounts()
    {
        var contentString = "<< /Resources << >> >>";

        using var input = contentString.ToStream();

        var output = await new DictionaryParser().ParseAsync(input, A.Dummy<IIndirectObjectDictionary>());

        output.Should().NotBeNull().And.HaveCount(1);

        var nestedDictionary = output.Get<Dictionary>("Resources");

        nestedDictionary.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task ParseSimpleNestedDictionary_WithWhitepsace_CorrectStreamPosition()
    {
        var contentString = "<< /Resources << >> >>";

        using var input = contentString.ToStream();

        var output = await new DictionaryParser().ParseAsync(input, A.Dummy<IIndirectObjectDictionary>());

        input.Position.Should().Be(
            contentString.Length,
            because: "the parser should move the stream past the dictionary-end delimiter"
            );
    }

    // TODO: what is this really testing? Make it assert one thing and remove as many interdependencies as possible
    [Fact]
    public async Task ParseComplexCatalogDictionary()
    {
        var contentString = "" +
            "<<" +
            "/AcroForm 90825 0 R" +
            "/Lang(en)" +
            "/MarkInfo<</Marked true>>" +
            "/Metadata 3633 0 R" +
            "/Names 90826 0 R" +
            "/OCProperties" +
                "<</D" +
                    "<</AS[" +
                        "<</Category[/Print]/Event/Print/OCGs[90827 0 R]>>" +
                        "<</Category[/Export]/Event/Export/OCGs[90827 0 R]>>" +
                        "<</Category[/Zoom]/Event/View/OCGs[90827 0 R]>>" +
                        "]" +
                        "/OFF[90827 0 R]" +
                        "/Order[[(2020-12-03_ISO_32000-2-final.pdf)90827 0 R]]" +
                        "/RBGroups[]" +
                    ">>" +
                "/OCGs[90827 0 R]" +
                ">>" +
            "/Outlines 90840 0 R" +
            "/PageMode/UseOutlines" +
            "/Pages 90540 0 R" +
            "/StructTreeRoot 4931 0 R" +
            "/Type/Catalog" +
            "/ViewerPreferences<</DisplayDocTitle true>>" +
            ">>";

        using var input = contentString.ToStream();

        var output = await new DictionaryParser().ParseAsync(input, A.Dummy<IIndirectObjectDictionary>());
    }

    [Fact]
    public async Task DelimiterAtStreamBufferBoundary()
    {
        // While the dictionary parser uses a 1024 buffer, the following string representation
        // of a dictionary has a delimiter which straddles the buffer boundary.

        var contentString = "\r<<" +
            "/ArtBox[0.0 0.0 841.89 595.276]" +
            "/BleedBox[0.0 0.0 841.89 595.276]" +
            "/Contents 113 0 R" +
            "/CropBox[0.0 0.0 841.89 595.276]" +
            "/MediaBox[0.0 0.0 841.89 595.276]" +
            "/Parent 240 0 R" +
            "/Resources<</ColorSpace<</CS0 249 0 R>>" +
                "/ExtGState<</GS0 251 0 R/GS1 252 0 R>>" +
                "/Font<</T1_0 248 0 R/T1_1 247 0 R>>" +
                "/ProcSet[/PDF/Text/ImageC]" +
                "/XObject<</Im0 114 0 R/Im1 115 0 R>>" +
                ">>" +
            "/Rotate 0" +
            "/TrimBox[0.0 0.0 841.89 595.276]" +
            "/Type/Page/PieceInfo<<" +
                "/InDesign<<" +
                    "/DocumentID<FEFF0078006D0070002E006400690064003A00630036003800330035003900300034002D0032006500660035002D0034003400300036002D0061003700310036002D006600640033006100360035006100370065003700310065>" +
                    "/LastModified<FEFF0044003A00320030003200340031003100310038003000320033003600340038005A>" +
                    "/NumberofPages 1" +
                    "/OriginalDocumentID<FEFF0078006D0070002E006400690064003A00630036003800330035003900300034002D0032006500660035002D0034003400300036002D0061003700310036002D006600640033006100360035006100370065003700310065>" +
                    "/PageTransformationMatrixList<</0[1.0 0.0 0.0 1.0 0.0 0.0]>>" +
                    "/PageUIDList<</0 2683>>" +
                    "/PageWidthList<</0 841.89>>" +
                    ">>" +
                ">>" +
            ">>";

        using var input = contentString.ToStream();

        var output = await new DictionaryParser().ParseAsync(input, A.Dummy<IIndirectObjectDictionary>());

        input.Position.Should().Be(Encoding.UTF8.GetByteCount(contentString));

        output.Count.Should().Be(11);
    }

    [Fact]
    public async Task ParseThis()
    {
        var contentString = "\r<<" +
            "/ArtBox[0.0 0.0 841.89 595.276]" +
            "/BleedBox[0.0 0.0 841.89 595.276]" +
            "/Contents 2 0 R" +
            "/CropBox[0.0 0.0 841.89 595.276]" +
            "/Group 9 0 R" +
            "/MediaBox[0.0 0.0 841.89 595.276]" +
            "/Parent 237 0 R" +
            "/Resources<<" +
                "/ColorSpace<</CS0 249 0 R>>" +
                "/ExtGState<</GS0 251 0 R/GS1 252 0 R>>" +
                "/Font<</T1_0 247 0 R/T1_1 248 0 R>>" +
                "/ProcSet[/PDF/Text/ImageC]" +
                "/XObject<</Im0 6 0 R/Im1 7 0 R/Im2 8 0 R>>" +
            ">>" +
            "/Rotate 0" +
            "/TrimBox[0.0 0.0 841.89 595.276]" +
            "/Type/Page" +
            "/PieceInfo<<" +
                "/InDesign<<" +
                    "/DocumentID<FEFF0078006D0070002E006400690064003A00630036003800330035003900300034002D0032006500660035002D0034003400300036002D0061003700310036002D006600640033006100360035006100370065003700310065>" +
                    "/LastModified<FEFF0044003A00320030003200340031003100310038003000320033003600310033005A>" +
                    "/NumberofPages 1" +
                    "/OriginalDocumentID<FEFF0078006D0070002E006400690064003A00630036003800330035003900300034002D0032006500660035002D0034003400300036002D0061003700310036002D006600640033006100360035006100370065003700310065>" +
                    "/PageTransformationMatrixList<</0[1.0 0.0 0.0 1.0 0.0 0.0]>>" +
                    "/PageUIDList<</0 406>>" +
                    "/PageWidthList<</0 841.89>>" +
                    ">>" +
                ">>" +
            ">>";

        using var input = contentString.ToStream();

        var output = await new DictionaryParser().ParseAsync(input, A.Dummy<IIndirectObjectDictionary>());

        input.Position.Should().Be(Encoding.UTF8.GetByteCount(contentString));

        output.Count.Should().Be(12);
    }

    [Fact]
    public async Task DictionaryEndsWithMultipleDelimiters()
    {
        var contentString = "<</PieceInfo<</InDesign<<>>>>>>";

        using var input = contentString.ToStream();

        var output = await new DictionaryParser().ParseAsync(input, A.Dummy<IIndirectObjectDictionary>());

        input.Position.Should().Be(Encoding.UTF8.GetByteCount(contentString));

        output.Count.Should().Be(1);
    }

    [Fact]
    public async Task ParseSimpleDictionary_WithWindowsLineEndings_CorrectFields()
    {
        var contentString = "<<\r\n" +
            "/Type /Page\r\n" +
            "/Other /Test\r\n" +
            ">>";

        using var input = contentString.ToStream();

        var output = await new DictionaryParser().ParseAsync(input, A.Dummy<IIndirectObjectDictionary>());

        output.Get<Name>("Type")!.Value.Should().Be("Page");
        output.Get<Name>("Other")!.Value.Should().Be("Test");

        input.Position.Should().Be(Encoding.UTF8.GetByteCount(contentString));
    }

    [Fact]
    public async Task ParseCompactDictionary()
    {
        var contentString = "<<" +
            "/DocumentID<FEFF0078006D0070002E006400690064003A00630036003800330035003900300034002D0032006500660035002D0034003400300036002D0061003700310036002D006600640033006100360035006100370065003700310065>" +
            "/LastModified<FEFF0044003A00320030003200340031003100310038003000320033003600310033005A>" +
            "/NumberofPages 1" +
            "/OriginalDocumentID<FEFF0078006D0070002E006400690064003A00630036003800330035003900300034002D0032006500660035002D0034003400300036002D0061003700310036002D006600640033006100360035006100370065003700310065>" +
            "/PageTransformationMatrixList<</0[1.0 0.0 0.0 1.0 0.0 0.0]>>" +
            "/PageUIDList<</0 406>>" +
            "/PageWidthList<</0 841.89>>" +
            ">>";

        using var input = contentString.ToStream();

        var output = await new DictionaryParser().ParseAsync(input, A.Dummy<IIndirectObjectDictionary>());

        input.Position.Should().Be(Encoding.UTF8.GetByteCount(contentString));
    }

    [Fact]
    public async Task ParseSimpleDictionary_WithWindowsLineEndings_CorrectStreamPosition()
    {
        var contentString = "<<\r\n" +
            "/Type /Page\r\n" +
            "/Other /Test\r\n" +
            ">>";

        using var input = contentString.ToStream();

        var output = await new DictionaryParser().ParseAsync(input, A.Dummy<IIndirectObjectDictionary>());

        input.Position.Should().Be(
            Encoding.UTF8.GetByteCount(contentString),
            because: "the parser should move the stream past the dictionary-end delimiter"
            );
    }

    [Fact]
    public async Task ParseSimpleDictionary_WithUnixLineEndings_CorrectFields()
    {
        var contentString = "<<\n" +
            "/Type /Page\n" +
            "/Other /Test\n" +
            ">>";

        using var input = contentString.ToStream();

        var output = await new DictionaryParser().ParseAsync(input, A.Dummy<IIndirectObjectDictionary>());

        output.Get<Name>("Type")!.Value.Should().Be("Page");
        output.Get<Name>("Other")!.Value.Should().Be("Test");

        input.Position.Should().Be(Encoding.UTF8.GetByteCount(contentString));
    }

    [Fact]
    public async Task ParseSimpleDictionary_WithUnixLineEndings_CorrectStreamPosition()
    {
        var contentString = "<<\n" +
            "/Type /Page\n" +
            "/Other /Test\n" +
            ">>";

        using var input = contentString.ToStream();

        var output = await new DictionaryParser().ParseAsync(input, A.Dummy<IIndirectObjectDictionary>());

        input.Position.Should().Be(
            Encoding.UTF8.GetByteCount(contentString),
            because: "the parser should move the stream past the dictionary-end delimiter"
            );
    }
}
