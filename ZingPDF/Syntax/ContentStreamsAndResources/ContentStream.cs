using ZingPDF.Syntax.Filters;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.ContentStreamsAndResources;

/// <summary>
/// ISO 32000-2:2020 7.8.2 - Content streams
/// </summary>
internal class ContentStream<TDictionary> : StreamObject<TDictionary> where TDictionary : class, IStreamDictionary
{
    private readonly IEnumerable<PdfObject> _graphicsObjects;

    public ContentStream(
        IEnumerable<PdfObject> graphicsObjects,
        IEnumerable<IFilter>? filters = null
        )
        : base(filters)
    {
        _graphicsObjects = graphicsObjects ?? throw new ArgumentNullException(nameof(graphicsObjects));
    }

    protected override async Task<Stream> GetSourceDataAsync(TDictionary dictionary)
    {
        var ms = new MemoryStream();

        foreach (var graphicsObject in _graphicsObjects)
        {
            await graphicsObject.WriteAsync(ms);
        }

        ms.Position = 0;

        return ms;
    }

    protected override Task<TDictionary> GetSpecialisedDictionaryAsync() => Task.FromResult((TDictionary)StreamDictionary.Empty());
}
