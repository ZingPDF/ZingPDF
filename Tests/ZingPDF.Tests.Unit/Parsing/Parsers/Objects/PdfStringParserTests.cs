using FluentAssertions;
using System.Text;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text.Encoding.PDFDocEncoding;

namespace ZingPDF.Parsing.Parsers.Objects;

public class PdfStringParserTests
{
    static PdfStringParserTests()
    {
        Encoding.RegisterProvider(PDFDocEncodingProvider.Instance);
    }

    [Fact]
    public async Task ParseSimpleString()
    {
        var content = "(This is a string)";
        using var input = content.ToStream();

        var output = await new PdfStringParser()
            .ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Syntax.Should().Be(PdfStringSyntax.Literal);
        output.Decode().Should().Be("This is a string");
    }

    [Fact]
    public async Task ParseEmptyString_CorrectContent()
    {
        var content = "()";
        using var input = content.ToStream();

        var output = await new PdfStringParser()
            .ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Syntax.Should().Be(PdfStringSyntax.Literal);
        output.Bytes.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseEmptyString_CorrectStreamPosition()
    {
        var content = "()";
        using var input = content.ToStream();

        _ = await new PdfStringParser()
            .ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.None));

        input.Position.Should().Be(
            content.Length,
            because: "the parser should move the stream past the string-end delimiter"
            );
    }

    [Theory]
    [InlineData("(Strings can contain newlines \r\n.)", "Strings can contain newlines \r\n.")]
    [InlineData("(Strings can contain newlines \n.)", "Strings can contain newlines \n.")]
    [InlineData("(Strings can contain tabs \t.)", "Strings can contain tabs \t.")]
    [InlineData("(Strings can contain special characters ( * ! & } ^ %and so on).)", "Strings can contain special characters ( * ! & } ^ %and so on).")]
    [InlineData("(Strings can contain escaped characters such as \\(\r, \t, \b, \f, \\\\\\).)", "Strings can contain escaped characters such as (\r, \t, \b, \f, \\).")]
    public async Task ParseSpecialCharactersArePreserved(string content, string expected)
    {
        using var input = content.ToStream();

        var output = await new PdfStringParser()
            .ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Decode().Should().Be(expected);
    }

    [Fact]
    public async Task ParseSolitaryReverseSolidusIsIgnored()
    {
        var content = "(This is a valid \\string)";
        using var input = content.ToStream();

        var output = await new PdfStringParser()
            .ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Decode().Should().Be("This is a valid string");
    }

    [Theory]
    [InlineData("(Strings can contain balanced parentheses ())", "Strings can contain balanced parentheses ()")]
    [InlineData("(Strings can (contain) balanced parentheses)", "Strings can (contain) balanced parentheses")]
    [InlineData("(Strings can (contain) balanced (parentheses))", "Strings can (contain) balanced (parentheses)")]
    [InlineData("((Strings can contain balanced parentheses))", "(Strings can contain balanced parentheses)")]
    public async Task ParseBalancedParentheses(string content, string expected)
    {
        using var input = content.ToStream();

        var output = await new PdfStringParser()
            .ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Decode().Should().Be(expected);
    }

    [Fact]
    public async Task ParseNewLineFollowingReverseSolidusIsIgnored()
    {
        var content = "(These \\\r\ntwo strings \\\r\nare the same.)";
        using var input = content.ToStream();

        var output = await new PdfStringParser()
            .ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Decode().Should().Be("These two strings are the same.");
    }

    [Theory]
    [InlineData("(This string contains \\245two octal characters\\307.)", "This string contains ¥two octal charactersÇ.")]
    [InlineData("(\\0053)", "\u00053")]
    [InlineData("(\\053)", "+")]
    [InlineData("(\\53)", "+")]
    public async Task OctalCharactersAreProperlyParsed(string content, string expected)
    {
        using var input = content.ToStream();

        var output = await new PdfStringParser()
            .ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Decode().Should().Be(expected);
    }

    [Theory]
    [InlineData("(Strings can contain escaped parentheses such as \\).)", "Strings can contain escaped parentheses such as ).")]
    [InlineData("(Strings can contain escaped parentheses such as \\))", "Strings can contain escaped parentheses such as )")]
    [InlineData("(Strings can contain escaped parentheses such as \\(.)", "Strings can contain escaped parentheses such as (.")]
    [InlineData("(Strings can contain escaped parentheses such as \\()", "Strings can contain escaped parentheses such as (")]
    [InlineData("(\\(Strings can contain escaped parentheses)", "(Strings can contain escaped parentheses")]
    [InlineData("( \\(Strings can contain escaped parentheses)", " (Strings can contain escaped parentheses")]
    public async Task ParseUnbalancedEscapedParentheses(string content, string expected)
    {
        using var input = content.ToStream();

        var output = await new PdfStringParser()
            .ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Decode().Should().Be(expected);
    }

    [Fact]
    public async Task ParseEscapedParenthesisAtEndOfStringCorrectStreamPosition()
    {
        using var input = "(test string \\))".ToStream();

        _ = await new PdfStringParser()
            .ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.None));

        input.Position.Should().Be(input.Length, because: "the parser should move the stream past the string-end delimiter");
    }

    [Fact]
    public async Task ParseHandlesSimpleUTF16BEEncodedString()
    {
        var inputBytes = new List<byte>();
        var input = "TEST";

        var encoding = new UnicodeEncoding(bigEndian: true, byteOrderMark: true);

        inputBytes.Add((byte)Constants.Characters.LeftParenthesis);
        inputBytes.AddRange(encoding.GetPreamble());
        inputBytes.AddRange(encoding.GetBytes(input));
        inputBytes.Add((byte)Constants.Characters.RightParenthesis);

        using var ms = new MemoryStream([.. inputBytes]);
        var output = await new PdfStringParser()
            .ParseAsync(ms, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Decode().Should().Be(input);
    }

    [Fact]
    public async Task ParseHandlesComplexUTF16BEEncodedString_CorrectContent()
    {
        var inputBytes = new List<byte>();
        var input = "https://learn.something.new.com/en-gb/file.pdf";

        var encoding = new UnicodeEncoding(bigEndian: true, byteOrderMark: true);

        inputBytes.Add((byte)Constants.Characters.LeftParenthesis);
        inputBytes.AddRange(encoding.GetPreamble());
        inputBytes.AddRange(encoding.GetBytes(input));
        inputBytes.Add((byte)Constants.Characters.RightParenthesis);

        using var ms = new MemoryStream([.. inputBytes]);
        var output = await new PdfStringParser()
            .ParseAsync(ms, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Decode().Should().Be(input);
    }

    [Fact]
    public async Task ParseHandlesComplexUTF16BEEncodedString_CorrectStreamPosition()
    {
        var inputBytes = new List<byte>();
        var input = "https://learn.something.new.com/en-gb/file.pdf";

        var encoding = new UnicodeEncoding(bigEndian: true, byteOrderMark: true);

        inputBytes.Add((byte)Constants.Characters.LeftParenthesis);
        inputBytes.AddRange(encoding.GetPreamble());
        inputBytes.AddRange(encoding.GetBytes(input));
        inputBytes.AddRange(Encoding.ASCII.GetBytes($"{Constants.Characters.RightParenthesis} /P 12 0 R /NM (0001-0001)"));

        using var ms = new MemoryStream([.. inputBytes]);
        _ = await new PdfStringParser()
            .ParseAsync(ms, ObjectContext.None);

        ms.Position.Should().Be(96, because: "parsing needs to continue from the end of the literal string");
    }

    [Fact]
    public async Task ParseHandlesUTF8ByteOrderMark()
    {
        var inputBytes = new List<byte>();

        var textInput = "TEST";
        var encoding = Encoding.UTF8;

        inputBytes.Add((byte)Constants.Characters.LeftParenthesis);
        inputBytes.AddRange(encoding.GetPreamble());
        inputBytes.AddRange(encoding.GetBytes($"{textInput}{Constants.Characters.RightParenthesis}"));

        using var ms = new MemoryStream([.. inputBytes]);
        var output = await new PdfStringParser()
            .ParseAsync(ms, ObjectContext.None);

        output.Decode().Should().Be(textInput);
    }

    [Fact]
    public async Task ParseMultilineUTF16BEString_CorrectContent()
    {
        var inputBytes = new List<byte>();

        var textInput = "Usage on RedHat Linux";
        var encoding = Encoding.BigEndianUnicode;

        inputBytes.Add((byte)Constants.Characters.LeftParenthesis);
        inputBytes.AddRange(encoding.GetPreamble());
        inputBytes.AddRange(encoding.GetBytes(textInput));
        inputBytes.Add((byte)Constants.Characters.RightParenthesis);

        using var ms = new MemoryStream([.. inputBytes]);
        var output = await new PdfStringParser()
            .ParseAsync(ms, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Decode().Should().Be(textInput);
    }

    [Fact]
    public async Task ParseMultilineUTF16BEString_CorrectStreamPosition()
    {
        var inputBytes = new List<byte>();

        var textInput = "Usage on RedHat Linux";
        var encoding = Encoding.BigEndianUnicode;

        inputBytes.Add((byte)Constants.Characters.LeftParenthesis);
        inputBytes.AddRange(encoding.GetPreamble());
        inputBytes.AddRange(encoding.GetBytes(textInput));
        inputBytes.Add((byte)Constants.Characters.RightParenthesis);

        // Add some extraneous content to ensure the parsing ends in the correct place.
        inputBytes.AddRange(Encoding.ASCII.GetBytes("\r\n<< /S /GoTo /D (section.23.5) >>\r\n"));

        using var ms = new MemoryStream([.. inputBytes]);
        _ = await new PdfStringParser()
            .ParseAsync(ms, ObjectContext.WithOrigin(ObjectOrigin.None));

        ms.Position.Should().Be(46, because: "the parser should move the stream past the string-end delimiter");
    }

    [Fact]
    public async Task ParseFullyOctalEncodedUTF16BEString_CorrectContent()
    {
        var input = "(\\376\\377\\000A\\000r\\000t\\000i\\000f\\000e\\000x)";

        string expected = "Artifex";

        using var ms = new MemoryStream(Encoding.ASCII.GetBytes(input));
        var output = await new PdfStringParser()
            .ParseAsync(ms, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Decode().Should().Be(expected);
    }

    [Fact]
    public async Task ParseFullyOctalEncodedUTF16BEString_CorrectStreamPosition()
    {
        var input = "(\\376\\377\\000A\\000r\\000t\\000i\\000f\\000e\\000x)";

        using var ms = new MemoryStream(Encoding.ASCII.GetBytes(input));
        _ = await new PdfStringParser()
            .ParseAsync(ms, ObjectContext.WithOrigin(ObjectOrigin.None));

        ms.Position.Should().Be(45, because: "the parser should move the stream past the string-end delimiter");
    }

    [Fact]
    public async Task ParseAnotherFullyOctalEncodedUTF16BEString_CorrectContent()
    {
        var input = "(\\376\\377\\000U\\000s\\000a\\000g\\000e\\000\\040" +
            "\\000o\\000n\\000\\040\\000R\\000e\\000d\\000H\\000a\\000t" +
            "\\000\\040\\000L\\000i\\000n\\000u\\000x)" +
            "\r\n<< /S /GoTo /D (section.23.5) >>";

        string expected = "Usage on RedHat Linux";

        using var ms = new MemoryStream(Encoding.ASCII.GetBytes(input));
        var output = await new PdfStringParser()
            .ParseAsync(ms, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Decode().Should().Be(expected);
    }

    [Fact]
    public async Task ParseAnotherFullyOctalEncodedUTF16BEString_CorrectStreamPosition()
    {
        var input = "(\\376\\377\\000U\\000s\\000a\\000g\\000e\\000\\040" +
            "\\000o\\000n\\000\\040\\000R\\000e\\000d\\000H\\000a\\000t" +
            "\\000\\040\\000L\\000i\\000n\\000u\\000x)" +
            "\r\n<< /S /GoTo /D (section.23.5) >>";

        using var ms = new MemoryStream(Encoding.ASCII.GetBytes(input));
        _ = await new PdfStringParser()
            .ParseAsync(ms, ObjectContext.WithOrigin(ObjectOrigin.None));

        ms.Position.Should().Be(124, because: "the parser should move the stream past the string-end delimiter");
    }

    [Fact]
    public async Task ParseHexString_IgnoresWhitespaceAndSetsSyntax()
    {
        var content = "<4E 6F 76 20>";
        using var input = content.ToStream();

        var output = await new PdfStringParser()
            .ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Syntax.Should().Be(PdfStringSyntax.Hex);
        output.Bytes.Should().Equal(Encoding.ASCII.GetBytes("Nov "));
    }

    [Fact]
    public async Task ParseHexString_OddNibblePadsLowNibble()
    {
        var content = "<4E6F7>";
        using var input = content.ToStream();

        var output = await new PdfStringParser()
            .ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Syntax.Should().Be(PdfStringSyntax.Hex);
        output.Bytes.Should().Equal([0x4E, 0x6F, 0x70]);
    }

    [Fact]
    public async Task ParseHexString_WithUtf16Bom_DecodesAsText()
    {
        var content = "<FEFF00410042>";
        using var input = content.ToStream();

        var output = await new PdfStringParser()
            .ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.None));

        output.Syntax.Should().Be(PdfStringSyntax.Hex);
        output.Decode().Should().Be("AB");
    }
}
