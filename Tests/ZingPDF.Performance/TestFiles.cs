using System.Collections.Concurrent;

namespace ZingPDF.Performance;

internal static class TestFiles
{
    private const string PdfBasePath = "TestFiles/pdf";
    private static readonly ConcurrentDictionary<string, byte[]> Cache = [];

    public const string Ghostscript = $"{PdfBasePath}/Ghostscript.pdf";
    public const string Minimal = $"{PdfBasePath}/minimal.pdf";
    public const string Minimal2 = $"{PdfBasePath}/minimal2.pdf";
    public const string Portfolio = $"{PdfBasePath}/MikeyFlemingFreelance_Folio.pdf";

    public static MemoryStream OpenStream(string path) => new(ReadAllBytes(path), writable: false);

    private static byte[] ReadAllBytes(string relativePath)
    {
        if (Cache.TryGetValue(relativePath, out var cached))
        {
            return cached;
        }

        var absolutePath = Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var bytes = File.ReadAllBytes(absolutePath);
        Cache.TryAdd(relativePath, bytes);
        return bytes;
    }
}
