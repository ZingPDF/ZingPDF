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
        var resolvedName = string.IsNullOrWhiteSpace(fontName) ? typeface.FamilyName : fontName;

        return new TrueTypeFontFace
        {
            FontName = SanitizeFontName(resolvedName),
            FamilyName = typeface.FamilyName,
            FontData = bytes,
            Metrics = font.GetFontMetrics(),
            BoundingBox = font.GetBoundingBox(),
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

    private static string SanitizeFontName(string fontName)
        => string.Concat(fontName.Where(ch => !char.IsWhiteSpace(ch)));
}
