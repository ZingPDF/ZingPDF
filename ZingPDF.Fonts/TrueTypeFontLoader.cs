using System.Text;
using SkiaSharp;
using ZingPDF.Fonts.Extensions;

namespace ZingPDF.Fonts;

/// <summary>
/// Loads TrueType font files and exposes the metrics required for PDF registration.
/// </summary>
public static class TrueTypeFontLoader
{
    private static readonly Encoding _winAnsi = CreateWinAnsiEncoding();

    /// <summary>
    /// Loads a TrueType font from a file.
    /// </summary>
    public static async Task<TrueTypeFontFace> LoadAsync(string fontPath, string? fontName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fontPath);

        await using var stream = new FileStream(fontPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await LoadAsync(stream, fontName);
    }

    /// <summary>
    /// Loads a TrueType font from a stream.
    /// </summary>
    public static async Task<TrueTypeFontFace> LoadAsync(Stream fontData, string? fontName = null)
    {
        ArgumentNullException.ThrowIfNull(fontData);

        var bytes = await ReadAllBytesAsync(fontData);

        using var dataStream = new MemoryStream(bytes, writable: false);
        using var skData = SKData.Create(dataStream);
        using var typeface = SKTypeface.FromData(skData)
            ?? throw new InvalidOperationException("Unable to load the supplied TrueType font.");
        using var font = new SKFont(typeface, 1000);

        var widths = BuildCharacterWidths(font);
        var missingWidth = MeasureWidth(font, "?");
        var resolvedName = ResolvePdfFontName(bytes, typeface, fontName);
        var embeddingPermissions = ReadEmbeddingPermissions(bytes);

        if (!embeddingPermissions.AllowsPdfEmbedding)
        {
            throw new NotSupportedException(
                "The supplied TrueType font does not permit PDF embedding. Use a font licensed for embedding, such as Google Fonts or another embeddable font file.");
        }

        return new TrueTypeFontFace
        {
            FontName = SanitizeFontName(resolvedName),
            FamilyName = typeface.FamilyName,
            FontData = bytes,
            Metrics = font.GetFontMetrics(),
            BoundingBox = TryReadBoundingBox(bytes) ?? font.GetBoundingBox(),
            EmbeddingPermissions = embeddingPermissions,
            WidthsByCharacterCode = widths,
            MissingWidth = missingWidth,
            AverageWidth = widths.Count == 0 ? missingWidth : (int)Math.Round(widths.Values.Average()),
            MaxWidth = widths.Count == 0 ? missingWidth : widths.Values.Max()
        };
    }

    private static Dictionary<byte, int> BuildCharacterWidths(SKFont font)
    {
        var widths = new Dictionary<byte, int>(224);
        var missingWidth = MeasureWidth(font, "?");

        for (var code = 32; code <= 255; code++)
        {
            widths[(byte)code] = TryDecodeWinAnsi((byte)code, out var ch)
                ? MeasureWidth(font, ch.ToString())
                : missingWidth;
        }

        return widths;
    }

    private static int MeasureWidth(SKFont font, string text)
        => (int)Math.Round(font.MeasureText(text));

