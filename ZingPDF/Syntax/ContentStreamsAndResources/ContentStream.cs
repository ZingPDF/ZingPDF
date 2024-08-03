using ZingPDF.Extensions;
using ZingPDF.Syntax.Filters;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.ContentStreamsAndResources;

internal class ContentStream(
    IEnumerable<ContentStreamObject> graphicsObjects,
    IEnumerable<IFilter>? filters,
    bool sourceDataIsCompressed
    )
    : ContentStream<StreamDictionary>(graphicsObjects, filters, sourceDataIsCompressed)
{
    protected override StreamDictionary GetSpecialisedDictionary()
        => StreamDictionary.FromDictionary(new Dictionary<Name, IPdfObject>());
}

/// <summary>
/// ISO 32000-2:2020 7.8.2 - Content streams
/// </summary>
internal abstract class ContentStream<TDictionary> : StreamObject<TDictionary> where TDictionary : class, IStreamDictionary
{
    private readonly IEnumerable<ContentStreamObject> _graphicsObjects;

    public ContentStream(
        IEnumerable<ContentStreamObject> graphicsObjects,
        IEnumerable<IFilter>? filters,
        bool sourceDataIsCompressed
        )
        : base(filters, sourceDataIsCompressed)
    {
        _graphicsObjects = graphicsObjects ?? throw new ArgumentNullException(nameof(graphicsObjects));
    }

    protected override async Task<Stream> GetSourceDataAsync(TDictionary dictionary)
    {
        var ms = new MemoryStream();

        foreach (var graphicsObject in _graphicsObjects)
        {
            await graphicsObject.WriteAsync(ms);
            await ms.WriteWhitespaceAsync();
        }

        ms.Position = 0;

        return ms;
    }
}
