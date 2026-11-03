using System.Buffers.Binary;
using System.Text;
using System.Security.Cryptography;
using SkiaSharp;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Text.CompositeFonts;
using PdfDictionary = ZingPDF.Syntax.Objects.Dictionaries.Dictionary;

namespace ZingPDF.Text.Editing;

internal sealed class EditingFont
{
    public required FontDictionary Dictionary { get; init; }
    public required ResolvedFont Resolved { get; init; }
    public required string ResourceName { get; init; }
    public required string Name { get; init; }
    public bool Composite { get; init; }
    public bool SupportedEncoding { get; private set; }
    public bool IsEmbedded => Program.Length > 0;
    public bool IsSubset => Name.Length > 7 && Name[6] == '+' && Name.Take(6).All(c => c is >= 'A' and <= 'Z');
    public byte[] Program { get; private set; } = [];
    public IPdfObject? DescriptorReference { get; private set; }
    public string Signature { get; private set; } = "";
    public double Ascent { get; private set; } = 800;
    public double Descent { get; private set; } = -200;
    private readonly Dictionary<int, double> _widths = [];
    private readonly Dictionary<string, byte[]> _codes = new(StringComparer.Ordinal);
    private double _defaultWidth = 500;

    public static async Task<EditingFont> LoadAsync(IPdf pdf, PdfDictionary fonts, string resourceName)
    {
        var dictionary = await fonts.GetRequiredProperty<FontDictionary>(resourceName).GetAsync();
        var resources = await ResolvedFontResourceSet.CreateAsync(pdf, fonts, [new PDFStandardFontMetricsProvider()], new HashSet<string> { resourceName });
        var resolved = resources.GetFont(resourceName);
        var encoding = await dictionary.Encoding.GetAsync();
        var composite = dictionary is Type0FontDictionary;
        // The standard Type 1 fonts without /Encoding use StandardEncoding, not PDFDocEncoding.
        // Embedded fonts without an explicit encoding may have a private built-in encoding.
        var explicitEncoding = encoding.Value is not null;
        if (!composite && !explicitEncoding && resolved.Metrics is not null)
            resolved = resolved with { Encoding = System.Text.Encoding.GetEncoding(Text.Encoding.PDFEncoding.Standard) };
        var model = new EditingFont
        {
            Dictionary = dictionary, Resolved = resolved, ResourceName = resourceName,
            Name = (await dictionary.BaseFont.GetAsync())?.Value ?? resolved.FontName,
            Composite = composite,
            SupportedEncoding = dictionary is not ZingPDF.Text.SimpleFonts.Type3FontDictionary && resolved.FontName is not ("Symbol" or "ZapfDingbats") &&
                (!composite || encoding.Type1?.Value == "Identity-H")
        };
        FontDictionary metricsDictionary = dictionary;
        if (composite && await dictionary.GetRequiredProperty<ArrayObject>("DescendantFonts").GetAsync() is { } descendants && descendants.FirstOrDefault() is { } descendant)
        {
            metricsDictionary = descendant is Syntax.Objects.IndirectObjects.IndirectObjectReference reference
                ? await pdf.Objects.GetAsync<CIDFontDictionary>(reference)
                : (FontDictionary)descendant;
            model._defaultWidth = (await metricsDictionary.GetOptionalProperty<Number>("DW").GetAsync())?.Value ?? 1000;
            if (await metricsDictionary.GetOptionalProperty<ArrayObject>("W").GetAsync() is { } widths)
            {
                var entries = widths.ToArray();
                for (var i = 0; i < entries.Length;)
                {
                    var first = (int)((Number)entries[i++]).Value;
                    if (entries[i++] is ArrayObject sequence)
                    {
                        foreach (var width in sequence.Cast<Number>()) model._widths[first++] = width.Value;
                    }
                    else
                    {
                        var last = (int)((Number)entries[i - 1]).Value;
                        var width = ((Number)entries[i++]).Value;
                        if (last - first > 65535) throw new NotSupportedException("The CID font contains an invalid width range.");
                        for (var code = first; code <= last; code++) model._widths[code] = width;
                    }
                }
            }
        }
        else
        {
            model._defaultWidth = resolved.DefaultWidth;
            if (await dictionary.Widths.GetAsync() is { } widths)
            {
                var first = (int)((await dictionary.FirstChar.GetAsync())?.Value ?? 0);
                foreach (var width in widths.Cast<Number>()) model._widths[first++] = width.Value;
            }
        }
        var descriptor = await metricsDictionary.FontDescriptor.GetAsync();
        if (descriptor is not null)
        {
            model.DescriptorReference = await metricsDictionary.FontDescriptor.GetRawValueAsync();
            model.Ascent = (await descriptor.Ascent.GetAsync())?.Value ?? 800;
            model.Descent = (await descriptor.Descent.GetAsync())?.Value ?? -200;
            var program = await descriptor.FontFile2.GetAsync()
                ?? await descriptor.FontFile.GetAsync()
                ?? await descriptor.FontFile3.GetAsync();
            if (program is not null)
            {
                using var data = await program.GetDecompressedDataAsync();
                using var bytes = new MemoryStream(); await data.CopyToAsync(bytes); model.Program = bytes.ToArray();
            }
        }
        if (!composite && !explicitEncoding && (model.IsEmbedded || resolved.Metrics is null) && resolved.ToUnicodeCMap is null)
            model.SupportedEncoding = false;
        if (resolved.ToUnicodeCMap is { } cmap)
        {
            foreach (var (code, text) in cmap.EnumerateMappings())
                if (!string.IsNullOrEmpty(text) && !text.Contains('\uFFFD')) model._codes.TryAdd(text, code);
        }
        else if (!composite && resolved.Encoding is { } singleByte)
        {
            using var data = model.Program.Length > 0 ? SKData.CreateCopy(model.Program) : null;
            using var typeface = data is not null ? SKTypeface.FromData(data) : null;
            using var font = typeface is not null ? new SKFont(typeface, 1000) : null;
            for (var code = 0; code <= 255; code++)
            {
                var text = singleByte.GetString([(byte)code]);
                if (text.Length != 1 || char.IsControl(text[0]) || text[0] == '\uFFFD') continue;
                // Unknown subset encodings must never claim coverage just because a byte exists.
                if (model.IsEmbedded && (font is null || !font.ContainsGlyphs(text))) continue;
                model._codes.TryAdd(text, [(byte)code]);
            }
        }
        using var signature = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        signature.AppendData(model.Program);
        foreach (var (code, width) in model._widths.OrderBy(p => p.Key)) signature.AppendData(System.Text.Encoding.UTF8.GetBytes($"{code}:{width.ToString(System.Globalization.CultureInfo.InvariantCulture)};"));
        foreach (var mapping in model._codes.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            signature.AppendData(System.Text.Encoding.UTF8.GetBytes(mapping.Key)); signature.AppendData(mapping.Value);
        }
        model.Signature = Convert.ToHexString(signature.GetHashAndReset());
        return model;
    }

