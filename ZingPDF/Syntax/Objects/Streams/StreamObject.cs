using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects.Streams;

/// <summary>
/// ISO 32000-2:2020 7.3.8 - Stream objects.<para></para>
/// </summary>
/// <remarks>
/// A Stream object consists of a stream dictionary, followed by the stream data.
/// </remarks>
public sealed class StreamObject<TDictionary> : PdfObject
    where TDictionary : class, IStreamDictionary
{
    public StreamObject(StreamData data, TDictionary dictionary)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));
        ArgumentNullException.ThrowIfNull(dictionary, nameof(dictionary));

        Data = data;
        Dictionary = dictionary;

        Dictionary.SetStreamProperties(Data.GetStreamDictionary());
    }

    public TDictionary Dictionary { get; }
    public StreamData Data { get; }

    protected override async Task WriteOutputAsync(Stream stream)
    {
        await Dictionary.WriteAsync(stream);

        await stream.WriteNewLineAsync();

        Data.Data.Position = 0;

        await Data.WriteAsync(stream);
    }
}
