using ZingPDF.Syntax.Filters;
using static ZingPDF.Constants;

namespace ZingPDF.Syntax.Objects.Streams;

/// <summary>
/// Factory for creating <see cref="StreamObject{TDictionary}"/> instances from source data."/>
/// </summary>
/// <remarks>
/// <para>This is an abstract class. The intent is for subclasses to implement the <see cref="GetData"/> method, providing the raw uncompressed data.</para> 
/// <para>To provide filters to be used to compress the stream, subclasses should implement the <see cref="GetFilters"/> method.</para>
/// <para>See <see cref="BasicStreamObjectFactory"/> for an example implementation.</para>
/// <para>Note: For creating <see cref="StreamObject{TDictionary}"/> instances from compressed data, use the <see cref="StreamObject{TDictionary}"/> constructor directly.</para>
/// </remarks>
internal abstract class StreamObjectFactory : IStreamObjectFactory
{
    protected abstract Task<Stream> GetDataAsync();
    protected virtual IEnumerable<FilterConfig> GetFilters() => [];

    public async Task<StreamObject<TDictionary>> CreateAsync<TDictionary>(TDictionary dictionary, ObjectContext context)
        where TDictionary : class, IStreamDictionary
    {
        var rawData = await GetDataAsync()
            ?? throw new InvalidOperationException("The stream data cannot be null.");

        var filters = GetFilters();

        var data = CompressDataIfRequired(rawData, filters);
        SetStreamDictionaryProperties(data.Length, rawData.Length, filters, dictionary, context);

        return new StreamObject<TDictionary>(data, dictionary);
    }

    private static Stream CompressDataIfRequired(Stream rawData, IEnumerable<FilterConfig> filters)
    {
        if (!filters.Any())
        {
            return rawData;
        }

        var ms = new MemoryStream();
        rawData.Position = 0;
        rawData.CopyTo(ms);

        foreach (var filter in filters.Select(f => FilterFactory.Create(f.FilterName, f.DecodeParms)))
        {
            ms = filter.Encode(ms);

            ms.Position = 0;
        }

        return ms;
    }

    private static TDictionary SetStreamDictionaryProperties<TDictionary>(
        long compressedLength,
        long uncompressedLength,
        IEnumerable<FilterConfig> filters,
        TDictionary dictionary,
        ObjectContext context
        )
        where TDictionary : class, IStreamDictionary
    {
        dictionary.Set<Number>(DictionaryKeys.Stream.Length, compressedLength);
        dictionary.Set<Number>(DictionaryKeys.Stream.DL, uncompressedLength);
        
        if (!filters.Any())
        {
            return dictionary;
        }

        dictionary.Set(DictionaryKeys.Stream.Filter, new ShorthandArrayObject(filters.Select(f => f.FilterName), context));

        if (filters.Any(f => f.DecodeParms != null))
        {
            dictionary.Set(
                DictionaryKeys.Stream.DecodeParms,
                new ShorthandArrayObject(filters.Select(f => (IPdfObject)f.DecodeParms ?? new Null(context)), context)
                );
        }

        return dictionary;
    }
}
