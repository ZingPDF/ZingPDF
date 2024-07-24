using FluentAssertions;
using Xunit;
using ZingPDF.Extensions;

namespace ZingPDF.Syntax.FileStructure.CrossReferences;

public class CrossReferenceEntryTests
{
    [Theory]
    [InlineData(0, 65535, false, "0000000000 65535 f\n")]
    [InlineData(15, 0, true, "0000000015 00000 n\n")]
    [InlineData(14278075, 0, true, "0014278075 00000 n\n")]
    public async Task WriteAsyncCorrectGenerationNumberFormat(long byteOffset, ushort genNumber, bool inUse, string expected)
    {
        var xrefEntry = new CrossReferenceEntry(byteOffset, genNumber, inUse, compressed: false);

        using var ms = new MemoryStream();

        await xrefEntry.WriteAsync(ms);

        ms.Position = 0;
        var output = await ms.GetAsync();

        output.Should().Be(expected);
    }
}
