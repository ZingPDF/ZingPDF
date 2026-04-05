using FluentAssertions;
using System.Text;
using Xunit;
using ZingPDF.Extensions;

namespace ZingPDF.Parsing;

public class SubStreamTests
{
    [Fact]
    public async Task ReadGetsCorrectContent()
    {
        var sourceStream = "01234567890".ToStream();
        var subStream = new SubStream(sourceStream, 1, 10);

        var buffer = new byte[1024];
        var read = await subStream.ReadAsync(buffer.AsMemory(0, 1024));
        var content = Encoding.ASCII.GetString(buffer, 0, read);

        read.Should().Be(9);
        content.Should().Be("123456789");
    }
}
