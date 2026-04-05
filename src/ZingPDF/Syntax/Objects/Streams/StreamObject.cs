using ZingPDF.Extensions;
using ZingPDF.Diagnostics;
using ZingPDF.Syntax.Encryption;
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
    private readonly IPdfEncryptionProvider? _encryptionProvider;
    private readonly object _decodedDataLock = new();
    private Task<byte[]>? _decodedDataTask;

    public StreamObject(Stream data, TDictionary dictionary, ObjectContext context)
        : this(data, dictionary, context, null)
    {
    }

    public StreamObject(Stream data, TDictionary dictionary)
        : this(data, dictionary, ObjectContext.UserCreated)
    {
    }

    internal StreamObject(Stream data, TDictionary dictionary, ObjectContext context, IPdfEncryptionProvider? encryptionProvider)
        : base(context)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));
        ArgumentNullException.ThrowIfNull(dictionary, nameof(dictionary));

        Data = data;
        Dictionary = dictionary;
        _encryptionProvider = encryptionProvider;
    }

    IStreamDictionary IStreamObject.Dictionary => Dictionary;

    public TDictionary Dictionary { get; }
    public Stream Data { get; }

    protected override async Task WriteOutputAsync(Stream stream)
    {
        if (Data.CanSeek)
        {
            Dictionary.Set(Constants.DictionaryKeys.Stream.Length, (Number)Data.Length);
        }

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
        using var trace = PerformanceTrace.Measure("StreamObject.GetDecompressedDataAsync");
        Data.Position = 0;

        ArrayObject? filterNames = await Dictionary.Filter.GetAsync();
        var hasFilters = filterNames is not null && filterNames.Any();

        if (_encryptionProvider is null)
        {
            // When no decryption is required, decode directly from the backing stream
            // instead of copying the stream contents into a new buffer first.
            Stream stream = new NonDisposingStreamView(Data);

            if (!hasFilters)
            {
                return stream;
            }

            IEnumerable<Dictionary> allFilterParams = (await Dictionary.DecodeParms.GetAsync() ?? []).Cast<Dictionary>();

            foreach (var filter in FilterFactory.CreateFilterInstances(filterNames!.Cast<Name>(), allFilterParams))
            {
                stream = filter.Decode(stream);
                stream.Position = 0;
            }

            return stream;
        }

        var decodedData = await GetOrCreateDecodedDataAsync(filterNames, hasFilters);
        return new MemoryStream(decodedData, writable: false);
    }

    private Task<byte[]> GetOrCreateDecodedDataAsync(ArrayObject? filterNames, bool hasFilters)
    {
        lock (_decodedDataLock)
        {
            return _decodedDataTask ??= DecodeToBytesAsync(filterNames, hasFilters);
        }
    }

    private async Task<byte[]> DecodeToBytesAsync(ArrayObject? filterNames, bool hasFilters)
    {
        if (_encryptionProvider is null)
        {
            Stream stream = new NonDisposingStreamView(Data);

            if (!hasFilters)
            {
                return await stream.ReadToEndAsync();
            }

            IEnumerable<Dictionary> allFilterParams = (await Dictionary.DecodeParms.GetAsync() ?? []).Cast<Dictionary>();

            foreach (var filter in FilterFactory.CreateFilterInstances(filterNames!.Cast<Name>(), allFilterParams))
            {
                stream = filter.Decode(stream);
                stream.Position = 0;
            }

            return await stream.ReadToEndAsync();
        }

        using var ms = new MemoryStream();
        await Data.CopyToAsync(ms);
        var dataBytes = await _encryptionProvider.DecryptObjectBytesAsync(Context, ms.ToArray(), Dictionary);

        if (!hasFilters)
        {
            return dataBytes;
        }

        Stream decryptedStream = new MemoryStream(dataBytes, writable: false);
        IEnumerable<Dictionary> filterParams = (await Dictionary.DecodeParms.GetAsync() ?? []).Cast<Dictionary>();

        foreach (var filter in FilterFactory.CreateFilterInstances(filterNames!.Cast<Name>(), filterParams))
        {
            decryptedStream = filter.Decode(decryptedStream);
            decryptedStream.Position = 0;
        }

        return await decryptedStream.ReadToEndAsync();
    }

    public override object Clone()
    {
        var ms = new MemoryStream();
        Data.Position = 0;
        Data.CopyTo(ms);
        ms.Position = 0;

        return new StreamObject<TDictionary>(ms, (TDictionary)Dictionary.Clone(), Context);
    }

    private sealed class NonDisposingStreamView : Stream
    {
        private readonly Stream _source;

        public NonDisposingStreamView(Stream source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _source.Position = 0;
        }

        public override bool CanRead => _source.CanRead;
        public override bool CanSeek => _source.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _source.Length;

        public override long Position
        {
            get => _source.Position;
            set => _source.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count) => _source.Read(buffer, offset, count);

        public override int ReadByte() => _source.ReadByte();

        public override long Seek(long offset, SeekOrigin origin) => _source.Seek(offset, origin);

        public override void Flush() => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            // Callers often dispose the stream returned by GetDecompressedDataAsync().
            // This wrapper prevents those usages from closing the underlying PDF stream.
            // Intentionally leave the source stream open.
            base.Dispose(disposing);
        }
    }
}
