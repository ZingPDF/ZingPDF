using System.Collections.Concurrent;

namespace ZingPDF.UnitTests.TestFiles;

public static class Files
{
    private const string _htmlBasePath = "testfiles/html";
    private const string _imageBasePath = "testfiles/image";
    private const string _pdfBasePath = "testfiles/pdf";

    private static readonly ConcurrentDictionary<string, byte[]> _files = [];

    public static string Minimal1 => $"{_pdfBasePath}/minimal.pdf";
    public static string Form => $"{_pdfBasePath}/form.pdf";
    public static string MikeyPortfolio => $"{_pdfBasePath}/MikeyFlemingFreelance_Folio.pdf";

    public static MemoryStream AsStream(string path) => new(ConcurrentRead(path));

    public static byte[] ConcurrentRead(string filePath)
    {
        if (_files.TryGetValue(filePath, out var result))
        {
            return result;
        }

        var file = File.ReadAllBytes(filePath);

        _files.TryAdd(filePath, file);

        return file;
    }
}
