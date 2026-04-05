using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.ContentStreamsAndResources;

/// <summary>
/// ISO 32000-2:2020 7.8.2 - Content streams
/// </summary>
/// <remarks>
/// This class is used to create a stream object from a collection of content stream objects.
/// </remarks>
internal class ContentStreamFactory : StreamObjectFactory
{
    private readonly IEnumerable<ContentStream> _content;

    public ContentStreamFactory(IEnumerable<ContentStream> content)
    {
        ArgumentNullException.ThrowIfNull(content, nameof(content));

        _content = content;
    }

    protected override async Task<Stream> GetDataAsync()
    {
        var ms = new MemoryStream();

        foreach (var graphicsObject in _content)
        {
            await graphicsObject.WriteAsync(ms);
            await ms.WriteWhitespaceAsync();
        }

        return ms;
    }
}
