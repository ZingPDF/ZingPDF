using ZingPDF.Extensions;
using ZingPDF.Syntax.Filters;

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
    public StreamObject(Stream data, TDictionary dictionary)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));
        ArgumentNullException.ThrowIfNull(dictionary, nameof(dictionary));

        Data = data;
        Dictionary = dictionary;

        //Dictionary.SetStreamProperties(GetStreamDictionary());
    }

    public TDictionary Dictionary { get; }
    public Stream Data { get; }

    protected override async Task WriteOutputAsync(Stream stream)
    {
        await Dictionary.WriteAsync(stream);

        await stream.WriteNewLineAsync();

        Data.Position = 0;

        await Data.CopyToAsync(stream);
    }

    public async Task<Stream> GetDecompressedDataAsync(IIndirectObjectDictionary indirectObjectDictionary)
    {
        ArgumentNullException.ThrowIfNull(indirectObjectDictionary, nameof(indirectObjectDictionary));

        // TODO: stream contents may be encrypted, decrypt.

        Data.Position = 0;

        // If there are no filters, return the source data as-is.
        if (Dictionary.Filter == null)
        {
            return Data;
        }

        var workingData = await Data.ReadToEndAsync();

        var filterNames = await Dictionary.Filter.GetAsync(indirectObjectDictionary);

        ShorthandArrayObject allFilterParams = [];
        
        if (Dictionary.DecodeParms != null)
        {
            allFilterParams = await Dictionary.DecodeParms.GetAsync(indirectObjectDictionary);
        }

        var filterInstances = FilterFactory.CreateFilterInstances(filterNames.Cast<Name>(), allFilterParams.Cast<Dictionaries.Dictionary>());

        foreach (var filter in filterInstances)
        {
            workingData = filter.Decode(workingData);
        }

        return new MemoryStream(workingData);
    }

    //public StreamDictionary GetStreamDictionary()
    //{
    //    var streamDictionary = new Dictionary<Name, IPdfObject>
    //    {
    //        { Constants.DictionaryKeys.Stream.Length, (Integer)Data.Length },
    //        { Constants.DictionaryKeys.Stream.DL, (Integer)Data.Length }
    //    };

    //    if (Filters.Count == 0)
    //    {
    //        return StreamDictionary.FromDictionary(streamDictionary);
    //    }

    //    // TODO: consider encapsulating this common logic, there are many properties which can be a single item or array of such.
    //    if (Filters.Count == 1)
    //    {
    //        streamDictionary.Add(Constants.DictionaryKeys.Stream.Filter, Filters.First().Name);
    //    }
    //    else
    //    {
    //        streamDictionary.Add(Constants.DictionaryKeys.Stream.Filter, new ArrayObject(Filters.Select(f => f.Name).ToArray()));
    //    }

    //    if (Filters.Any(f => f.Params != null))
    //    {
    //        if (Filters.Count == 1)
    //        {
    //            streamDictionary.Add(Constants.DictionaryKeys.Stream.DecodeParms, Filters.First().Params!);
    //        }
    //        else
    //        {
    //            streamDictionary.Add(Constants.DictionaryKeys.Stream.DecodeParms, new ArrayObject(Filters.Select<IFilter, IPdfObject>(f =>
    //            {
    //                if (f.Params != null)
    //                {
    //                    return f.Params;
    //                }
    //                else
    //                {
    //                    return new Null();
    //                }
    //            }).ToArray()));
    //        }
    //    }

    //    return StreamDictionary.FromDictionary(streamDictionary);
    //}
}
