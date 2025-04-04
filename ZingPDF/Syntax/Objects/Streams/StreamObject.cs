using ZingPDF.Extensions;
using ZingPDF.Syntax.Filters;

namespace ZingPDF.Syntax.Objects.Streams;

/// <summary>
/// ISO 32000-2:2020 7.3.8 - Stream objects.
/// </summary>
/// <remarks>
/// A Stream object consists of a stream dictionary, followed by the stream data.
/// </remarks>
public sealed class StreamObject<TDictionary> : PdfObject
    where TDictionary : class, IStreamDictionary
{
    public StreamObject(Stream data, TDictionary dictionary)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));
        ArgumentNullException.ThrowIfNull(dictionary, nameof(dictionary));

        Data = data;
        Dictionary = dictionary;
    }

    public TDictionary Dictionary { get; }
    public Stream Data { get; }

    protected override async Task WriteOutputAsync(Stream stream)
    {
        await Dictionary.WriteAsync(stream);

        await stream.WriteNewLineAsync();

        Data.Position = 0;

        await new Keyword(Constants.StreamStart).WriteAsync(stream);
        await stream.WriteNewLineAsync();

        await Data.CopyToAsync(stream);

        await stream.WriteNewLineAsync();
        await new Keyword(Constants.StreamEnd).WriteAsync(stream);
    }

    public async Task<Stream> GetDecompressedDataAsync()
    {
        // TODO: stream contents may be encrypted, decrypt.

        Data.Position = 0;

        // If there are no filters, return the source data as-is.
        if (Dictionary.Filter == null)
        {
            return Data;
        }

        IEnumerable<Name> filterNames = [];
        Either<Name?, ArrayObject?> filterValue = await Dictionary.Filter.GetAsync();

        if (filterValue.Value != null)
        {
            filterNames = filterValue.Type1 != null ? [filterValue.Type1] : filterValue.Type2!.Cast<Name>();
        }

        if (!filterNames.Any())
        {
            return Data;
        }

        IEnumerable<Dictionaries.Dictionary> allFilterParams = [];
        Either<Dictionaries.Dictionary?, ArrayObject?> decodeParmsValue = await Dictionary.DecodeParms.GetAsync();

        if (decodeParmsValue.Value != null)
        {
            allFilterParams = decodeParmsValue.Type1 != null
                ? [decodeParmsValue.Type1]
                : decodeParmsValue.Type2!.Cast<Dictionaries.Dictionary>();
        }

        var ms = new MemoryStream();
        Data.Position = 0;
        Data.CopyTo(ms);
        ms.Position = 0;

        foreach (var filter in FilterFactory.CreateFilterInstances(filterNames, allFilterParams.Cast<Dictionaries.Dictionary>()))
        {
            ms = filter.Decode(ms);

            ms.Position = 0;
        }

        return ms;
    }
}
