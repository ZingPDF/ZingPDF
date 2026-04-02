namespace ZingPDF.Elements.Drawing.Text.Extraction.CmapParsing;

public class CMap
{
    private readonly HashSet<int> _codeLengths = [];
    private readonly Node _root = new();
    private int _smallestCodeLength = int.MaxValue;

    public int MappingCount { get; private set; }

    public void AddMapping(byte[] src, string dst)
    {
        RegisterCodeLength(src.Length);
        AddMappingCore(src, dst);
    }

    public void AddMapping(ulong srcValue, int srcLength, string dst)
    {
        RegisterCodeLength(srcLength);
        AddMappingCore(srcValue, srcLength, dst);
    }

    public string? Map(byte[] src)
    {
        var node = _root;
        foreach (var b in src)
        {
            node = node.GetChild(b);
            if (node == null)
            {
                return null;
            }
        }

        return node.MappedValue;
    }

    internal void RegisterCodeLength(int length)
    {
        if (length > 0)
        {
            _codeLengths.Add(length);
            if (length < _smallestCodeLength)
            {
                _smallestCodeLength = length;
            }
        }
    }

    internal bool TryReadMatch(ReadOnlySpan<byte> source, out string? mapped, out int bytesConsumed)
    {
        var node = _root;
        mapped = null;
        bytesConsumed = 0;

        for (var i = 0; i < source.Length; i++)
        {
            node = node.GetChild(source[i]);
            if (node == null)
            {
                break;
            }

            if (node.MappedValue != null)
            {
                mapped = node.MappedValue;
                bytesConsumed = i + 1;
            }
        }

        return mapped != null;
    }

    internal int GetFallbackCodeLength(int remainingBytes)
    {
        if (remainingBytes <= 0)
        {
            return 0;
        }

        if (_smallestCodeLength == int.MaxValue)
        {
            return 1;
        }

        return remainingBytes >= _smallestCodeLength ? _smallestCodeLength : 1;
    }

    private void AddMappingCore(ReadOnlySpan<byte> src, string dst)
    {
        var node = _root;
        foreach (var b in src)
        {
            node = node.GetOrAddChild(b);
        }

        if (node.MappedValue == null)
        {
            MappingCount++;
        }

        node.MappedValue = dst;
    }

    private void AddMappingCore(ulong srcValue, int srcLength, string dst)
    {
        var node = _root;
        for (var i = srcLength - 1; i >= 0; i--)
        {
            var shift = i * 8;
            node = node.GetOrAddChild((byte)(srcValue >> shift));
        }

        if (node.MappedValue == null)
        {
            MappingCount++;
        }

        node.MappedValue = dst;
    }

    private sealed class Node
    {
        private Node[]? _children;

        public string? MappedValue { get; set; }

        public Node? GetChild(byte value) => _children?[value];

        public Node GetOrAddChild(byte value)
        {
            _children ??= new Node[256];
            return _children[value] ??= new Node();
        }
    }
}
