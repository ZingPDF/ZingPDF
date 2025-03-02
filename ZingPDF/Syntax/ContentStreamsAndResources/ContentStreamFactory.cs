using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.ContentStreamsAndResources;

/// <summary>
/// ISO 32000-2:2020 7.8.2 - Content streams
/// </summary>
internal class ContentStreamFactory<TDictionary> : IStreamObjectFactory<TDictionary>
    where TDictionary : class, IStreamDictionary
{
    private readonly IEnumerable<ContentStream> _content;
    private readonly TDictionary _dictionary;

    public ContentStreamFactory(IEnumerable<ContentStream> content, TDictionary dictionary)
    {
        ArgumentNullException.ThrowIfNull(content, nameof(content));
        ArgumentNullException.ThrowIfNull(dictionary, nameof(dictionary));

        _content = content;
        _dictionary = dictionary;
    }

    public StreamObject<TDictionary> Create()
    {
        var ms = new MemoryStream();

        foreach (var graphicsObject in _content)
        {
            graphicsObject.WriteAsync(ms).Wait();
            ms.WriteWhitespaceAsync().Wait();
        }

        return new StreamObject<TDictionary>(ms, _dictionary);
    }
}
