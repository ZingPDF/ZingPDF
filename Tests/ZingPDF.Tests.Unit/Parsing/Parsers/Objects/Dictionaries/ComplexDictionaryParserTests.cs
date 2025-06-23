using FakeItEasy;
using FakeItEasy.Configuration;
using FluentAssertions;
using MorseCode.ITask;
using Nito.AsyncEx;
using System.Text;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Parsing.Parsers.Objects.Dictionaries;

public class ComplexDictionaryParserTests
{
    [Theory]
    [InlineData("<< >>")]
    [InlineData("<<>>")]
    public async Task ParseEmptyAsync(string inputString)
    {
        using var input = inputString.ToStream();

        var output = await new ComplexDictionaryParser(
            A.Dummy<IPdfObjectCollection>(),
            A.Dummy<IParser<PdfObjectGroup>>(),
            A.Dummy<IDictionaryIdentifier>()
            )
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

        output.Should().BeEmpty();

        input.Position.Should().Be(inputString.Length);
    }

    [Fact]
    public async Task ParseSimpleNestedDictionary_CorrectCounts()
    {
        var contentString = "<</Resources <<>>>>";

        using var input = contentString.ToStream();
        var pdfObjects = A.Fake<IPdfContext>();

        A.CallTo((object)(() => pdfObjects.Parsers.Names.ParseAsync(A<Stream>.Ignored, A<ParseContext>.Ignored)))
            .Returns(Task.FromResult((Name)"Something").AsITask());

        var output = await new ComplexDictionaryParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

        output.Should().NotBeNull().And.HaveCount(1);

        var nestedDictionary = output.GetAs<Dictionary>("Resources");

        nestedDictionary.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task ParseSimpleNestedDictionary_CorrectStreamPosition()
    {
        var contentString = "<</Resources <<>>>>";

        using var input = contentString.ToStream();

        var output = await new ComplexDictionaryParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

        input.Should().HavePosition(
            contentString.Length,
            because: "the parser should move the stream past the dictionary-end delimiter"
            );
    }

    [Fact]
    public async Task ParseSimpleNestedDictionary_WithWhitespace_CorrectCounts()
    {
        var contentString = "<< /Resources << >> >>";

        using var input = contentString.ToStream();

        var output = await new ComplexDictionaryParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

        output.Should().NotBeNull().And.HaveCount(1);

        var nestedDictionary = output.GetAs<Dictionary>("Resources");

        nestedDictionary.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task ParseSimpleNestedDictionary_WithWhitepsace_CorrectStreamPosition()
    {
        var contentString = "<< /Resources << >> >>";

        using var input = contentString.ToStream();

        var output = await new ComplexDictionaryParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

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

        var output = await new ComplexDictionaryParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));
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

        var output = await new ComplexDictionaryParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

        input.Position.Should().Be(Encoding.UTF8.GetByteCount(contentString));

        output.Count().Should().Be(11);
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

        var output = await new ComplexDictionaryParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

        input.Position.Should().Be(Encoding.UTF8.GetByteCount(contentString));

        output.Count().Should().Be(12);
    }

    [Fact]
    public async Task DictionaryEndsWithMultipleDelimiters()
    {
        var contentString = "<</PieceInfo<</InDesign<<>>>>>>";

        using var input = contentString.ToStream();

        var output = await new ComplexDictionaryParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

        input.Position.Should().Be(Encoding.UTF8.GetByteCount(contentString));

        output.Count().Should().Be(1);
    }

    [Fact]
    public async Task ParseSimpleDictionary_WithWindowsLineEndings_CorrectFields()
    {
        var contentString = "<<\r\n" +
            "/Type /Page\r\n" +
            "/Other /Test\r\n" +
            ">>";

        using var input = contentString.ToStream();

        var output = await new ComplexDictionaryParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

        var type = output.Type;
        var other = output.GetAs<Name>("Other");

        type.Should().NotBeNull();
        type!.Value.Should().Be("Page");

        other.Should().NotBeNull();
        other!.Value.Should().Be("Test");

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

        var output = await new ComplexDictionaryParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

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

        var output = await new ComplexDictionaryParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

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

        var output = await new ComplexDictionaryParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

        var type = output.Type;
        var other = output.GetAs<Name>("Other");

        type.Should().NotBeNull();
        type!.Value.Should().Be("Page");

        other.Should().NotBeNull();
        other!.Value.Should().Be("Test");

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

        var output = await new ComplexDictionaryParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

        input.Position.Should().Be(
            Encoding.UTF8.GetByteCount(contentString),
            because: "the parser should move the stream past the dictionary-end delimiter"
            );
    }

