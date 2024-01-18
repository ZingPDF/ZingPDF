using FluentAssertions;
using System.Text;
using Xunit;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    public class LiteralStringParserTests
    {
        [Fact]
        public async Task ParseSimpleString()
        {
            var content = "(This is a string)";
            using var input = content.ToStream();

            LiteralString expectedLiteralString = "This is a string";

            var output = await new LiteralStringParser().ParseAsync(input);

            output.Should().BeEquivalentTo(expectedLiteralString);
        }

        [Fact]
        public async Task ParseEmptyString()
        {
            var content = "()";
            using var input = content.ToStream();

            LiteralString expectedLiteralString = "";

            var output = await new LiteralStringParser().ParseAsync(input);

            output.Should().BeEquivalentTo(expectedLiteralString);
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

            LiteralString expectedLiteralString = expected;

            var output = await new LiteralStringParser().ParseAsync(input);

            output.Should().BeEquivalentTo(expectedLiteralString);
        }

        [Fact]
        public async Task ParseSolitaryReverseSolidusIsIgnored()
        {
            var content = "(This is a valid \\string)";
            using var input = content.ToStream();

            LiteralString expectedLiteralString = "This is a valid string";

            var output = await new LiteralStringParser().ParseAsync(input);

            output.Should().BeEquivalentTo(expectedLiteralString);
        }

        [Theory]
        [InlineData("(Strings can contain balanced parentheses ())", "Strings can contain balanced parentheses ()")]
        [InlineData("(Strings can (contain) balanced parentheses)", "Strings can (contain) balanced parentheses")]
        [InlineData("(Strings can (contain) balanced (parentheses))", "Strings can (contain) balanced (parentheses)")]
        [InlineData("((Strings can contain balanced parentheses))", "(Strings can contain balanced parentheses)")]
        public async Task ParseBalancedParentheses(string content, string expected)
        {
            using var input = content.ToStream();

            LiteralString expectedLiteralString = expected;

            var output = await new LiteralStringParser().ParseAsync(input);

            output.Should().BeEquivalentTo(expectedLiteralString);
        }

        [Fact]
        public async Task ParseNewLineFollowingReverseSolidusIsIgnored()
        {
            var content = "(These \\\r\ntwo strings \\\r\nare the same.)";
            using var input = content.ToStream();

            LiteralString expectedLiteralString = "These two strings are the same.";

            var output = await new LiteralStringParser().ParseAsync(input);

            output.Should().BeEquivalentTo(expectedLiteralString);
        }

        [Theory]
        [InlineData("(This string contains \\245two octal characters\\307.)", "This string contains ¥two octal charactersÇ.")]
        [InlineData("(\\0053)", "\u00053")]
        [InlineData("(\\053)", "+")]
        [InlineData("(\\53)", "+")]
        public async Task ParseOctalCharactersAreProperlyParsed(string content, string expected)
        {
            using var input = content.ToStream();

            LiteralString expectedLiteralString = expected;

            var output = await new LiteralStringParser().ParseAsync(input);

            output.Should().BeEquivalentTo(expectedLiteralString);
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

            LiteralString expectedLiteralString = expected;

            var output = await new LiteralStringParser().ParseAsync(input);

            output.Should().BeEquivalentTo(expectedLiteralString);
        }

        [Fact]
        public async Task ParseEscapedParenthesisAtEndOfStringCorrectStreamPosition()
        {
            using var input = "(test string \\))".ToStream();

            _ = await new LiteralStringParser().ParseAsync(input);

            input.Position.Should().Be(input.Length, because: "the parser should move the stream past the string-end delimiter");
        }

        [Fact]
        public async Task ParseHandlesSimpleUTF16BEEncodedString()
        {
            var inputBytes = new List<byte>();
            var input = "TEST";
            
            var encoding = new UnicodeEncoding(bigEndian: true, byteOrderMark: true);

            inputBytes.Add((byte)Constants.LeftParenthesis);
            inputBytes.AddRange(encoding.GetPreamble());
            inputBytes.AddRange(encoding.GetBytes(input));
            inputBytes.Add((byte)Constants.RightParenthesis);

            LiteralString expectedLiteralString = input;

            using var ms = new MemoryStream(inputBytes.ToArray());
            var output = await new LiteralStringParser().ParseAsync(ms);

            output.Should().BeEquivalentTo(expectedLiteralString);
        }

        [Fact]
        public async Task ParseHandlesComplexUTF16BEEncodedString()
        {
            var inputBytes = new List<byte>();
            var input = "https://learn.something.new.com/en-gb/file.pdf";

            var encoding = new UnicodeEncoding(bigEndian: true, byteOrderMark: true);

            inputBytes.Add((byte)Constants.LeftParenthesis);
            inputBytes.AddRange(encoding.GetPreamble());
            inputBytes.AddRange(encoding.GetBytes(input));
            inputBytes.AddRange(Encoding.ASCII.GetBytes($"{Constants.RightParenthesis} /P 12 0 R /NM (0001-0001)"));

            LiteralString expectedLiteralString = input;

            using var ms = new MemoryStream(inputBytes.ToArray());
            var output = await new LiteralStringParser().ParseAsync(ms);

            output.Should().BeEquivalentTo(expectedLiteralString);
            ms.Position.Should().Be(96, because: "parsing needs to continue from the end of the literal string");
        }

        [Fact]
        public async Task ParseHandlesUTF8ByteOrderMark()
        {
            var inputBytes = new List<byte>();

            var textInput = "TEST";
            var encoding = Encoding.UTF8;

            inputBytes.Add((byte)Constants.LeftParenthesis);
            inputBytes.AddRange(encoding.GetPreamble());
            inputBytes.AddRange(encoding.GetBytes($"{textInput}{Constants.RightParenthesis}"));

            LiteralString expectedLiteralString = textInput;

            using var ms = new MemoryStream(inputBytes.ToArray());
            var output = await new LiteralStringParser().ParseAsync(ms);

            output.Should().BeEquivalentTo(expectedLiteralString);
        }

        [Fact]
        public async Task ParseOctalPreambleString()
        {
            var input = "(\\376\\377\\000U\\000s\\000a\\000g\\000e\\000\\040\\000o\\000n\\000\\040\\000R\\000e\\000d\\000H\\000a\\000t\\000\\040\\000L\\000i\\000n\\000u\\000x)\r\n" +
                "<< /S /GoTo /D (section.23.5) >>\r\n" +
                "(\\376\\377\\000O\\000t\\000h\\000e\\000r\\000\\040\\000C\\000a\\000n\\000o\\000n\\000\\040\\000B\\000u\\000b\\000b\\000l\\000e\\000J\\000e\\000t\\000\\040\\000\\050\\000B\\000J\\000C\\000\\051\\000\\040\\000p\\000r\\000i\\000n\\000t\\000e\\000r\\000s)\r\n" +
                "<< /S /GoTo /D (subsection.23.5.1) >>\r\n(\\376\\377\\000H\\000i\\000s\\000t\\000o\\000r\\000y)\r\n" +
                "<< /S /GoTo /D (subsection.23.5.2) >>\r\n" +
                "(\\376\\377\\000C\\000o\\000n\\000f\\000i\\000g\\000u\\000r\\000i\\000n\\000g\\000\\040\\000a\\000n\\000d\\000\\040\\000b\\000u\\000i\\000l\\000d\\000i\\000n\\000g\\000\\040\\000t\\000h\\000e\\000\\040\\000B\\000J\\000C\\000\\040\\000d\\000r\\000i\\000v\\000e\\000r\\000s)\r\n" +
                "<< /S /GoTo /D (subsubsection*.579) >>\r\n" +
                "(\\376\\377\\000C\\000M\\000Y\\000K\\000-\\000t\\000o\\000-\\000R\\000G\\000B\\000\\040\\000c\\000o\\000l\\000o\\000r\\000\\040\\000c\\000o\\000n\\000v\\000e\\000r\\000s\\000i\\000o\\000n)\r\n" +
                "<< /S /GoTo /D (subsubsection*.580) >>\r\n" +
                "(\\376\\377\\000V\\000e\\000r\\000t\\000i\\000c\\000a\\000l\\000\\040\\000c\\000e\\000n\\\r\n";

            var inputBytes = input.ToStream();

            var output = await new LiteralStringParser().ParseAsync(inputBytes);

            output.Value.Should().Be("Usage on RedHat Linux");
            inputBytes.Position.Should().Be(150, because: "the parser should move the stream past the string-end delimiter");
        }
    }
}
