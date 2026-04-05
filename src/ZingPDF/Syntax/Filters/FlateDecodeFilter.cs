using System.IO.Compression;
using ZingPDF.Syntax.Filters.FilterUtils;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Syntax.Filters;

internal class FlateDecodeFilter : IFilter
{
    private const int _defaultPredictor = 1;
    private const int _defaultColors = 1;
    private const int _defaultBitsPerComponent = 8;
    private const int _defaultColumns = 1;

    public FlateDecodeFilter(Dictionary? filterParams)
    {
        Params = filterParams;
    }

    public Name Name => Constants.Filters.Flate;
    public Dictionary? Params { get; }

    public MemoryStream Decode(Stream data)
    {
        if (data is null) throw new FilterInputFormatException(nameof(data));

        // Skip first two bytes (Zlib header)
        if (data.ReadByte() == -1 || data.ReadByte() == -1)
            throw new FilterInputFormatException(nameof(data), "Input stream is missing required zlib header.");

        using var deflateStream = new DeflateStream(data, CompressionMode.Decompress);
        using var decompressed = new MemoryStream();
        deflateStream.CopyTo(decompressed);

        var predictor = Params?.GetAs<Number>("Predictor") ?? _defaultPredictor;
        var columns = Params?.GetAs<Number>("Columns") ?? _defaultColumns;
        var colors = Params?.GetAs<Number>("Colors") ?? _defaultColors;
        var bitsPerComponent = Params?.GetAs<Number>("BitsPerComponent") ?? _defaultBitsPerComponent;

        var decoded = PngPredictor.Decode(
            decompressed.ToArray(), // predictor operates on byte[]
            predictor,
            columns,
            colors,
            bitsPerComponent
        );

        return new MemoryStream(decoded);
    }

    public MemoryStream Encode(Stream data)
    {
        if (data is null) throw new FilterInputFormatException(nameof(data));

        using var buffer = new MemoryStream();
        data.CopyTo(buffer);
        var uncompressed = buffer.ToArray();

        var output = new MemoryStream();

        // Zlib header
        output.WriteByte(0x78); // CMF
        output.WriteByte(0xDA); // FLG

        using (var deflateStream = new DeflateStream(output, CompressionMode.Compress, true))
        {
            deflateStream.Write(uncompressed, 0, uncompressed.Length);
        }

        // Zlib footer: Adler-32 checksum
        var adler32 = Adler32(uncompressed);
        output.WriteByte((byte)(adler32 >> 24));
        output.WriteByte((byte)(adler32 >> 16));
        output.WriteByte((byte)(adler32 >> 8));
        output.WriteByte((byte)(adler32));

        output.Position = 0;
        return output;
    }

    private static uint Adler32(byte[] data)
    {
        const uint Modulus = 65521;
        uint s1 = 1, s2 = 0;
        foreach (byte b in data)
        {
            s1 = (s1 + b) % Modulus;
            s2 = (s2 + s1) % Modulus;
        }
        return (s2 << 16) + s1;
    }
}
