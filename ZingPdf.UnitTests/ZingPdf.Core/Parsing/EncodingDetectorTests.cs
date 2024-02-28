using FluentAssertions;
using System.Text;
using Xunit;

namespace ZingPdf.Core.Parsing
{
    public class EncodingDetectorTests
    {
        [Fact]
        public async Task DetectEncodingUTF8()
        {
            var utf8Content = new List<byte>();
            utf8Content.AddRange(Encoding.ASCII.GetBytes("("));
            utf8Content.AddRange(Encoding.UTF8.Preamble);
            utf8Content.AddRange(Encoding.UTF8.GetBytes("test"));
            utf8Content.AddRange(Encoding.ASCII.GetBytes(")"));

            using var ms = new MemoryStream([.. utf8Content]);
            ms.Position = 1;

            var output = await new EncodingDetector().DetectAsync(ms);

            output.EncodingName.Should().Be(Encoding.UTF8.EncodingName);
        }

        [Fact]
        public async Task DetectEncodingUTF8OctalPreamble()
        {
            var utf8Content = new List<byte>();
            utf8Content.AddRange(Encoding.ASCII.GetBytes("("));
            utf8Content.AddRange(Encoding.ASCII.GetBytes("\\357\\273\\277"));
            utf8Content.AddRange(Encoding.UTF8.GetBytes("test"));
            utf8Content.AddRange(Encoding.ASCII.GetBytes(")"));

            using var ms = new MemoryStream([.. utf8Content]);
            ms.Position = 1;

            var output = await new EncodingDetector().DetectAsync(ms);

            output.EncodingName.Should().Be(Encoding.UTF8.EncodingName);
        }

        [Fact]
        public async Task DetectEncodingUTF16BE()
        {
            var utf16beContent = new List<byte>();
            utf16beContent.AddRange(Encoding.ASCII.GetBytes("("));
            utf16beContent.AddRange(Encoding.BigEndianUnicode.Preamble);
            utf16beContent.AddRange(Encoding.BigEndianUnicode.GetBytes("test"));
            utf16beContent.AddRange(Encoding.ASCII.GetBytes(")"));

            using var ms = new MemoryStream([.. utf16beContent]);
            ms.Position = 1;

            var output = await new EncodingDetector().DetectAsync(ms);

            output.EncodingName.Should().Be(Encoding.BigEndianUnicode.EncodingName);
        }

        [Fact]
        public async Task DetectEncodingUTF16BEOctalPreamble()
        {
            var utf16beContent = new List<byte>();
            utf16beContent.AddRange(Encoding.ASCII.GetBytes("("));
            utf16beContent.AddRange(Encoding.ASCII.GetBytes("\\376\\377"));
            utf16beContent.AddRange(Encoding.BigEndianUnicode.GetBytes("test"));
            utf16beContent.AddRange(Encoding.ASCII.GetBytes(")"));

            using var ms = new MemoryStream([.. utf16beContent]);
            ms.Position = 1;

            var output = await new EncodingDetector().DetectAsync(ms);

            output.EncodingName.Should().Be(Encoding.BigEndianUnicode.EncodingName);
        }

        [Fact]
        public async Task DetectEncodingPdfDoc()
        {
            var latinContent = Encoding.Latin1.GetBytes("(test)").ToList();

            using var ms = new MemoryStream([.. latinContent]);
            var output = await new EncodingDetector().DetectAsync(ms);

            output.EncodingName.Should().Be(Encoding.Latin1.EncodingName);
        }
    }
}
