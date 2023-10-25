using FluentAssertions;
using Xunit;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Parsing.PrimitiveParsers;

namespace ZingPdf.Core.Parsing
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
    }
}
