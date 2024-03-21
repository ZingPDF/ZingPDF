using FluentAssertions;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Objects.Primitives;

namespace ZingPDF.Parsing.PrimitiveParsers;

public class ArrayParserTests
{
    [Fact]
    public async Task ParseEmptyArray_CorrectCount()
    {
        using var input = "[]".ToStream();

        var output = await new ArrayParser()
            .ParseAsync(input);

        output.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseEmptyArray_CorrectStreamPosition()
    {
        using var input = "[]".ToStream();

        var output = await new ArrayParser()
            .ParseAsync(input);

        input.Position.Should().Be(2);
    }

    [Fact]
    public async Task ParseEmptyArray_WithWhitespace_CorrectCount()
    {
        using var input = "[ ]".ToStream();

        var output = await new ArrayParser()
            .ParseAsync(input);

        output.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseEmptyArray_WithWhitespace_CorrectStreamPosition()
    {
        using var input = "[ ]".ToStream();

        var output = await new ArrayParser()
            .ParseAsync(input);

        input.Position.Should().Be(3);
    }

    //[Fact]
    //public async Task ParseNestedArray()
    //{
    //    using var input = "[[(2020-12-03_ISO_32000-2-final.pdf)90827 0 R]]".ToStream();

    //    var output = await new ArrayParser()
    //        .ParseAsync(input);

    //    output.Should().HaveCount(1);
    //    output.First().Should().BeOfType<ArrayObject>();
    //    output.First().As<ArrayObject>().Should().HaveCount(2);

    //    input.Position.Should().Be(input.Length);
    //}

    [Theory]
    [InlineData("[ /Test ]", 1)]
    [InlineData("[ /Test /Test ]", 2)]
    public async Task ParseArrayWithLeadingAndTrailingSpace_CorrectCount(string input, int expectedCount)
    {
        using var inputStream = input.ToStream();

        var output = await new ArrayParser()
            .ParseAsync(inputStream);

        output.Should().HaveCount(expectedCount);
    }

    [Theory]
    [InlineData("[ /Test ]")]
    [InlineData("[ /Test /Test ]")]
    public async Task ParseArrayWithLeadingAndTrailingSpace_CorrectStreamPosition(string input)
    {
        using var inputStream = input.ToStream();

        var output = await new ArrayParser()
            .ParseAsync(inputStream);

        inputStream.Position.Should().Be(input.Length);
    }

    [Theory]
    [InlineData("[/Test]", 1)]
    [InlineData("[/Test/Test]", 2)]
    public async Task ParseArray_NoWhitespace_CorrectCount(string input, int expectedCount)
    {
        using var inputStream = input.ToStream();

        var output = await new ArrayParser()
            .ParseAsync(inputStream);

        output.Should().HaveCount(expectedCount);
    }

    [Theory]
    [InlineData("[/Test]")]
    [InlineData("[/Test/Test]")]
    public async Task ParseArray_NoWhitespace_CorrectStreamPosition(string input)
    {
        using var inputStream = input.ToStream();

        var output = await new ArrayParser()
            .ParseAsync(inputStream);

        inputStream.Position.Should().Be(
            input.Length,
            because: "the parser should move the stream past the array-end delimiter"
            );
    }

    [Fact]
    public async Task ParseArrayOfIntegersMultiline()
    {
        // During parsing, the TokenTypeIdentifier must not mistake the line ending
        // and the tokens preceding it for a cross reference section index.
        var contentString = "[ 1 52\r\n" +
            " 1 54 1 56 1 58 1 60 1 62 1 64 1 66 1 69 1 71 ]";

        using var input = contentString.ToStream();

        var output = await new ArrayParser()
            .ParseAsync(input);

        output.All(x => x is Integer).Should().BeTrue();
    }

    [Fact]
    public async Task ParseArrayOfHexValues()
    {
        var contentString = "[<81b14aafa313db63dbd6f981e49f94f4>\r\n" +
            "<81b14aafa313db63dbd6f981e49f94f4>\r\n" +
            "]\r\n";

        using var input = contentString.ToStream();

        var output = await new ArrayParser()
            .ParseAsync(input);

        output.Count().Should().Be(2);
        output.All(x => x is HexadecimalString).Should().BeTrue();

        input.Position.Should().Be(74, because: "the parser should move the stream past the array-end delimiter");
    }

    [Fact]
    public async Task ParseEmptyNestedArray_WithWhitespace_CorrectCounts()
    {
        var contentString = "[ [ ] ]";

        using var input = contentString.ToStream();

        var output = await new ArrayParser()
            .ParseAsync(input);

        output.Should().HaveCount(1);
        output.Get<ArrayObject>(0).Should().BeEmpty();
    }

    [Fact]
    public async Task ParseEmptyNestedArray_WithWhitespace_CorrectStreamPosition()
    {
        var contentString = "[ [ ] ]";

        using var input = contentString.ToStream();

        var output = await new ArrayParser()
            .ParseAsync(input);

        input.Position.Should().Be(7, because: "the parser should move the stream past the array-end delimiter");
    }

    [Fact]
    public async Task ParseEmptyNestedArray_CorrectCounts()
    {
        var contentString = "[[]]";

        using var input = contentString.ToStream();

        var output = await new ArrayParser()
            .ParseAsync(input);

        output.Should().HaveCount(1);
        output.Get<ArrayObject>(0).Should().BeEmpty();
    }

    [Fact]
    public async Task ParseEmptyNestedArray_CorrectStreamPosition()
    {
        var contentString = "[[]]";

        using var input = contentString.ToStream();

        var output = await new ArrayParser()
            .ParseAsync(input);

        input.Position.Should().Be(
            contentString.Length,
            because: "the parser should move the stream past the array-end delimiter"
            );
    }

    [Fact]
    public async Task ParseSimpleNestedArray_CorrectCounts()
    {
        var contentString = "[/Test[]]";

        using var input = contentString.ToStream();

        var output = await new ArrayParser()
            .ParseAsync(input);

        output.Should().HaveCount(2);
        output.Get<ArrayObject>(1).Should().BeEmpty();
    }

    [Fact]
    public async Task ParseSimpleNestedArray_CorrectStreamPosition()
    {
        var contentString = "[/Test[]]";

        using var input = contentString.ToStream();

        var output = await new ArrayParser()
            .ParseAsync(input);

        input.Position.Should().Be(
            contentString.Length,
            because: "the parser should move the stream past the array-end delimiter"
            );
    }
}
