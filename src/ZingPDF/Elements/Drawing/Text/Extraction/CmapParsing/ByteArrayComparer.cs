namespace ZingPDF.Elements.Drawing.Text.Extraction.CmapParsing;

public class ByteArrayComparer : IEqualityComparer<byte[]>
{
    public static readonly ByteArrayComparer Instance = new();

    public bool Equals(byte[]? x, byte[]? y) =>
        x!.SequenceEqual(y!);

    public int GetHashCode(byte[] obj) =>
        HashCode.Combine(obj.Length, obj.Take(4).Aggregate(0, (a, b) => a * 31 + b));
}
