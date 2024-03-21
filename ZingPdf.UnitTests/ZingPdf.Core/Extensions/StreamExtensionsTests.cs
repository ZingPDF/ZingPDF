using FluentAssertions;
using Xunit;

namespace ZingPDF.Extensions
{
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
                Constants.ArrayStart,
                Constants.LeftParenthesis
            ];

            var inputStream = "test ".ToStream();

            await inputStream.ReadUpToExcludingAsync(_nameDelimiters);

            inputStream.Position.Should().Be(4);
        }
    }
}