    private static bool TryDecodeWinAnsi(byte value, out char character)
    {
        try
        {
            character = _winAnsi.GetString([value])[0];
            return true;
        }
        catch (DecoderFallbackException)
        {
            character = default;
            return false;
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        return memory.ToArray();
    }

    private static Encoding CreateWinAnsiEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(
            1252,
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
    }

    private static string ResolvePdfFontName(byte[] fontData, SKTypeface typeface, string? requestedName)
    {
        var postScriptName = TryGetFontName(fontData, 6);
        if (!string.IsNullOrWhiteSpace(postScriptName))
        {
            return postScriptName;
        }

        if (!string.IsNullOrWhiteSpace(requestedName))
        {
            return requestedName;
        }

        return TryGetFontName(fontData, 4)
            ?? TryGetFontName(fontData, 1)
            ?? typeface.FamilyName;
    }

    private static string? TryGetFontName(byte[] fontData, ushort nameId)
    {
        if (fontData.Length < 12)
        {
            return null;
        }

        var tableCount = ReadUInt16BigEndian(fontData, 4);
        const int tableRecordSize = 16;
        const int tableDirectoryOffset = 12;

        for (var i = 0; i < tableCount; i++)
        {
            var recordOffset = tableDirectoryOffset + (i * tableRecordSize);
            if (recordOffset + tableRecordSize > fontData.Length)
            {
                return null;
            }

            if (ReadAscii(fontData, recordOffset, 4) != "name")
            {
                continue;
            }

            var tableOffset = (int)ReadUInt32BigEndian(fontData, recordOffset + 8);
            var tableLength = (int)ReadUInt32BigEndian(fontData, recordOffset + 12);
            if (tableOffset < 0 || tableLength <= 0 || tableOffset + tableLength > fontData.Length || tableOffset + 6 > fontData.Length)
            {
                return null;
            }

            var count = ReadUInt16BigEndian(fontData, tableOffset + 2);
            var stringOffset = ReadUInt16BigEndian(fontData, tableOffset + 4);
            var recordsOffset = tableOffset + 6;
            var storageOffset = tableOffset + stringOffset;

            var records = new List<(int Priority, string Value)>();

            for (var recordIndex = 0; recordIndex < count; recordIndex++)
            {
                var nameRecordOffset = recordsOffset + (recordIndex * 12);
                if (nameRecordOffset + 12 > fontData.Length)
                {
                    break;
                }

                var platformId = ReadUInt16BigEndian(fontData, nameRecordOffset);
                var encodingId = ReadUInt16BigEndian(fontData, nameRecordOffset + 2);
                var currentNameId = ReadUInt16BigEndian(fontData, nameRecordOffset + 6);
                var length = ReadUInt16BigEndian(fontData, nameRecordOffset + 8);
                var offset = ReadUInt16BigEndian(fontData, nameRecordOffset + 10);

                if (currentNameId != nameId)
                {
                    continue;
                }

                var valueOffset = storageOffset + offset;
                if (valueOffset < 0 || valueOffset + length > fontData.Length)
                {
                    continue;
                }

                var value = DecodeNameRecord(fontData, valueOffset, length, platformId, encodingId);
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                records.Add((GetNameRecordPriority(platformId, encodingId), value));
            }

            return records
                .OrderBy(x => x.Priority)
                .Select(x => x.Value.Trim())
                .FirstOrDefault();
        }

        return null;
    }

    private static FontBoundingBox? TryReadBoundingBox(byte[] fontData)
    {
        if (!TryGetTable(fontData, "head", out var tableOffset, out _))
        {
            return null;
        }

        if (tableOffset + 54 > fontData.Length)
        {
            return null;
        }

        var unitsPerEm = ReadUInt16BigEndian(fontData, tableOffset + 18);
        if (unitsPerEm == 0)
        {
            return null;
        }

        var xMin = ReadInt16BigEndian(fontData, tableOffset + 36);
        var yMin = ReadInt16BigEndian(fontData, tableOffset + 38);
        var xMax = ReadInt16BigEndian(fontData, tableOffset + 40);
        var yMax = ReadInt16BigEndian(fontData, tableOffset + 42);

        return new FontBoundingBox(
            ScaleFontUnit(xMin, unitsPerEm),
            ScaleFontUnit(yMin, unitsPerEm),
            ScaleFontUnit(xMax, unitsPerEm),
            ScaleFontUnit(yMax, unitsPerEm));
    }

    private static TrueTypeEmbeddingPermissions ReadEmbeddingPermissions(byte[] fontData)
    {
        if (!TryGetTable(fontData, "OS/2", out var tableOffset, out _))
        {
            return new TrueTypeEmbeddingPermissions(
                0,
                IsInstallable: true,
                AllowsPreviewAndPrint: false,
                AllowsEditableEmbedding: false,
                RequiresFullFontEmbedding: false,
                BitmapEmbeddingOnly: false);
        }

        if (tableOffset + 10 > fontData.Length)
        {
            throw new InvalidOperationException("The OS/2 table is truncated.");
        }

        var fsType = ReadUInt16BigEndian(fontData, tableOffset + 8);
        var isInstallable = fsType == 0;
        var allowsPreviewAndPrint = (fsType & 0x0004) != 0;
        var allowsEditableEmbedding = (fsType & 0x0008) != 0;
        var requiresFullFontEmbedding = (fsType & 0x0100) != 0;
        var bitmapEmbeddingOnly = (fsType & 0x0200) != 0;

        return new TrueTypeEmbeddingPermissions(
            fsType,
            isInstallable,
            allowsPreviewAndPrint,
            allowsEditableEmbedding,
            requiresFullFontEmbedding,
            bitmapEmbeddingOnly);
    }

    private static int GetNameRecordPriority(ushort platformId, ushort encodingId)
        => (platformId, encodingId) switch
        {
            (3, 1) => 0,
            (3, 10) => 1,
            (0, _) => 2,
            (1, 0) => 3,
            _ => 10
        };

    private static string? DecodeNameRecord(byte[] fontData, int offset, int length, ushort platformId, ushort encodingId)
    {
        var bytes = fontData.AsSpan(offset, length).ToArray();

        return platformId switch
        {
            0 => Encoding.BigEndianUnicode.GetString(bytes),
            3 when encodingId is 0 or 1 or 10 => Encoding.BigEndianUnicode.GetString(bytes),
            1 => Encoding.ASCII.GetString(bytes),
            _ => null
        };
    }

    private static bool TryGetTable(byte[] fontData, string tag, out int tableOffset, out int tableLength)
    {
        tableOffset = 0;
        tableLength = 0;

        if (fontData.Length < 12)
        {
            return false;
        }

        var tableCount = ReadUInt16BigEndian(fontData, 4);
        const int tableRecordSize = 16;
        const int tableDirectoryOffset = 12;

        for (var i = 0; i < tableCount; i++)
        {
            var recordOffset = tableDirectoryOffset + (i * tableRecordSize);
            if (recordOffset + tableRecordSize > fontData.Length)
            {
                return false;
            }

            if (ReadAscii(fontData, recordOffset, 4) != tag)
            {
                continue;
            }

            tableOffset = (int)ReadUInt32BigEndian(fontData, recordOffset + 8);
            tableLength = (int)ReadUInt32BigEndian(fontData, recordOffset + 12);
            return tableOffset >= 0 && tableLength > 0 && tableOffset + tableLength <= fontData.Length;
        }

        return false;
    }

    private static int ScaleFontUnit(short value, ushort unitsPerEm)
        => (int)Math.Round(value * (1000d / unitsPerEm));

    private static ushort ReadUInt16BigEndian(byte[] data, int offset)
        => (ushort)((data[offset] << 8) | data[offset + 1]);

    private static uint ReadUInt32BigEndian(byte[] data, int offset)
        => (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);

    private static short ReadInt16BigEndian(byte[] data, int offset)
        => unchecked((short)ReadUInt16BigEndian(data, offset));

    private static string ReadAscii(byte[] data, int offset, int length)
        => Encoding.ASCII.GetString(data, offset, length);

    private static string SanitizeFontName(string fontName)
        => string.Concat(fontName.Where(ch => !char.IsWhiteSpace(ch)));
}
