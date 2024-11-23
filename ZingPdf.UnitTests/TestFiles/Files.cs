using System.Collections.Concurrent;

namespace ZingPDF.UnitTests.TestFiles;

public static class Files
{
    private static readonly ConcurrentDictionary<string, byte[]> _files = [];

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
