using System.Buffers.Binary;
using System.Text;
using SkiaSharp;
using ZingPDF.Fonts;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text.CompositeFonts;

namespace ZingPDF.Text;

internal sealed class UnicodeTrueTypeFont : IDisposable
{
    private readonly SKData _data;
    private readonly SKTypeface _typeface;
    private readonly SKFont _font;
    private readonly Dictionary<Rune, ushort> _cids = [];
    private readonly Dictionary<ushort, ushort> _glyphs = [];
    private readonly Dictionary<ushort, double> _widths = [];
    public TrueTypeFontFace Face { get; }
    public string MissingCharacters { get; }
    public byte[] EncodedText { get; }
    public double Width => _cids.Count == 0 ? 0 : _encodedRunes.Sum(r => _widths[_cids[r]]);
    private readonly Rune[] _encodedRunes;

    private UnicodeTrueTypeFont(TrueTypeFontFace face, string text)
    {
        Face = face;
        _data = SKData.CreateCopy(face.FontData);
        var typeface = SKTypeface.FromData(_data);
        if (typeface is null) { _data.Dispose(); throw new NotSupportedException("This font could not be read."); }
        _typeface = typeface;
        _font = new SKFont(_typeface, 1000);
        var missing = new StringBuilder();
        _encodedRunes = text.EnumerateRunes().ToArray();
        using var encoded = new MemoryStream();
        foreach (var rune in _encodedRunes)
        {
            if (!_cids.TryGetValue(rune, out var cid))
            {
                var glyph = _font.GetGlyph(rune.Value);
                if (glyph == 0)
                {
                    if (!missing.ToString().Contains(rune.ToString(), StringComparison.Ordinal)) missing.Append(rune);
                }
                cid = checked((ushort)(_cids.Count + 1));
                _cids.Add(rune, cid); _glyphs[cid] = glyph;
                _widths[cid] = Math.Round(_font.GetGlyphWidths(new ushort[] { glyph })[0], 3);
            }
            encoded.WriteByte((byte)(cid >> 8)); encoded.WriteByte((byte)cid);
        }
        EncodedText = encoded.ToArray(); MissingCharacters = missing.ToString();
    }

    public static async Task<UnicodeTrueTypeFont> CreateAsync(ReadOnlyMemory<byte> bytes, string text, string? name)
    {
        ValidateProgram(bytes);
        using var stream = new MemoryStream(bytes.ToArray(), writable: false);
        return FromFace(await TrueTypeFontLoader.LoadAsync(stream, name), text);
    }

    internal static UnicodeTrueTypeFont FromFace(TrueTypeFontFace face, string text)
    {
        ValidateProgram(face.FontData);
        ValidateText(text);
        return new(face, text);
    }

