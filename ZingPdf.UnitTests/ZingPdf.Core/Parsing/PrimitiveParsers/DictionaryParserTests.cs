using FluentAssertions;
using System.Text;
using Xunit;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

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
        public async Task ParseSimpleNestedDictionary_CorrectCounts()
        {
            var contentString = "<</Resources <<>>>>";

            using var input = contentString.ToStream();

            var output = await new DictionaryParser().ParseAsync(input);

            output.Should().NotBeNull().And.HaveCount(1);

            var nestedDictionary = output.Get<Dictionary>("Resources");

            nestedDictionary.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public async Task ParseSimpleNestedDictionary_CorrectStreamPosition()
        {
            var contentString = "<</Resources <<>>>>";

            using var input = contentString.ToStream();

            var output = await new DictionaryParser().ParseAsync(input);

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

            var output = await new DictionaryParser().ParseAsync(input);

            output.Should().NotBeNull().And.HaveCount(1);

            var nestedDictionary = output.Get<Dictionary>("Resources");

            nestedDictionary.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public async Task ParseSimpleNestedDictionary_WithWhitepsace_CorrectStreamPosition()
        {
            var contentString = "<< /Resources << >> >>";

            using var input = contentString.ToStream();

            var output = await new DictionaryParser().ParseAsync(input);

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

            var output = await new DictionaryParser().ParseAsync(input);
        }

        [Fact]
        public async Task ParseSimpleDictionary_WithWindowsLineEndings_CorrectFields()
        {
            var contentString = "<<\r\n" +
                "/Type /Page\r\n" +
                "/Other /Test\r\n" +
                ">>";

            using var input = contentString.ToStream();

            var output = await new DictionaryParser().ParseAsync(input);

            output.Get<Name>("Type")!.Value.Should().Be("Page");
            output.Get<Name>("Other")!.Value.Should().Be("Test");

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

            var output = await new DictionaryParser().ParseAsync(input);

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

            var output = await new DictionaryParser().ParseAsync(input);

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

            var output = await new DictionaryParser().ParseAsync(input);

            input.Position.Should().Be(
                Encoding.UTF8.GetByteCount(contentString),
                because: "the parser should move the stream past the dictionary-end delimiter"
                );
        }
    }
}
