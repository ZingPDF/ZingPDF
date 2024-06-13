using FluentAssertions;
using Xunit;
using ZingPDF.Extensions;

namespace ZingPDF.Parsing.Parsers.Objects;

public class NameParserTests
{
    [Theory]
    [InlineData("/Size ", "Size")]
    [InlineData("/Size 50", "Size")]
    [InlineData("/Type/Catalog", "Type")]
    [InlineData("/Pages 2 0 R", "Pages")]
    [InlineData("<</Type/Catalog/Pages 2 0 R", "Type")]
    [InlineData("/Page\r\n", "Page")]
    [InlineData("/DecodeParms<</Columns", "DecodeParms")]
    [InlineData("/Lang\n", "Lang")]
    [InlineData("/Name1 ", "Name1")]
    [InlineData("/ASomewhatLongerName ", "ASomewhatLongerName")]
    [InlineData("/A;Name_With-Various***Characters? ", "A;Name_With-Various***Characters?")]
    [InlineData("/1.2 ", "1.2")]
    [InlineData("/$$ ", "$$")]
    [InlineData("/@pattern ", "@pattern")]
    [InlineData("/.notdef ", ".notdef")]
    [InlineData("/Lime#20Green ", "Lime Green")]
    [InlineData("/paired#28#29parentheses ", "paired()parentheses")]
    [InlineData("/The_Key_of_F#23_Minor ", "The_Key_of_F#_Minor")]
    public async Task ParseBasic_CorrectContent(string content, string expected)
    {
        using var input = content.ToStream();

        var output = await new NameParser().ParseAsync(input);

        output.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("/Size ", 5)]
    [InlineData("/Size 50", 5)]
    [InlineData("/Type/Catalog", 5)]
    [InlineData("/Pages 2 0 R", 6)]
    [InlineData("<</Type/Catalog/Pages 2 0 R", 7)]
    [InlineData("/Page\r\n", 5)]
    [InlineData("/DecodeParms<</Columns", 12)]
    [InlineData("/Lang\n", 5)]
    [InlineData("/Name1 ", 6)]
    [InlineData("/ASomewhatLongerName ", 20)]
    [InlineData("/A;Name_With-Various***Characters? ", 34)]
    [InlineData("/1.2 ", 4)]
    [InlineData("/$$ ", 3)]
    [InlineData("/@pattern ", 9)]
    [InlineData("/.notdef ", 8)]
    [InlineData("/Lime#20Green ", 13)]
    [InlineData("/paired#28#29parentheses ", 24)]
    [InlineData("/The_Key_of_F#23_Minor ", 22)]
    public async Task ParseBasic_CorrectStreamPosition(string content, int expectedPosition)
    {
        using var input = content.ToStream();

        var output = await new NameParser().ParseAsync(input);

        input.Position.Should().Be(expectedPosition);
    }
}
