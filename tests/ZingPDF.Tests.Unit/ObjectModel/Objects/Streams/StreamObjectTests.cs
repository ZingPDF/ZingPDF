using System.Text;
using FakeItEasy;
using FluentAssertions;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Filters;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Tests.Unit.ObjectModel.Objects.Streams;

public class StreamObjectTests
{
    [Fact]
    public async Task GetDecompressedDataAsync_WhenCalledRepeatedlyOnFilteredStream_ReturnsDecodedContentEachTime()
    {
        const string inputText = "Hello cached stream";
        using var input = new MemoryStream(Encoding.ASCII.GetBytes(inputText));
        using var encoded = new FlateDecodeFilter(null).Encode(input);

        var pdf = A.Dummy<IPdf>();
        var context = ObjectContext.None;
        var dictionary = new StreamDictionary(pdf, context);
        dictionary.Set(Constants.DictionaryKeys.Stream.Length, (Number)encoded.Length);
        dictionary.Set(Constants.DictionaryKeys.Stream.Filter, new Name(Constants.Filters.Flate, context));

        var streamObject = new StreamObject<StreamDictionary>(encoded, dictionary, context);

        await using (var first = await streamObject.GetDecompressedDataAsync())
        {
            (await first.GetAsync(inputText.Length)).Should().Be(inputText);
        }

        await using (var second = await streamObject.GetDecompressedDataAsync())
        {
            (await second.GetAsync(inputText.Length)).Should().Be(inputText);
        }
    }
}
