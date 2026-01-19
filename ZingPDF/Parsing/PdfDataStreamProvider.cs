using System;
using System.Collections.Generic;
using System.IO;
using ZingPDF;

namespace ZingPDF.Parsing;

internal interface IPdfDataStreamProvider
{
    Stream OpenRead();
}

internal sealed class PdfDataStreamProvider : IPdfDataStreamProvider, IDisposable
{
    private readonly Stream _data;
    private readonly Func<Stream> _streamFactory;
    private readonly List<Stream> _openStreams = [];
    private readonly object _openStreamsLock = new();

    public PdfDataStreamProvider(IPdf pdf)
    {
        ArgumentNullException.ThrowIfNull(pdf);

        _data = pdf.Data;
        _streamFactory = CreateStreamFactory(_data);
    }

    public Stream OpenRead()
    {
        var stream = _streamFactory();

        lock (_openStreamsLock)
        {
            _openStreams.Add(stream);
        }

        return stream;
    }

    public void Dispose()
    {
        List<Stream> streamsToDispose;

        lock (_openStreamsLock)
        {
            streamsToDispose = [.. _openStreams];
            _openStreams.Clear();
        }

        foreach (var stream in streamsToDispose)
        {
            stream.Dispose();
        }
    }

    private static Func<Stream> CreateStreamFactory(Stream data)
    {
        if (data is FileStream fileStream)
        {
            var path = fileStream.Name;

            return () => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        if (data is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var buffer))
        {
            return () => new MemoryStream(buffer.Array!, buffer.Offset, buffer.Count, writable: false, publiclyVisible: true);
        }

        var originalPosition = data.Position;
        data.Position = 0;

        using var temp = new MemoryStream();
        data.CopyTo(temp);

        data.Position = originalPosition;

        var bytes = temp.ToArray();

        return () => new MemoryStream(bytes, writable: false);
    }
}
