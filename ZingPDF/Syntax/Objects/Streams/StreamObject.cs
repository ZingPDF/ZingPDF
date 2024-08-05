using Nito.AsyncEx;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Filters;

namespace ZingPDF.Syntax.Objects.Streams;

/// <summary>
/// ISO 32000-2:2020 7.3.8 - Stream objects.<para></para>
/// This is an abstract class.
/// </summary>
/// <remarks>
/// During rendering, this class will call the abstract method <see cref="GetSpecialisedDictionary"/>
/// to retrieve the specialised stream dictionary for the subclass. This dictionary must implement <see cref="IStreamDictionary"/>.<para></para>
/// This class will also call the abstract method <see cref="GetSourceDataAsync(TDictionary)"/> to generate the stream data.<para></para>
/// Finally, this class will produce a <see cref="StreamDictionary"/> and merge it into the specialised dictionary.
/// </remarks>
internal abstract class StreamObject<TDictionary> : PdfObject, IStreamObject<TDictionary>
    where TDictionary : class, IStreamDictionary
{
    private readonly IEnumerable<IFilter> _filters;
    private readonly bool _sourceDataIsCompressed;
    private readonly Dictionary<Name, IPdfObject> _streamDictionary;
    private TDictionary? _specialisedDictionary;

    protected readonly AsyncLazy<Stream> _sourceData;
    protected readonly AsyncLazy<Stream> _compressedData;

    /// <summary>
    /// Construct a new <see cref="StreamObject{TDictionary}"/>.
    /// </summary>
    protected StreamObject(IEnumerable<IFilter>? filters, bool sourceDataIsCompressed)
    {
        _filters = filters ?? [];
        _sourceDataIsCompressed = sourceDataIsCompressed;

        if (_sourceDataIsCompressed && !_filters.Any())
        {
            throw new ArgumentException("Data is compressed, but no filters provided");
        }

        // First produce a basic stream dictionary from the supplied filters.
        // This won't contain Length or DL properties until the object is written.
        _streamDictionary = InitialiseStreamDictionary();

        _sourceData = new AsyncLazy<Stream>(async () => await GetSourceDataAsync(_specialisedDictionary!));
        _compressedData = new AsyncLazy<Stream>(CompressDataAsync);
    }

    public TDictionary Dictionary
    {
        get
        {
            if (!Written)
            {
                throw new InvalidOperationException("Stream dictionary property is not available until the object has been written");
            }

            return _specialisedDictionary!;
        }
    }

    /// <summary>
    /// When overridden in a derived class this method returns the specialised dictionary for the subclass.
    /// </summary>
    protected abstract TDictionary GetSpecialisedDictionary();

    /// <summary>
    /// When overriden in a derived class this method returns the uncompressed data stream.
    /// </summary>
    protected abstract Task<Stream> GetSourceDataAsync(TDictionary dictionary);

    protected override async Task WriteOutputAsync(Stream stream)
    {
        // Example subclass: CrossReferenceStreamObject
        // To produce the stream data we need the xref dict so first, produce the specialised xref dict.
        // During rendering, GetSourceDataAsync and CompressDataAsync (if required) will be called.
        // Length and DL will be set on _streamDictionary which is then merged into _specialisedDictionary.
        _specialisedDictionary = GetSpecialisedDictionary();

        var streamData = await _sourceData;
        var compressedData = await _compressedData;

        _streamDictionary[Constants.DictionaryKeys.Stream.Length] = (Integer)compressedData.Length;
        _streamDictionary[Constants.DictionaryKeys.Stream.DL] = (Integer)streamData.Length;

        _specialisedDictionary.SetStreamProperties(_streamDictionary);

        await _specialisedDictionary.WriteAsync(stream);

        await stream.WriteNewLineAsync();
        await new Keyword(Constants.StreamStart).WriteAsync(stream);

        await stream.WriteNewLineAsync();
        await compressedData.CopyToAsync(stream);
        await stream.WriteNewLineAsync();

        await new Keyword(Constants.StreamEnd).WriteAsync(stream);
    }

    public async Task<Stream> GetDecompressedDataAsync()
    {
        if (!_sourceDataIsCompressed)
        {
            return await _sourceData;
        }
        
        var workingData = await (await _sourceData).ReadToEndAsync();

        foreach (var filter in _filters)
        {
            workingData = filter.Decode(workingData);
        }

        return new MemoryStream(workingData);
    }

    private async Task<Stream> CompressDataAsync()
    {
        if (_sourceDataIsCompressed)
        {
            return await _sourceData;
        }

        if (!_filters.Any())
        {
            return await _sourceData;
        }

        var workingData = await (await _sourceData).ReadToEndAsync();

        foreach (var filter in _filters)
        {
            workingData = filter.Encode(workingData);
        }

        return new MemoryStream(workingData);
    }

    private Dictionary<Name, IPdfObject> InitialiseStreamDictionary()
    {
        var streamDictionary = new Dictionary<Name, IPdfObject>();

        if (!_filters.Any())
        {
            return streamDictionary;
        }

        streamDictionary.Add("Filter", new ArrayObject(_filters.Select(f => f.Name).ToArray()));

        if (_filters.Any(f => f.Params != null))
        {
            if (_filters.Count() == 1)
            {
                streamDictionary.Add("DecodeParms", _filters.First().Params!);
            }
            else
            {
                streamDictionary.Add("DecodeParms", new ArrayObject(_filters.Select<IFilter, IPdfObject>(f =>
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

        return streamDictionary;
    }
}
