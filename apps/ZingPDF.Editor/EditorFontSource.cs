using System.Security.Cryptography;
using System.Text;
using ZingPDF.Fonts;
using ZingPDF.GoogleFonts;

namespace ZingPDF.Editor;

public interface IEditorFontSource
{
    Task<byte[]> GetFontAsync(string family, string variant, CancellationToken cancellationToken);
}

/// <summary>Downloads and caches TrueType fonts. Document text is never sent to Google.</summary>
public sealed class GoogleEditorFontSource : IEditorFontSource
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(40) };
    private readonly GoogleFontsClient _client = new(Http);
    public static string[] Families { get; } = ["Noto Sans", "Noto Serif", "Roboto", "Open Sans", "Lato", "Inter", "Montserrat", "Merriweather", "Source Sans 3", "Libre Baskerville"];
    public async Task<byte[]> GetFontAsync(string family, string variant, CancellationToken cancellationToken)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("script-coverage-v1|" + family + "|" + variant)));
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZingPDF Editor", "Fonts");
        var path = Path.Combine(folder, key + ".ttf");
        if (File.Exists(path))
        {
            var info = new FileInfo(path);
            if (info.Length <= 32 * 1024 * 1024)
            {
                var cached = await File.ReadAllBytesAsync(path, cancellationToken);
                try { using var input = new MemoryStream(cached); await TrueTypeFontLoader.LoadAsync(input); return cached; }
                catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or ArgumentException) { File.Delete(path); }
            }
        }
        await using var font = await _client.DownloadFontAsync(new() { Family = family, Variant = variant }, cancellationToken);
        var bytes = font.ToArray();
        await TrueTypeFontLoader.LoadAsync(font);
        try
        {
            Directory.CreateDirectory(folder);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { await File.WriteAllBytesAsync(temporary, bytes, cancellationToken); File.Move(temporary, path, overwrite: true); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* Download remains usable without a writable cache. */ }
        return bytes;
    }
}
