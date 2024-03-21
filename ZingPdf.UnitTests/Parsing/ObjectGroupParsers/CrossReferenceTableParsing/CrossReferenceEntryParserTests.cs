using FluentAssertions;
using Xunit;
using ZingPDF.Extensions;

namespace ZingPDF.Parsing.ObjectGroupParsers.CrossReferenceTableParsing;

public class CrossReferenceEntryParserTests
{
    [Theory]
    [InlineData("0000000000 65535 f\n", 0, 65535, false)]
    [InlineData("0000000017 00000 n\n", 17, 0, true)]
    public async Task ParseAsyncBasic(string input, long expectedOffset, ushort expectedGenNumber, bool expectedInUse)
    {
        var output = await new CrossReferenceEntryParser().ParseAsync(input.ToStream());

        output.Value1.Should().Be(expectedOffset);
        output.Value2.Should().Be(expectedGenNumber);
        output.InUse.Should().Be(expectedInUse);
    }
}
