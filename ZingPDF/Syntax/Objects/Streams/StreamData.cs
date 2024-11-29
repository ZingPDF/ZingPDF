using ZingPDF.Extensions;
using ZingPDF.Syntax.Filters;

namespace ZingPDF.Syntax.Objects.Streams;

internal class StreamData : PdfObject
{
    public StreamData(Stream stream, bool dataIsCompressed, IEnumerable<IFilter>? filters = null)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        if (dataIsCompressed && (filters is null || !filters.Any()))
        {
            throw new ArgumentException("Stream data is compressed, but no filters specified.");
        }

        Data = stream;
        Compressed = dataIsCompressed;
        Filters = filters?.ToList() ?? [];
    }

    public Stream Data { get; }
    public bool Compressed { get; }
    public List<IFilter> Filters { get; }

    public async Task<Stream> GetDecompressedDataAsync()
    {
        // TODO: stream contents may be encrypted, decrypt.

        // If there are no filters, return the source data as-is.
        if (Filters.Count == 0)
        {
            return Data;
        }

        Data.Position = 0;

        var workingData = await Data.ReadToEndAsync();

        foreach (var filter in Filters)
        {
            workingData = filter.Decode(workingData);
        }

        return new MemoryStream(workingData);
    }

    protected override async Task WriteOutputAsync(Stream stream)
    {
        await new Keyword(Constants.StreamStart).WriteAsync(stream);

        await stream.WriteNewLineAsync();

        if (Compressed || Filters.Count == 0)
        {
            // Write data as-is if it is already compressed, or has no compression filters
            await Data.CopyToAsync(stream);
        }
        else
        {
            // Otherwise, compress the data and write
            var compressedData = await CompressDataAsync();
            await compressedData.CopyToAsync(stream);
        }
        
        await stream.WriteNewLineAsync();

        await new Keyword(Constants.StreamEnd).WriteAsync(stream);
    }

    private async Task<Stream> CompressDataAsync()
    {
        var workingData = await Data.ReadToEndAsync();

        foreach (var filter in Filters)
        {
            workingData = filter.Encode(workingData);
        }

        return new MemoryStream(workingData);
    }

    public StreamDictionary GetStreamDictionary()
    {
        var streamDictionary = new Dictionary<Name, IPdfObject>();

        if (Compressed)
        {
            streamDictionary.Add(Constants.DictionaryKeys.Stream.Length, (Integer)Data.Length);
        }
        else
        {
            streamDictionary.Add(Constants.DictionaryKeys.Stream.Length, (Integer)Data.Length);
            streamDictionary.Add(Constants.DictionaryKeys.Stream.DL, (Integer)Data.Length);
        }

        if (Filters.Count == 0)
        {
            return StreamDictionary.FromDictionary(streamDictionary);
        }

        streamDictionary.Add(Constants.DictionaryKeys.Stream.Filter, new ArrayObject(Filters.Select(f => f.Name).ToArray()));

        if (Filters.Any(f => f.Params != null))
        {
            if (Filters.Count == 1)
            {
                streamDictionary.Add(Constants.DictionaryKeys.Stream.DecodeParms, Filters.First().Params!);
            }
            else
            {
                streamDictionary.Add(Constants.DictionaryKeys.Stream.DecodeParms, new ArrayObject(Filters.Select<IFilter, IPdfObject>(f =>
                {
                    if (f.Params != null)
                    {
                        return f.Params;
                    }
                    else
                    {
                        return new Null();
                    }
                }).ToArray()));
            }
        }

        return StreamDictionary.FromDictionary(streamDictionary);
    }
}
