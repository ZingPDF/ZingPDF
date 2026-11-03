using System.Text;
using FluentAssertions;
using Xunit;
using ZingPDF.Parsing.Parsers.Objects;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Tests.Unit;

public class StreamBinaryDelimiterTests
{
    [Theory]
    [InlineData("\n", 32)] [InlineData("\n", 9)] [InlineData("\n", 10)]
    [InlineData("\r\n", 32)] [InlineData("\r\n", 13)] [InlineData("\r", 32)]
    public async Task BinaryPayload_LeadingWhitespaceIsData_NotPartOfStreamDelimiter(string delimiter, byte first)
    {
        using var pdf = Pdf.Create();
        var bytes = new byte[] { first, 9, 10, 13, 32, 0, 255, 65 };
        var dictionary = new StreamDictionary(pdf, ObjectContext.UserCreated);
        dictionary.Set("Length", (Number)bytes.Length);
        using var input = new MemoryStream();
        input.Write(Encoding.ASCII.GetBytes("stream" + delimiter)); input.Write(bytes);
        input.Write(Encoding.ASCII.GetBytes("\nendstream")); input.Position = 0;
        var parsed = await new StreamObjectParser<StreamDictionary>(dictionary, null).ParseAsync(input, ObjectContext.None);
        using var decoded = await parsed.GetDecompressedDataAsync();
        using var actual = new MemoryStream(); await decoded.CopyToAsync(actual);
        actual.ToArray().Should().Equal(bytes);
    }
}