    [Fact]
    public async Task ParseFieldDictionary()
    {
        const int parentIndex = 572;
        const int parentGenerationNumber = 0;

        var pdfObjects = A.Fake<IPdfContext>();

        var objects = new Dictionary(
            new Dictionary<string, IPdfObject>
            {
                [Constants.DictionaryKeys.Field.FT] = (Name)"Tx"
            },
            pdfObjects,
            ObjectOrigin.None
        );

        A.CallTo(() => pdfObjects.Objects.GetAsync<Dictionary>(new IndirectObjectReference(parentIndex, parentGenerationNumber)))
            .Returns(objects);

        var contentString = "<<" +
            "/AP<</N 1794 0 R>>" +
            "/BS<</S/S/W 1>>" +
            "/DA(/Montserrat-Regular 9 Tf 0 g)" +
            "/F 4" +
            "/Ff 67108866" +
            "/I[0]" +
            "/MK<<>>" +
            "/Opt[(Please Select:)(1800 010 091 - tprecoveries@autogeneral.com.au)(1300 885 996 - motorclaims@autogeneral.com.au)(1800 701 513 - homeclaims@autogeneral.com.au)]" +
            "/P 1793 0 R" +
            $"/Parent {parentIndex} {parentGenerationNumber} R" +
            "/Rect[280.1603 716.7783 577.1978 731.0313]" +
            "/Subtype/Widget" +
            "/Type/Annot" +
            ">>";

        using var input = contentString.ToStream();

        var output = await new ComplexDictionaryParser(pdfObjects)
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.None));

        input.Position.Should().Be(Encoding.UTF8.GetByteCount(contentString));

        output.Count().Should().Be(13);

        output.Should().BeOfType<FieldDictionary>(because: "the field inherits the FT entry from its parent");
    }

    [Fact]
    public async Task ParseTextFieldDictionary()
    {
        var contentString = "<<" +
            "/AP<</N 1795 0 R>>" +
            "/BS<</S/S/Type/Border/W 1>>" +
            "/DA(/Montserrat-Regular 10 Tf 0 g)" +
            "/F 4" +
            "/FT/Tx" +
            "/Ff 8388608" +
            "/MK<<>>" +
            "/P 1793 0 R" +
            "/Rect[194.275 611.3085 576.2624 628.8166]" +
            "/Subtype/Widget" +
            "/T(Claim / Policy)" +
            "/Type/Annot" +
            "/V(117975041 02)" +
            ">>";

        using var input = contentString.ToStream();

        var output = await new ComplexDictionaryParser(A.Dummy<IPdfContext>())
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.None));

        input.Position.Should().Be(Encoding.UTF8.GetByteCount(contentString));

        output.Count().Should().Be(13);
    }

    [Fact]
    public async Task DeepNestedInheritance()
    {
        var pdfObjects = A.Fake<IPdfContext>();

        // Root page tree node
        A.CallTo(() => pdfObjects.Objects.GetAsync<Dictionary>(new IndirectObjectReference(new(1, 0))))
            .Returns(new Dictionary(
                new Dictionary<string, IPdfObject>
                {
                    [Constants.DictionaryKeys.Type] = (Name)Constants.DictionaryTypes.Pages,
                    [Constants.DictionaryKeys.PageTree.PageTreeNode.Kids] = new ArrayObject([new IndirectObjectReference(2, 0)], ObjectOrigin.None),
                    [Constants.DictionaryKeys.PageTree.MediaBox] = Rectangle.FromDimensions(1, 1)
                },
                pdfObjects,
                ObjectOrigin.None
            ));

        // First level page tree node
        A.CallTo(() => pdfObjects.Objects.GetAsync<Dictionary>(new IndirectObjectReference(new(2, 0))))
            .Returns(PageTreeNodeDictionary.FromDictionary(
                new Dictionary<string, IPdfObject>
                {
                    [Constants.DictionaryKeys.Parent] = new IndirectObjectReference(1, 0),
                    [Constants.DictionaryKeys.Type] = (Name)Constants.DictionaryTypes.Pages,
                    [Constants.DictionaryKeys.PageTree.PageTreeNode.Kids] = new ArrayObject([new IndirectObjectReference(3, 0)], ObjectOrigin.None),
                    [Constants.DictionaryKeys.PageTree.PageTreeNode.Count] = new Number(1)
                },
                pdfObjects,
                ObjectOrigin.None
            ));

        // Page does not specify MediaBox, so it should inherit it from the root page tree node
        var contentString = "<<" +
            "/Type/Page" +
            "/Parent 2 0 R" +
            ">>";

        using var input = contentString.ToStream();

        var output = await new ComplexDictionaryParser(pdfObjects)
            .ParseAsync(input, ParseContext.WithOrigin(ObjectOrigin.None)) as PageDictionary;

        output.Should().NotBeNull();

        input.Position.Should().Be(Encoding.UTF8.GetByteCount(contentString));

        output!.MediaBox.Should().NotBeNull("the page inherits the entry from its grandparent");
    }
}
