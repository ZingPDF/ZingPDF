namespace ZingPDF.Elements.Drawing.Text.Extraction.CmapParsing;

public class CMap
{
    public Dictionary<byte[], string> CharMap { get; } = new(ByteArrayComparer.Instance);

    public void AddMapping(byte[] src, string dst) => CharMap[src] = dst;

    public string? Map(byte[] src) =>
        CharMap.TryGetValue(src, out var result) ? result : null;
}