    public IEnumerable<(byte[] Code, string Text, double Width)> Decode(byte[] bytes)
    {
        for (var offset = 0; offset < bytes.Length;)
        {
            string text;
            int count;
            if (Resolved.ToUnicodeCMap?.TryReadMatch(bytes.AsSpan(offset), out var mapped, out var consumed) == true)
            { text = mapped!; count = consumed; }
            else if (Composite)
            { count = Math.Min(2, bytes.Length - offset); text = "\uFFFD"; }
            else
            { count = 1; text = Resolved.Encoding?.GetString(bytes, offset, 1) ?? "\uFFFD"; }
            var code = bytes.AsSpan(offset, count).ToArray();
            yield return (code, text, Width(code, text));
            offset += count;
        }
    }

    public double Width(byte[] code, string text)
    {
        var value = CodeValue(code);
        if (_widths.TryGetValue(value, out var width)) return width;
        if (!Composite && Resolved.Metrics is { } metrics && text.Length == 1 && metrics.Widths.TryGetValue(text[0], out var measured)) return measured;
        return _defaultWidth;
    }

    public bool TryEncode(string text, out byte[] bytes, out string missing)
    {
        using var output = new MemoryStream();
        var missingText = new StringBuilder();
        var sequences = _codes.Keys.OrderByDescending(t => t.Length).ToArray();
        for (var offset = 0; offset < text.Length;)
        {
            var match = sequences.FirstOrDefault(s => text.AsSpan(offset).StartsWith(s, StringComparison.Ordinal));
            if (match is not null) { output.Write(_codes[match]); offset += match.Length; }
            else
            {
                var rune = Rune.GetRuneAt(text, offset);
                if (!missingText.ToString().Contains(rune.ToString(), StringComparison.Ordinal)) missingText.Append(rune);
                offset += rune.Utf16SequenceLength;
            }
        }
        bytes = output.ToArray(); missing = missingText.ToString();
        return missing.Length == 0;
    }

    private static int CodeValue(byte[] code) => code.Length == 2 ? BinaryPrimitives.ReadUInt16BigEndian(code) : code.Length == 1 ? code[0] : -1;
}
