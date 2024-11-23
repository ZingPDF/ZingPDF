using FluentAssertions;
using Xunit;

namespace ZingPDF.Extensions;

public class StreamExtensionsTests
{
    [Theory]
    [InlineData("test1", 5)]
    [InlineData("test\n1", 6)]
    public async Task AdvanceBeyond_SetsCorrectPosition(string inputString, int expectedPosition)
    {
        var inputStream = inputString.ToStream();

        await inputStream.AdvanceBeyondNextAsync('1');

        inputStream.Position.Should().Be(expectedPosition);
    }

    [Theory]
    [InlineData("test1", 4)]
    [InlineData("test\n1", 5)]
    public async Task AdvanceTo_SetsCorrectPosition(string inputString, int expectedPosition)
    {
        var inputStream = inputString.ToStream();

        await inputStream.AdvanceToNextAsync('1');

        inputStream.Position.Should().Be(expectedPosition);
    }

    [Fact]
    public async Task ReadUpToExcluding_NameDelimiters_SetsCorrectPosition()
    {
        char[] _nameDelimiters =
        [
            Constants.Solidus,
            Constants.Space,
            Constants.CarriageReturn,
            Constants.LineFeed,
            Constants.LessThan,
            Constants.LeftSquareBracket,
            Constants.LeftParenthesis
        ];

        var inputStream = "test ".ToStream();

        await inputStream.ReadUpToExcludingAsync(_nameDelimiters);

        inputStream.Position.Should().Be(4);
    }

    [Theory]
    [InlineData(" ", 1)]
    [InlineData("\r", 1)]
    [InlineData("\n", 1)]
    [InlineData("\r\n", 2)]
    [InlineData("\f", 1)]
    [InlineData("\t", 1)]
    [InlineData(" 123", 1)]
    [InlineData("\r123", 1)]
    [InlineData("\n123", 1)]
    [InlineData("\r\n123", 2)]
    [InlineData("\f123", 1)]
    [InlineData("\t123", 1)]
    public void AdvancePastWhitespace_SetsCorrectPosition(string contentString, int expectedPosition)
    {
        var inputStream = contentString.ToStream();

        inputStream.AdvancePastWhitepace();

        inputStream.Position.Should().Be(expectedPosition);
    }
}