    private static void ValidateProgram(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length < 12 || bytes.Length > 32 * 1024 * 1024)
            throw new NotSupportedException("Choose a TrueType font file of up to 32 MB.");
        var signature = BinaryPrimitives.ReadUInt32BigEndian(bytes.Span);
        if (signature is not (0x00010000 or 0x74727565))
            throw new NotSupportedException("This font is not a TrueType outline font. Choose another font.");
    }

    public async Task<IndirectObjectReference> EmbedAsync(Pdf pdf, IPdfObject? existingDescriptor = null)
    {
        if (MissingCharacters.Length != 0) throw new NotSupportedException($"The selected font does not contain: {MissingCharacters}");
        // Authoring and replacement share the same program, CID widths and Unicode mappings.
        // Reuse an existing descriptor when extending an embedded program's mappings.
        var descriptor = existingDescriptor ?? await pdf.EmbedTrueTypeProgramAsync(Face);
        var widths = new ArrayObject();
        var descendant = new CIDFontDictionary(new Dictionary<string, IPdfObject>(), pdf, ObjectContext.UserCreated);
        descendant.Set("Type", (Name)"Font"); descendant.Set("Subtype", (Name)"CIDFontType2");
        descendant.Set("BaseFont", (Name)Face.FontName); descendant.Set("FontDescriptor", descriptor);
        descendant.Set("CIDSystemInfo", new CIDSystemInfoDictionary(new Dictionary<string, IPdfObject>(), pdf, ObjectContext.UserCreated));
        var system = await descendant.CIDSystemInfo.GetAsync();
        system.Set("Registry", PdfString.FromTextAuto("Adobe", ObjectContext.UserCreated)); system.Set("Ordering", PdfString.FromTextAuto("Identity", ObjectContext.UserCreated)); system.Set("Supplement", (Number)0);
        descendant.Set("W", widths); descendant.Set("DW", (Number)1000);
        var mapData = new MemoryStream();
        var unicodeData = new MemoryStream();
        var mapObject = await AddStreamAsync(pdf, mapData);
        var unicodeObject = await AddStreamAsync(pdf, unicodeData);
        descendant.Set("CIDToGIDMap", mapObject.Reference);
        var cidObject = await pdf.Objects.AddAsync(descendant);
        var font = new Type0FontDictionary(pdf, ObjectContext.UserCreated);
        font.Set("BaseFont", (Name)Face.FontName); font.Set("Encoding", (Name)"Identity-H");
        font.Set("DescendantFonts", new ArrayObject([cidObject.Reference], ObjectContext.UserCreated));
        font.Set("ToUnicode", unicodeObject.Reference);
        var result = await pdf.Objects.AddAsync(font);
        _refreshMappings = () =>
        {
            widths.Clear();
            var map = new byte[(_cids.Count + 1) * 2];
            foreach (var (cid, glyph) in _glyphs)
            {
                BinaryPrimitives.WriteUInt16BigEndian(map.AsSpan(cid * 2, 2), glyph);
                widths.Add((Number)cid); widths.Add(new ArrayObject([(Number)_widths[cid]], ObjectContext.UserCreated));
            }
            var cmap = new StringBuilder("/CIDInit /ProcSet findresource begin\n12 dict begin\nbegincmap\n/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n/CMapName /ZingUnicode def\n/CMapType 2 def\n1 begincodespacerange\n<0000> <FFFF>\nendcodespacerange\n");
            foreach (var chunk in _cids.Chunk(100))
            {
                cmap.Append(chunk.Length).Append(" beginbfchar\n");
                foreach (var entry in chunk)
                    cmap.Append('<').Append(entry.Value.ToString("X4")).Append("> <").Append(Convert.ToHexString(System.Text.Encoding.BigEndianUnicode.GetBytes(entry.Key.ToString()))).Append(">\n");
                cmap.Append("endbfchar\n");
            }
            cmap.Append("endcmap\nCMapName currentdict /CMap defineresource pop\nend\nend\n");
            Write(mapData, map); Write(unicodeData, System.Text.Encoding.ASCII.GetBytes(cmap.ToString()));
            pdf.Objects.Update(cidObject); pdf.Objects.Update(mapObject); pdf.Objects.Update(unicodeObject);
        };
        _refreshMappings();
        return result.Reference;
    }

    private Action? _refreshMappings;
    public string GetMissingCharacters(string text)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateText(text);
        return string.Concat(text.EnumerateRunes().Where(r => r.Value is not (10 or 13) && _font.GetGlyph(r.Value) == 0).Distinct());
    }
    public double Measure(string text) => text.EnumerateRunes().Sum(r => (double)_font.GetGlyphWidths(new ushort[] { _font.GetGlyph(r.Value) })[0]);
    public byte[] Encode(string text)
    {
        var missing = GetMissingCharacters(text);
        if (missing.Length != 0) throw new ArgumentException($"The font '{Face.FontName}' does not contain: {missing}", nameof(text));
        var runes = text.EnumerateRunes().ToArray();
        if (_cids.Count + runes.Distinct().Count(r => !_cids.ContainsKey(r)) > ushort.MaxValue)
            throw new NotSupportedException("This font resource exceeds 65,535 distinct Unicode characters. Register another font resource.");
        var changed = false;
        using var bytes = new MemoryStream();
        foreach (var rune in runes)
        {
            if (!_cids.TryGetValue(rune, out var cid))
            {
                cid = (ushort)(_cids.Count + 1); _cids.Add(rune, cid);
                var glyph = _font.GetGlyph(rune.Value); _glyphs[cid] = glyph;
                _widths[cid] = Math.Round(_font.GetGlyphWidths(new ushort[] { glyph })[0], 3); changed = true;
            }
            bytes.WriteByte((byte)(cid >> 8)); bytes.WriteByte((byte)cid);
        }
        if (changed) _refreshMappings?.Invoke();
        return bytes.ToArray();
    }
    private static void ValidateText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        for (var index = 0; index < text.Length;)
        {
            if (!Rune.TryGetRuneAt(text, index, out var rune)) throw new ArgumentException("The text contains an incomplete Unicode character.", nameof(text));
            index += rune.Utf16SequenceLength;
        }
    }
    private static void Write(MemoryStream stream, byte[] bytes)
    { stream.SetLength(0); stream.Write(bytes); stream.Position = 0; }
    private static async Task<IndirectObject> AddStreamAsync(IPdf pdf, MemoryStream bytes)
    {
        var dictionary = new StreamDictionary(pdf, ObjectContext.UserCreated);
        return await pdf.Objects.AddAsync(new StreamObject<StreamDictionary>(bytes, dictionary));
    }

    private bool _disposed;
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _font.Dispose(); _typeface.Dispose(); _data.Dispose();
    }
}
