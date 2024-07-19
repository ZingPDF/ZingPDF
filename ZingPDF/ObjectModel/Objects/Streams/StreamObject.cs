using Nito.AsyncEx;
using ZingPDF.Extensions;
using ZingPDF.ObjectModel.Filters;

namespace ZingPDF.ObjectModel.Objects.Streams;

/// <summary>
/// ISO 32000-2:2020 7.3.8 - Stream objects.<para></para>
/// This is an abstract class.
/// </summary>
/// <remarks>
/// During rendering, this class will call the abstract method <see cref="GetSpecialisedDictionaryAsync"/>
/// to retrieve the specialised stream dictionary for the subclass. This dictionary must implement <see cref="IStreamDictionary"/>.<para></para>
/// This class will also call the abstract method <see cref="GetSourceDataAsync(TDictionary)"/> to generate the stream data.<para></para>
/// Finally, this class will produce a <see cref="StreamDictionary"/> and merge it into the specialised dictionary.
/// </remarks>
internal abstract class StreamObject<TDictionary> : PdfObject, IStreamObject<TDictionary> where TDictionary : class, IStreamDictionary
{
    protected readonly AsyncLazy<TDictionary> _specialisedDictionary;
    protected readonly AsyncLazy<Stream> _sourceData;
    protected readonly AsyncLazy<Stream> _compressedData;

    private readonly IEnumerable<IFilter> _filters;

    public TDictionary Dictionary
    {
        get
        {
            if (!Written)
            {
                throw new InvalidOperationException("Stream dictionary property is not available until the object has been written");
            }

            return _specialisedDictionary.Task.Result;
        }
    }

    /// <summary>
    /// Construct a new <see cref="StreamObject{TDictionary}"/>.
    /// </summary>
    protected StreamObject(IEnumerable<IFilter>? filters)
    {
        _filters = filters ?? [];

        _specialisedDictionary = new AsyncLazy<TDictionary>(GetSpecialisedDictionaryAsync);
        _sourceData = new AsyncLazy<Stream>(async () => await GetSourceDataAsync(await _specialisedDictionary));
        _compressedData = new AsyncLazy<Stream>(CompressDataAsync);
    }

    /// <summary>
    /// When overridden in a derived class this method returns the specialised dictionary for the subclass.
    /// </summary>
    protected abstract Task<TDictionary> GetSpecialisedDictionaryAsync();

    /// <summary>
    /// When overriden in a derived class this method returns the uncompressed data stream.
    /// </summary>
    protected abstract Task<Stream> GetSourceDataAsync(TDictionary dictionary);

    protected override async Task WriteOutputAsync(Stream stream)
    {
        // Example subclass: CrossReferenceStreamObject
        // to produce the stream data we need the xref dict
        // so, first, produce the specialised xref dict
        // then, produce data, providing xref dict to method
        // then, produce stream dict, merge with xref dict

        var specialisedDict = await _specialisedDictionary;
        var streamData = await _sourceData;
        var compressedData = await _compressedData;

        var streamDict = new Dictionary(
            (await CreateBaseStreamDictionaryAsync(compressedData.Length, streamData.Length))
            .MergeInto(specialisedDict)
            );

        await streamDict.WriteAsync(stream);

        await stream.WriteNewLineAsync();
        await new Keyword(Constants.StreamStart).WriteAsync(stream);

        await stream.WriteNewLineAsync();
        await compressedData.CopyToAsync(stream);
        await stream.WriteNewLineAsync();

        await new Keyword(Constants.StreamEnd).WriteAsync(stream);
    }

    public Task<Stream> GetDecompressedDataAsync() => _sourceData.Task;

    private async Task<Stream> CompressDataAsync()
    {
        var workingData = await (await _sourceData).ReadToEndAsync();

        foreach (var filter in _filters)
        {
            workingData = filter.Encode(workingData);
        }

        return new MemoryStream(workingData);
    }

    private Task<IStreamDictionary> CreateBaseStreamDictionaryAsync(long encodedLength, long? unencodedLength)
    {
        var streamDictionary = new Dictionary<Name, IPdfObject>()
        {
            { Constants.DictionaryKeys.Stream.Length, new Integer(encodedLength) },
        };

        if (unencodedLength is not null)
        {
            streamDictionary.Add(Constants.DictionaryKeys.Stream.DL, new Integer(unencodedLength.Value));
        }

        if (_filters.Any())
        {
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
        }

        return Task.FromResult<IStreamDictionary>(StreamDictionary.FromDictionary(streamDictionary));
    }
}
