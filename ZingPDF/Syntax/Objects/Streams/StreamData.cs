//using ZingPDF.Extensions;
//using ZingPDF.Syntax.Filters;
//using ZingPDF.Syntax.Objects.Dictionaries;

//namespace ZingPDF.Syntax.Objects.Streams;

//public class StreamData : PdfObject
//{
//    private readonly AsyncProperty<ShorthandArrayObject> _filters;
//    private readonly AsyncProperty<ShorthandArrayObject>? _decodeParms;

//    public StreamData(
//        Stream stream,
//        IStreamDictionary 
//        AsyncProperty<ShorthandArrayObject> filters,
//        AsyncProperty<ShorthandArrayObject>? decodeParms
//        )
//    {
//        ArgumentNullException.ThrowIfNull(stream, nameof(stream));
//        ArgumentNullException.ThrowIfNull(filters, nameof(filters));

//        Data = stream;

//        _filters = filters;
//        _decodeParms = decodeParms;
//    }

//    public Stream Data { get; }

//    protected override async Task WriteOutputAsync(Stream stream)
//    {
//        await new Keyword(Constants.StreamStart).WriteAsync(stream);

//        await stream.WriteNewLineAsync();

//        Data.Position = 0;

//        await Data.CopyToAsync(stream);

//        await stream.WriteNewLineAsync();

//        await new Keyword(Constants.StreamEnd).WriteAsync(stream);
//    }

//    public async Task<Stream> GetDecompressedDataAsync(IIndirectObjectDictionary indirectObjectDictionary)
//    {
//        ArgumentNullException.ThrowIfNull(indirectObjectDictionary, nameof(indirectObjectDictionary));

//        // TODO: stream contents may be encrypted, decrypt.

//        Data.Position = 0;

//        // If there are no filters, return the source data as-is.
//        if (_filters == null)
//        {
//            return Data;
//        }

//        var workingData = await Data.ReadToEndAsync();

//        var filterNames = await _filters.GetAsync(indirectObjectDictionary);
//        var allFilterParams = await (_decodeParms?.GetAsync(indirectObjectDictionary)
//            ?? Task.FromResult<ShorthandArrayObject>([]));

//        var filterInstances = FilterFactory.CreateFilterInstances(filterNames.Cast<Name>(), allFilterParams.Cast<Dictionary>());

//        foreach (var filter in filterInstances)
//        {
//            workingData = filter.Decode(workingData);
//        }

//        return new MemoryStream(workingData);
//    }

//    public StreamDictionary GetStreamDictionary()
//    {
//        var streamDictionary = new Dictionary<Name, IPdfObject>
//        {
//            { Constants.DictionaryKeys.Stream.Length, (Integer)Data.Length },
//            { Constants.DictionaryKeys.Stream.DL, (Integer)Data.Length }
//        };

//        if (Filters.Count == 0)
//        {
//            return StreamDictionary.FromDictionary(streamDictionary);
//        }

//        // TODO: consider encapsulating this common logic, there are many properties which can be a single item or array of such.
//        if (Filters.Count == 1)
//        {
//            streamDictionary.Add(Constants.DictionaryKeys.Stream.Filter, Filters.First().Name);
//        }
//        else
//        {
//            streamDictionary.Add(Constants.DictionaryKeys.Stream.Filter, new ArrayObject(Filters.Select(f => f.Name).ToArray()));
//        }

//        if (Filters.Any(f => f.Params != null))
//        {
//            if (Filters.Count == 1)
//            {
//                streamDictionary.Add(Constants.DictionaryKeys.Stream.DecodeParms, Filters.First().Params!);
//            }
//            else
//            {
//                streamDictionary.Add(Constants.DictionaryKeys.Stream.DecodeParms, new ArrayObject(Filters.Select<IFilter, IPdfObject>(f =>
//                {
//                    if (f.Params != null)
//                    {
//                        return f.Params;
//                    }
//                    else
//                    {
//                        return new Null();
//                    }
//                }).ToArray()));
//            }
//        }

//        return StreamDictionary.FromDictionary(streamDictionary);
//    }
//}

//public class StreamData2 : PdfObject
//{
//    public StreamData2( 
//        Stream stream,
//        bool dataIsCompressed,
//        AsyncProperty<ShorthandArrayObject>? filters = null,
//        AsyncProperty<ShorthandArrayObject>? decodeParms = null
//        )
//    {
//        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

