using FluentAssertions;
using System.Text;
using Xunit;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    public class DictionaryParserTests
    {
        [Fact]
        public async Task ParseEmptyAsync()
        {
            using var input = "<< >>".ToStream();

            var output = await new DictionaryParser().ParseAsync(input);

            output.Should().BeEmpty();
        }

        [Fact]
        public async Task ParseSimpleNestedDictionary()
        {
            var contentString = "<</Resources <</ProcSet [/PDF /Text]>>>>";

            using var input = contentString.ToStream();

            var output = await new DictionaryParser().ParseAsync(input);

            output.Should().NotBeNull().And.HaveCount(1);

            var nestedDictionary = output.Get<Dictionary>("Resources");

            nestedDictionary.Should().NotBeNull().And.HaveCount(1);

            var array = nestedDictionary!.Get<ArrayObject>("ProcSet");

            array.Should().NotBeNull().And.HaveCount(2);
        }

        [Fact]
        public async Task ParseTrailerDictionary()
        {
            var contentString = "<< /Size 50 /Root 49 0 R /Info 47 0 R " +
                "/ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>\r\n" +
                "startxref\r\n148076\r\n%%EOF\r\n";

            using var input = contentString.ToStream();

            var output = await new DictionaryParser().ParseAsync(input);

            output.Get<Integer>("Size");
            output.Get<IndirectObjectReference>("Root");
            output.Get<IndirectObjectReference>("Info");
            output.Get<ArrayObject>("ID").Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ParsePageDictionary()
        {
            var contentString = "<<" +
                "/Type /Page\r\n" +
                "/Resources <</ProcSet [/PDF /Text /ImageB /ImageC /ImageI]\r\n" +
                    "/ExtGState <</G3 3 0 R>>\r\n" +
                    "/Pattern <</P6 6 0 R\r\n" +
                        "/P7 7 0 R\r\n" +
                        "/P8 8 0 R\r\n" +
                        "/P9 9 0 R>>\r\n" +
                    "/XObject <</X11 11 0 R>>\r\n" +
                    "/Font <</F4 4 0 R\r\n" +
                        "/F5 5 0 R\r\n" +
                        "/F10 10 0 R>>" +
                    ">>\r\n" +
                "/MediaBox [0 0 594.95996 841.91998]\r\n" +
                "/Contents 12 0 R\r\n" +
                "/StructParents 0\r\n" +
                "/Parent 94 0 R" +
                ">>";

            using var input = contentString.ToStream();

            var output = await new DictionaryParser().ParseAsync(input);

            output.Get<Name>("Type")!.Value.Should().Be("Page");
            output.Get<Dictionary>("Resources").Should().HaveCount(5);
            output.Get<ArrayObject>("MediaBox").Should().HaveCount(4);

            var contentsReference = output.Get<IndirectObjectReference>("Contents");
            contentsReference.Should().NotBeNull();
            contentsReference!.Id.Index.Should().Be(12);
            contentsReference!.Id.GenerationNumber.Should().Be(0);

            var structParents = output.Get<Integer>("StructParents");
            structParents.Should().NotBeNull();
            structParents!.Value.Should().Be(0);

            var parentReference = output.Get<IndirectObjectReference>("Parent");
            parentReference.Should().NotBeNull();
            parentReference!.Id.Index.Should().Be(94);
            parentReference!.Id.GenerationNumber.Should().Be(0);
        }

        [Fact]
        public async Task ParseComplexDelimiters()
        {
            var contentString = "<<" +
                "/DecodeParms<</Columns 5/Predictor 12>>" +
                "/Filter/FlateDecode" +
                "/ID[<2B551D2AFE52654494F9720283CFF1C4><3CDA8BB6D5834E41A5E2AA16C35E4C47>]" +
                "/Index[90793 1014]/Info 90792 0 R/Length 185/Prev 14709647" +
                "/Root 90794 0 R/Size 91807/Type/XRef/W[1 3 1]>>";

            using var input = contentString.ToStream();

            var output = await new DictionaryParser().ParseAsync(input);
        }

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

            var output = await new DictionaryParser().ParseAsync(input);
        }

        [Fact]
        public async Task ParseSingleArrayElement()
        {
            var contentString = "<</Index[90793 1014]>>";

            using var input = contentString.ToStream();

            var output = await new DictionaryParser().ParseAsync(input);

            output.Should().HaveCount(1);
            input.Position.Should().Be(22, because: "the parser should move the stream past the string-end delimiter");
        }

        [Fact]
        public async Task ParseSimplePageDictionary()
        {
            var contentString = "<<\r\n" +
                "/Type /Page\r\n" +
                "/MediaBox [0 0 594.95996 841.91998]\r\n" +
                ">>";

            using var input = contentString.ToStream();

            var output = await new DictionaryParser().ParseAsync(input);

            output.Get<Name>("Type")!.Value.Should().Be("Page");
            output.Get<ArrayObject>("MediaBox").Should().HaveCount(4);

            input.Position.Should().Be(Encoding.UTF8.GetByteCount(contentString));
        }
    }
}
