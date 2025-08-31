using ZingPDF.Extensions;
using ZingPDF.Syntax.Filters;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Syntax.Objects.Streams;

/// <summary>
/// ISO 32000-2:2020 7.3.8 - Stream objects.
/// </summary>
/// <remarks>
/// A Stream object consists of a stream dictionary, followed by the stream data.
/// </remarks>
public sealed class StreamObject<TDictionary> : PdfObject, IStreamObject
    where TDictionary : class, IStreamDictionary
{
    public StreamObject(Stream data, TDictionary dictionary, ObjectContext context)
        : base(context)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));
        ArgumentNullException.ThrowIfNull(dictionary, nameof(dictionary));

        Data = data;
        Dictionary = dictionary;
    }

    public StreamObject(Stream data, TDictionary dictionary)
        : this(data, dictionary, ObjectContext.UserCreated)
    {
    }

    IStreamDictionary IStreamObject.Dictionary => Dictionary;

    public TDictionary Dictionary { get; }
    public Stream Data { get; }

    protected override async Task WriteOutputAsync(Stream stream)
    {
        await Dictionary.WriteAsync(stream);

        await stream.WriteNewLineAsync();

        Data.Position = 0;

        await new Keyword(Constants.StreamStart, Context).WriteAsync(stream);
        await stream.WriteNewLineAsync();

        await Data.CopyToAsync(stream);

        await stream.WriteNewLineAsync();
        await new Keyword(Constants.StreamEnd, Context).WriteAsync(stream);
    }

    public async Task<Stream> GetDecompressedDataAsync()
    {
        // TODO: stream contents may be encrypted, decrypt.

        Data.Position = 0;

        // If there are no filters, return the source data as-is.
        ArrayObject? filterNames = await Dictionary.Filter.GetAsync();
        if (filterNames is null || !filterNames.Any())
        {
            return Data;
        }

        IEnumerable<Dictionary> allFilterParams = (await Dictionary.DecodeParms.GetAsync() ?? []).Cast<Dictionary>();

        var ms = new MemoryStream();
        Data.Position = 0;
        Data.CopyTo(ms);
        ms.Position = 0;

        foreach (var filter in FilterFactory.CreateFilterInstances(filterNames.Cast<Name>(), allFilterParams))
        {
            ms = filter.Decode(ms);

            ms.Position = 0;
        }

        return ms;
    }

    public override object Clone()
    {
        var ms = new MemoryStream();
        Data.Position = 0;
        Data.CopyTo(ms);
        ms.Position = 0;

        return new StreamObject<TDictionary>(ms, (TDictionary)Dictionary.Clone(), Context);
    }
}