//        if (dataIsCompressed && filters is null)
//        {
//            throw new ArgumentException("Stream data is compressed, but no filters specified.");
//        }

//        Data = stream;
//        Compressed = dataIsCompressed;
//        Filters = filters;
//        DecodeParms = decodeParms;
//    }

//    public Stream Data { get; }
//    public bool Compressed { get; }
//    public AsyncProperty<ShorthandArrayObject>? Filters { get; }
//    public AsyncProperty<ShorthandArrayObject>? DecodeParms { get; }

//    public async Task<Stream> GetDecompressedDataAsync(IIndirectObjectDictionary indirectObjectDictionary)
//    {
//        ArgumentNullException.ThrowIfNull(indirectObjectDictionary, nameof(indirectObjectDictionary));

//        // TODO: stream contents may be encrypted, decrypt.

//        Data.Position = 0;

//        // If there are no filters, return the source data as-is.
//        if (Filters == null)
//        {
//            return Data;
//        }

//        var workingData = await Data.ReadToEndAsync();

//        var filterNames = await Filters.GetAsync(indirectObjectDictionary);
//        var allFilterParams = await (DecodeParms?.GetAsync(indirectObjectDictionary)
//            ?? Task.FromResult<ShorthandArrayObject>([]));

//        var filterInstances = FilterFactory.CreateFilterInstances(filterNames.Cast<Name>(), allFilterParams.Cast<Dictionary>());

//        foreach (var filter in filterInstances)
//        {
//            workingData = filter.Decode(workingData);
//        }

//        return new MemoryStream(workingData);
//    }

//    protected override async Task WriteOutputAsync(Stream stream)
//    {
//        await new Keyword(Constants.StreamStart).WriteAsync(stream);

//        await stream.WriteNewLineAsync();

//        Data.Position = 0;

//        if (Compressed || Filters == null)
//        {
//            // Write data as-is if it is already compressed, or has no compression filters
//            await Data.CopyToAsync(stream);
//        }
//        else
//        {
//            // Otherwise, compress the data and write
//            var compressedData = await CompressDataAsync();
//            await compressedData.CopyToAsync(stream);
//        }
        
//        await stream.WriteNewLineAsync();

//        await new Keyword(Constants.StreamEnd).WriteAsync(stream);
//    }

//    private async Task<Stream> CompressDataAsync()
//    {
//        var workingData = await Data.ReadToEndAsync();

//        foreach (var filter in Filters)
//        {
//            workingData = filter.Encode(workingData);
//        }

//        return new MemoryStream(workingData);
//    }

//    public StreamDictionary GetStreamDictionary()
//    {
//        var streamDictionary = new Dictionary<Name, IPdfObject>();

//        if (Compressed)
//        {
//            streamDictionary.Add(Constants.DictionaryKeys.Stream.Length, (Integer)Data.Length);
//        }
//        else
//        {
//            streamDictionary.Add(Constants.DictionaryKeys.Stream.Length, (Integer)Data.Length);
//            streamDictionary.Add(Constants.DictionaryKeys.Stream.DL, (Integer)Data.Length);
//        }

//        if (Filters.Count == 0)
//        {
//            return StreamDictionary.FromDictionary(streamDictionary);
//        }

//        // TODO: consider encapsulating this common logic, there are many properties which can be a single item or array of such.
//        if (Filters.Count == 1)
//        {
//            streamDictionary.Add(Constants.DictionaryKeys.Stream.Filter, Filters.First().Name);
//        }
//        else
//        {
//            streamDictionary.Add(Constants.DictionaryKeys.Stream.Filter, new ArrayObject(Filters.Select(f => f.Name).ToArray()));
//        }

//        if (Filters.Any(f => f.Params != null))
//        {
//            if (Filters.Count == 1)
//            {
//                streamDictionary.Add(Constants.DictionaryKeys.Stream.DecodeParms, Filters.First().Params!);
//            }
//            else
//            {
//                streamDictionary.Add(Constants.DictionaryKeys.Stream.DecodeParms, new ArrayObject(Filters.Select<IFilter, IPdfObject>(f =>
//                {
//                    if (f.Params != null)
//                    {
//                        return f.Params;
//                    }
//                    else
//                    {
//                        return new Null();
//                    }
//                }).ToArray()));
//            }
//        }

//        return StreamDictionary.FromDictionary(streamDictionary);
//    }
//}
