using System.Collections.Concurrent;
using Xunit;

namespace ZingPDF.UnitTests.TestFiles;

public static class Files
{
    private const string _htmlBasePath = "testfiles/html";
    private const string _imageBasePath = "testfiles/image";
    private const string _pdfBasePath = "testfiles/pdf";

    private static readonly ConcurrentDictionary<string, byte[]> _files = [];

    public const string ComboboxForm = $"{_pdfBasePath}/combobox-form.pdf";
    public const string ComplexForm = $"{_pdfBasePath}/complex-form.pdf";
    public const string Form = $"{_pdfBasePath}/form.pdf";
    public const string Ghostscript = $"{_pdfBasePath}/Ghostscript.pdf";
    public const string MikeyPortfolio = $"{_pdfBasePath}/MikeyFlemingFreelance_Folio.pdf";
    public const string Minimal1 = $"{_pdfBasePath}/minimal.pdf";
    public const string Minimal2 = $"{_pdfBasePath}/minimal2.pdf";
    public const string Minimal3 = $"{_pdfBasePath}/minimal3.pdf";
    public const string Test = $"{_pdfBasePath}/test.pdf";

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
