using System.Buffers;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using ZingPDF.Diagnostics;
using ZingPDF.Elements.Drawing.Text.Extraction.CmapParsing;
using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Text;
using ZingPDF.Text.CompositeFonts;
using ZingPDF.Text.Encoding;

namespace ZingPDF.Elements.Drawing.Text.Extraction;

internal sealed class LowLevelTextPageExtractor : IDisposable
{
    private static readonly Encoding Ascii = Encoding.ASCII;
    private static readonly Encoding PdfDocEncoding = Encoding.GetEncoding(PDFEncoding.PDFDoc);

    private readonly ResolvedFontResourceSet _fontResources;
    private readonly LowLevelTextState _state;
    private readonly List<Operand> _operands = new(8);
    private readonly StringBuilder _textBuilder = new();

    private byte[] _byteScratch = [];
    private char[] _charScratch = [];

    public LowLevelTextPageExtractor(ResolvedFontResourceSet fontResources)
    {
        _fontResources = fontResources;
        _state = new LowLevelTextState(fontResources);
    }

    public async Task AppendTextRunsAsync(Stream stream, int pageNumber, List<TextRun> destination)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(destination);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        if (TryGetBuffer(stream, out var directBuffer))
        {
            ProcessBuffer(directBuffer.AsSpan(), pageNumber, destination, glyphDestination: null);
            return;
        }

        byte[]? rented = null;

        try
        {
            if (stream.CanSeek && stream.Length <= int.MaxValue)
            {
                var length = (int)stream.Length;
                if (length == 0)
                {
                    return;
                }

                rented = ArrayPool<byte>.Shared.Rent(length);
                await ReadExactlyAsync(stream, rented, length);
                ProcessBuffer(rented.AsSpan(0, length), pageNumber, destination, glyphDestination: null);
                return;
            }

            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            ProcessBuffer(copy.GetBuffer().AsSpan(0, (int)copy.Length), pageNumber, destination, glyphDestination: null);
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    public async Task AppendGlyphRunsAsync(Stream stream, int pageNumber, List<GlyphRun> destination)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(destination);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        if (TryGetBuffer(stream, out var directBuffer))
        {
            ProcessBuffer(directBuffer.AsSpan(), pageNumber, textDestination: null, destination);
            return;
        }

        byte[]? rented = null;

        try
        {
            if (stream.CanSeek && stream.Length <= int.MaxValue)
            {
                var length = (int)stream.Length;
                if (length == 0)
                {
                    return;
                }

                rented = ArrayPool<byte>.Shared.Rent(length);
                await ReadExactlyAsync(stream, rented, length);
                ProcessBuffer(rented.AsSpan(0, length), pageNumber, textDestination: null, destination);
                return;
            }

            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            ProcessBuffer(copy.GetBuffer().AsSpan(0, (int)copy.Length), pageNumber, textDestination: null, destination);
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    public async Task AppendPlainTextAsync(Stream stream, StringBuilder destination)
    {
        using var trace = PerformanceTrace.Measure("LowLevelTextPageExtractor.AppendPlainTextAsync");
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(destination);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        var collector = new PlainTextCollector(destination);

        if (TryGetBuffer(stream, out var directBuffer))
        {
            ProcessBuffer(directBuffer.AsSpan(), pageNumber: 0, textDestination: null, glyphDestination: null, plainTextCollector: collector);
            return;
        }

        byte[]? rented = null;

        try
        {
            if (stream.CanSeek && stream.Length <= int.MaxValue)
            {
                var length = (int)stream.Length;
                if (length == 0)
                {
                    return;
                }

                rented = ArrayPool<byte>.Shared.Rent(length);
                await ReadExactlyAsync(stream, rented, length);
                ProcessBuffer(rented.AsSpan(0, length), pageNumber: 0, textDestination: null, glyphDestination: null, plainTextCollector: collector);
                return;
            }

            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            ProcessBuffer(copy.GetBuffer().AsSpan(0, (int)copy.Length), pageNumber: 0, textDestination: null, glyphDestination: null, plainTextCollector: collector);
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    public async Task AppendUsedFontResourceNamesAsync(Stream stream, ISet<string> destination)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(destination);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        if (TryGetBuffer(stream, out var directBuffer))
        {
            CollectUsedFontResourceNames(directBuffer.AsSpan(), destination);
            return;
        }

        byte[]? rented = null;

        try
        {
            if (stream.CanSeek && stream.Length <= int.MaxValue)
            {
                var length = (int)stream.Length;
                if (length == 0)
                {
                    return;
                }

                rented = ArrayPool<byte>.Shared.Rent(length);
                await ReadExactlyAsync(stream, rented, length);
                CollectUsedFontResourceNames(rented.AsSpan(0, length), destination);
                return;
            }

            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            CollectUsedFontResourceNames(copy.GetBuffer().AsSpan(0, (int)copy.Length), destination);
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    public void Dispose()
    {
        if (_byteScratch.Length != 0)
        {
            ArrayPool<byte>.Shared.Return(_byteScratch);
            _byteScratch = [];
        }

        if (_charScratch.Length != 0)
        {
            ArrayPool<char>.Shared.Return(_charScratch);
            _charScratch = [];
        }
    }

    private void ProcessBuffer(
        ReadOnlySpan<byte> data,
        int pageNumber,
        List<TextRun>? textDestination,
        List<GlyphRun>? glyphDestination,
        PlainTextCollector? plainTextCollector = null)
    {
        using var trace = PerformanceTrace.Measure("LowLevelTextPageExtractor.ProcessBuffer");
        _operands.Clear();

        var index = 0;
        while (TryReadNextToken(data, ref index, out var token))
        {
            switch (token)
            {
                case (byte)'/':
                    _operands.Add(Operand.ForName(ReadNameBounds(data, ref index)));
                    break;

                case (byte)'(':
                    _operands.Add(Operand.ForLiteralString(ReadLiteralStringBounds(data, ref index)));
                    break;

                case (byte)'<':
                    if (PeekByte(data, index) == (byte)'<')
                    {
                        index++;
                        SkipDictionary(data, ref index);
                    }
                    else
                    {
                        _operands.Add(Operand.ForHexString(ReadHexStringBounds(data, ref index)));
                    }

                    break;

                case (byte)'[':
                    _operands.Add(Operand.ForArray(ReadArrayBounds(data, ref index)));
                    break;

                default:
                    if (IsNumberStart(token))
                    {
                        index--;
                        _operands.Add(Operand.ForNumber(ParseNumber(data, ref index)));
                    }
                    else
                    {
                        index--;
                        HandleOperator(ReadToken(data, ref index), data, ref index, pageNumber, textDestination, glyphDestination, plainTextCollector);
                        _operands.Clear();
                    }

                    break;
            }
        }
    }

    private void CollectUsedFontResourceNames(ReadOnlySpan<byte> data, ISet<string> destination)
    {
        _operands.Clear();

        var index = 0;
        while (TryReadNextToken(data, ref index, out var token))
        {
            switch (token)
            {
                case (byte)'/':
                    _operands.Add(Operand.ForName(ReadNameBounds(data, ref index)));
                    break;

                case (byte)'(':
                    _operands.Add(Operand.ForLiteralString(ReadLiteralStringBounds(data, ref index)));
                    break;

                case (byte)'<':
                    if (PeekByte(data, index) == (byte)'<')
                    {
                        index++;
                        SkipDictionary(data, ref index);
                    }
                    else
                    {
                        _operands.Add(Operand.ForHexString(ReadHexStringBounds(data, ref index)));
                    }

                    break;

                case (byte)'[':
                    _operands.Add(Operand.ForArray(ReadArrayBounds(data, ref index)));
                    break;

                default:
                    if (IsNumberStart(token))
                    {
                        index--;
                        _operands.Add(Operand.ForNumber(ParseNumber(data, ref index)));
                    }
                    else
                    {
                        index--;
                        var @operator = ReadToken(data, ref index);
                        if (@operator.Length == 2
                            && @operator[0] == (byte)'T'
                            && @operator[1] == (byte)'f'
                            && TryGetNameOperand(data, 0, out var fontName))
                        {
                            destination.Add(fontName);
                        }

                        _operands.Clear();
                    }

                    break;
            }
        }
    }

    private void HandleOperator(
        ReadOnlySpan<byte> @operator,
        ReadOnlySpan<byte> data,
        ref int index,
        int pageNumber,
        List<TextRun>? textDestination,
        List<GlyphRun>? glyphDestination,
        PlainTextCollector? plainTextCollector)
    {
        using var trace = PerformanceTrace.Measure("LowLevelTextPageExtractor.HandleOperator");
        switch (@operator.Length)
        {
            case 1:
                switch (@operator[0])
                {
                    case (byte)'\'':
                        _state.MoveToStartOfNextLine();
                        if (TryGetStringOperand(0, out var apostropheText))
                        {
                            EmitTextShow(apostropheText, data, pageNumber, textDestination, glyphDestination, plainTextCollector);
                        }

                        return;

                    case (byte)'"':
                        if (TryGetNumberOperand(0, out var quoteWordSpacing)
                            && TryGetNumberOperand(1, out var quoteCharSpacing)
                            && TryGetStringOperand(2, out var quoteText))
                        {
                            _state.SetWordSpacing((float)quoteWordSpacing);
                            _state.SetCharSpacing((float)quoteCharSpacing);
                            _state.MoveToStartOfNextLine();
                            EmitTextShow(quoteText, data, pageNumber, textDestination, glyphDestination, plainTextCollector);
                        }

                        return;
                }

                break;

            case 2:
                if (@operator[0] == (byte)'B' && @operator[1] == (byte)'T')
                {
                    _state.BeginTextObject();
                    return;
                }

                if (@operator[0] == (byte)'E' && @operator[1] == (byte)'T')
                {
                    _state.EndTextObject();
                    return;
                }

                if (@operator[0] == (byte)'T' && @operator[1] == (byte)'c')
                {
                    if (TryGetNumberOperand(0, out var charSpacing))
                    {
                        _state.SetCharSpacing((float)charSpacing);
                    }

                    return;
                }

                if (@operator[0] == (byte)'T' && @operator[1] == (byte)'w')
                {
                    if (TryGetNumberOperand(0, out var wordSpacing))
                    {
                        _state.SetWordSpacing((float)wordSpacing);
                    }

                    return;
                }

                if (@operator[0] == (byte)'T' && @operator[1] == (byte)'z')
                {
                    if (TryGetNumberOperand(0, out var horizontalScaling))
                    {
                        _state.SetHorizontalScaling((float)horizontalScaling);
                    }

                    return;
                }

                if (@operator[0] == (byte)'T' && @operator[1] == (byte)'L')
                {
                    if (TryGetNumberOperand(0, out var leading))
                    {
                        _state.SetTextLeading((float)leading);
                    }

                    return;
                }

                if (@operator[0] == (byte)'T' && @operator[1] == (byte)'f')
                {
                    if (TryGetNameOperand(data, 0, out var fontName)
                        && TryGetNumberOperand(1, out var fontSize))
                    {
                        _state.SetFont(fontName, (float)fontSize);
                    }

                    return;
                }

                if (@operator[0] == (byte)'T' && @operator[1] == (byte)'s')
                {
                    if (TryGetNumberOperand(0, out var rise))
                    {
                        _state.SetTextRise((float)rise);
                    }

                    return;
                }

                if (@operator[0] == (byte)'T' && @operator[1] == (byte)'d')
                {
                    if (TryGetNumberOperand(0, out var tx)
                        && TryGetNumberOperand(1, out var ty))
                    {
                        _state.MoveTextPosition((float)tx, (float)ty);
                    }

                    return;
                }

                if (@operator[0] == (byte)'T' && @operator[1] == (byte)'D')
                {
                    if (TryGetNumberOperand(0, out var tdx)
                        && TryGetNumberOperand(1, out var tdy))
                    {
                        _state.MoveTextPositionAndSetLeading((float)tdx, (float)tdy);
                    }

                    return;
                }

                if (@operator[0] == (byte)'T' && @operator[1] == (byte)'m')
                {
                    if (TryGetMatrixOperands(out var a, out var b, out var c, out var d, out var e, out var f))
                    {
                        _state.SetTextMatrix((float)a, (float)b, (float)c, (float)d, (float)e, (float)f);
                    }

                    return;
                }

                if (@operator[0] == (byte)'T' && @operator[1] == (byte)'*')
                {
                    _state.MoveToStartOfNextLine();
                    return;
                }

                if (@operator[0] == (byte)'T' && @operator[1] == (byte)'j')
                {
                    if (TryGetStringOperand(0, out var text))
                    {
                        EmitTextShow(text, data, pageNumber, textDestination, glyphDestination, plainTextCollector);
                    }

                    return;
                }

                if (@operator[0] == (byte)'T' && @operator[1] == (byte)'J')
                {
                    if (TryGetArrayOperand(0, out var arraySlice))
                    {
                        EmitTextArray(arraySlice, data, pageNumber, textDestination, glyphDestination, plainTextCollector);
                    }

                    return;
                }

                if (@operator[0] == (byte)'B' && @operator[1] == (byte)'I')
                {
                    SkipInlineImage(data, ref index);
                    return;
                }

                break;
        }
    }

    private void EmitTextShow(
        Operand textOperand,
        ReadOnlySpan<byte> data,
        int pageNumber,
        List<TextRun>? textDestination,
        List<GlyphRun>? glyphDestination,
        PlainTextCollector? plainTextCollector)
    {
        using var trace = PerformanceTrace.Measure("LowLevelTextPageExtractor.EmitTextShow");
        ResetTextBuilder();

        List<PositionedGlyph>? glyphs = glyphDestination != null ? [] : null;
        RunCapture capture = new(pageNumber, _state.CurrentFontName, _state.FontSize, glyphs);
        var buildText = textDestination != null || plainTextCollector != null;

        var encodedText = DecodeStringOperand(textOperand, data);
        AppendEncodedText(encodedText, ref capture, buildText);

        EmitCapturedRun(ref capture, textDestination, glyphDestination, plainTextCollector);
    }

    private void EmitTextArray(
        Operand arrayOperand,
        ReadOnlySpan<byte> data,
        int pageNumber,
        List<TextRun>? textDestination,
        List<GlyphRun>? glyphDestination,
        PlainTextCollector? plainTextCollector)
    {
        using var trace = PerformanceTrace.Measure("LowLevelTextPageExtractor.EmitTextArray");
        ResetTextBuilder();

        List<PositionedGlyph>? glyphs = glyphDestination != null ? [] : null;
        RunCapture capture = new(pageNumber, _state.CurrentFontName, _state.FontSize, glyphs);
        var buildText = textDestination != null || plainTextCollector != null;

        var array = data.Slice(arrayOperand.Start, arrayOperand.EndExclusive - arrayOperand.Start);
        var index = 0;

        while (TryReadNextToken(array, ref index, out var token))
        {
            switch (token)
            {
                case (byte)'(':
                    AppendEncodedText(DecodeLiteralString(array, ref index), ref capture, buildText);
                    break;

                case (byte)'<':
                    if (PeekByte(array, index) == (byte)'<')
                    {
                        index++;
                        SkipDictionary(array, ref index);
                    }
                    else
                    {
                        AppendEncodedText(DecodeHexString(array, ref index), ref capture, buildText);
                    }

                    break;

                case (byte)'[':
                    SkipArray(array, ref index);
                    break;

                default:
                    if (IsNumberStart(token))
                    {
                        index--;
                        _state.ApplyTextArrayAdjustment(ParseNumber(array, ref index));
                    }
                    else
                    {
                        index--;
                        _ = ReadToken(array, ref index);
                    }

                    break;
            }
        }

        EmitCapturedRun(ref capture, textDestination, glyphDestination, plainTextCollector);
    }

    private void AppendEncodedText(ReadOnlySpan<byte> encodedText, ref RunCapture capture, bool buildText)
    {
        if (encodedText.Length == 0)
        {
            return;
        }

        var currentFont = _state.CurrentFont;

        if (currentFont.ToUnicodeCMap != null)
        {
            AppendWithCMap(encodedText, currentFont, ref capture, buildText);
            return;
        }

        AppendWithEncoding(encodedText, currentFont.Encoding ?? PdfDocEncoding, ref capture, buildText);
    }

    private void AppendWithCMap(ReadOnlySpan<byte> encodedText, ResolvedFont currentFont, ref RunCapture capture, bool buildText)
    {
        using var trace = PerformanceTrace.Measure("LowLevelTextPageExtractor.AppendWithCMap");
        var cmap = currentFont.ToUnicodeCMap!;
        var offset = 0;

        while (offset < encodedText.Length)
        {
            if (cmap.TryReadMatch(encodedText.Slice(offset), out var mapped, out var consumed))
            {
                AppendMappedText(mapped!, ref capture, buildText);
                offset += consumed;
                continue;
            }

            if (currentFont.Encoding != null)
            {
                AppendWithEncoding(encodedText.Slice(offset, 1), currentFont.Encoding, ref capture, buildText);
                offset++;
                continue;
            }

            AppendMappedText("\uFFFD", ref capture, buildText);
            offset += cmap.GetFallbackCodeLength(encodedText.Length - offset);
        }
    }

    private void AppendWithEncoding(ReadOnlySpan<byte> encodedText, Encoding encoding, ref RunCapture capture, bool buildText)
    {
        using var trace = PerformanceTrace.Measure("LowLevelTextPageExtractor.AppendWithEncoding");
        var charCount = encoding.GetCharCount(encodedText);
        if (charCount == 0)
        {
            return;
        }

        EnsureCharScratch(charCount);
        var written = encoding.GetChars(encodedText, _charScratch.AsSpan(0, charCount));
        AppendMappedChars(_charScratch.AsSpan(0, written), ref capture, buildText);
    }

    private void AppendMappedText(string mapped, ref RunCapture capture, bool buildText)
    {
        if (!string.IsNullOrEmpty(mapped))
        {
            AppendMappedChars(mapped.AsSpan(), ref capture, buildText);
        }
    }

    private void AppendMappedChars(ReadOnlySpan<char> chars, ref RunCapture capture, bool buildText)
    {
        if (buildText)
        {
            _textBuilder.Append(chars);
        }

        for (var i = 0; i < chars.Length; i++)
        {
            var character = chars[i];
            var (x, y, width) = _state.Advance(character, capture.PreviousCharacter);
            if (!capture.HasGlyph)
            {
                capture.HasGlyph = true;
                capture.StartX = x;
                capture.StartY = y;
                capture.Height = _state.EffectiveFontSizeVertical;
            }

            capture.EndX = x + width;
            capture.AllWhitespace &= char.IsWhiteSpace(character);

            if (capture.Glyphs != null)
            {
                capture.Glyphs.Add(new PositionedGlyph
                {
                    Character = character,
                    X = x,
                    Y = y,
                    Width = width,
                    Height = _state.EffectiveFontSizeVertical,
                    FontName = capture.FontName,
                    FontSize = capture.FontSize,
                    PageNumber = capture.PageNumber
                });
            }

            capture.PreviousCharacter = character;
        }
    }

    private void EmitCapturedRun(ref RunCapture capture, List<TextRun>? textDestination, List<GlyphRun>? glyphDestination, PlainTextCollector? plainTextCollector)
    {
        if (!capture.HasGlyph)
        {
            return;
        }

        var textValue = string.Empty;
        if (textDestination != null || plainTextCollector != null)
        {
            textValue = _textBuilder.ToString();
        }

        if (textDestination != null)
        {
            textDestination.Add(new TextRun(
                capture.PageNumber,
                textValue,
                capture.StartX,
                capture.StartY,
                capture.EndX,
                capture.Height,
                capture.FontName,
                capture.FontSize,
                capture.AllWhitespace));
        }

        plainTextCollector?.AppendRun(textValue, capture.StartX, capture.StartY, capture.EndX, capture.Height, capture.AllWhitespace);

        if (glyphDestination != null && capture.Glyphs != null && capture.Glyphs.Count != 0)
        {
            glyphDestination.Add(new GlyphRun(capture.PageNumber, capture.Glyphs));
        }
    }

    private void ResetTextBuilder()
    {
        if (_textBuilder.Length != 0)
        {
            _textBuilder.Clear();
        }
    }

    private ReadOnlySpan<byte> DecodeStringOperand(Operand operand, ReadOnlySpan<byte> data)
    {
        var raw = data.Slice(operand.Start, operand.EndExclusive - operand.Start);
        return operand.Kind switch
        {
            OperandKind.LiteralString => DecodeLiteralString(raw),
            OperandKind.HexString => DecodeHexString(raw),
            _ => ReadOnlySpan<byte>.Empty
        };
    }

    private ReadOnlySpan<byte> DecodeLiteralString(ReadOnlySpan<byte> data, ref int index)
    {
        var rawBounds = ReadLiteralStringBounds(data, ref index);
        return DecodeLiteralString(data.Slice(rawBounds.Start, rawBounds.Length));
    }

    private ReadOnlySpan<byte> DecodeLiteralString(ReadOnlySpan<byte> raw)
    {
        using var trace = PerformanceTrace.Measure("LowLevelTextPageExtractor.DecodeLiteralString");
        EnsureByteScratch(raw.Length);
        var written = 0;

        for (var i = 0; i < raw.Length; i++)
        {
            var current = raw[i];
            if (current != (byte)'\\')
            {
                _byteScratch[written++] = current;
                continue;
            }

            if (++i >= raw.Length)
            {
                throw new EndOfStreamException("Unterminated escape sequence in literal string.");
            }

            var escaped = raw[i];
            switch (escaped)
            {
                case (byte)'\r':
                    if (i + 1 < raw.Length && raw[i + 1] == (byte)'\n')
                    {
                        i++;
                    }

                    break;

                case (byte)'\n':
                    break;

                case (byte)'n':
                    _byteScratch[written++] = 0x0A;
                    break;

                case (byte)'r':
                    _byteScratch[written++] = 0x0D;
                    break;

                case (byte)'t':
                    _byteScratch[written++] = 0x09;
                    break;

                case (byte)'b':
                    _byteScratch[written++] = 0x08;
                    break;

                case (byte)'f':
                    _byteScratch[written++] = 0x0C;
                    break;

                case (byte)'(':
                case (byte)')':
                case (byte)'\\':
                    _byteScratch[written++] = escaped;
                    break;

                case >= (byte)'0' and <= (byte)'7':
                    var octal = escaped - (byte)'0';
                    for (var octalDigits = 0; octalDigits < 2 && i + 1 < raw.Length; octalDigits++)
                    {
                        var peek = raw[i + 1];
                        if (peek is < (byte)'0' or > (byte)'7')
                        {
                            break;
                        }

                        i++;
                        octal = (octal << 3) + (peek - (byte)'0');
                    }

                    _byteScratch[written++] = (byte)(octal & 0xFF);
                    break;

                default:
                    _byteScratch[written++] = escaped;
                    break;
            }
        }

        return _byteScratch.AsSpan(0, written);
    }

    private ReadOnlySpan<byte> DecodeHexString(ReadOnlySpan<byte> data, ref int index)
    {
        var rawBounds = ReadHexStringBounds(data, ref index);
        return DecodeHexString(data.Slice(rawBounds.Start, rawBounds.Length));
    }

    private ReadOnlySpan<byte> DecodeHexString(ReadOnlySpan<byte> raw)
    {
        using var trace = PerformanceTrace.Measure("LowLevelTextPageExtractor.DecodeHexString");
        EnsureByteScratch((raw.Length + 1) / 2);

        var written = 0;
        var highNibble = -1;

        for (var i = 0; i < raw.Length; i++)
        {
            var current = raw[i];
            if (IsWhite(current))
            {
                continue;
            }

            var hex = HexValue(current);
            if (hex < 0)
            {
                throw new InvalidOperationException($"Invalid hex digit '{(char)current}' in content stream string.");
            }

            if (highNibble < 0)
            {
                highNibble = hex;
                continue;
            }

            _byteScratch[written++] = (byte)((highNibble << 4) | hex);
            highNibble = -1;
        }

        if (highNibble >= 0)
        {
            _byteScratch[written++] = (byte)(highNibble << 4);
        }

        return _byteScratch.AsSpan(0, written);
    }

    private void EnsureByteScratch(int minimumLength)
    {
        if (_byteScratch.Length >= minimumLength)
        {
            return;
        }

        if (_byteScratch.Length != 0)
        {
            ArrayPool<byte>.Shared.Return(_byteScratch);
        }

        _byteScratch = ArrayPool<byte>.Shared.Rent(Math.Max(minimumLength, 64));
    }

    private void EnsureCharScratch(int minimumLength)
    {
        if (_charScratch.Length >= minimumLength)
        {
            return;
        }

        if (_charScratch.Length != 0)
        {
            ArrayPool<char>.Shared.Return(_charScratch);
        }

        _charScratch = ArrayPool<char>.Shared.Rent(Math.Max(minimumLength, 64));
    }

    private bool TryGetNumberOperand(int operandIndex, out double value)
    {
        if ((uint)operandIndex < (uint)_operands.Count && _operands[operandIndex].Kind == OperandKind.Number)
        {
            value = _operands[operandIndex].NumberValue;
            return true;
        }

        value = default;
        return false;
    }

    private bool TryGetNameOperand(ReadOnlySpan<byte> data, int operandIndex, out string name)
    {
        if ((uint)operandIndex < (uint)_operands.Count && _operands[operandIndex].Kind == OperandKind.Name)
        {
            name = DecodeName(_operands[operandIndex], data);
            return true;
        }

        name = string.Empty;
        return false;
    }

    private bool TryGetStringOperand(int operandIndex, out Operand operand)
    {
        if ((uint)operandIndex < (uint)_operands.Count)
        {
            var candidate = _operands[operandIndex];
            if (candidate.Kind is OperandKind.LiteralString or OperandKind.HexString)
            {
                operand = candidate;
                return true;
            }
        }

        operand = default;
        return false;
    }

    private bool TryGetArrayOperand(int operandIndex, out Operand operand)
    {
        if ((uint)operandIndex < (uint)_operands.Count && _operands[operandIndex].Kind == OperandKind.Array)
        {
            operand = _operands[operandIndex];
            return true;
        }

        operand = default;
        return false;
    }

    private bool TryGetMatrixOperands(
        out double a,
        out double b,
        out double c,
        out double d,
        out double e,
        out double f)
    {
        if (TryGetNumberOperand(0, out a)
            && TryGetNumberOperand(1, out b)
            && TryGetNumberOperand(2, out c)
            && TryGetNumberOperand(3, out d)
            && TryGetNumberOperand(4, out e)
            && TryGetNumberOperand(5, out f))
        {
            return true;
        }

        a = b = c = d = e = f = default;
        return false;
    }

    private static string DecodeName(Operand operand, ReadOnlySpan<byte> data)
    {
        var raw = data.Slice(operand.Start, operand.EndExclusive - operand.Start);
        if (raw.IndexOf((byte)'#') < 0)
        {
            return Ascii.GetString(raw);
        }

        Span<byte> buffer = raw.Length <= 128 ? stackalloc byte[raw.Length] : new byte[raw.Length];
        var written = 0;

        for (var i = 0; i < raw.Length; i++)
        {
            if (raw[i] == (byte)'#'
                && i + 2 < raw.Length
                && HexValue(raw[i + 1]) >= 0
                && HexValue(raw[i + 2]) >= 0)
            {
                buffer[written++] = (byte)((HexValue(raw[i + 1]) << 4) | HexValue(raw[i + 2]));
                i += 2;
                continue;
            }

            buffer[written++] = raw[i];
        }

        return Ascii.GetString(buffer[..written]);
    }

    private static OperandSlice ReadNameBounds(ReadOnlySpan<byte> data, ref int index)
    {
        SkipIgnoredContent(data, ref index);
        var start = index;

        while (index < data.Length && !IsDelimiterOrWhitespace(data[index]))
        {
            index++;
        }

        return new OperandSlice(start, index);
    }

    private static OperandSlice ReadLiteralStringBounds(ReadOnlySpan<byte> data, ref int index)
    {
        var start = index;
        var depth = 1;

        while (index < data.Length)
        {
            var current = data[index++];
            if (current == (byte)'\\')
            {
                if (index >= data.Length)
                {
                    throw new EndOfStreamException("Unterminated escape sequence in literal string.");
                }

                var escaped = data[index++];
                if (escaped == (byte)'\r' && PeekByte(data, index) == (byte)'\n')
                {
                    index++;
                }

                continue;
            }

            if (current == (byte)'(')
            {
                depth++;
                continue;
            }

            if (current == (byte)')')
            {
                depth--;
                if (depth == 0)
                {
                    return new OperandSlice(start, index - 1);
                }
            }
        }

        throw new EndOfStreamException("Unterminated literal string.");
    }

    private static OperandSlice ReadHexStringBounds(ReadOnlySpan<byte> data, ref int index)
    {
        var start = index;

        while (index < data.Length)
        {
            if (data[index++] == (byte)'>')
            {
                return new OperandSlice(start, index - 1);
            }
        }

        throw new EndOfStreamException("Unterminated hex string.");
    }

    private static OperandSlice ReadArrayBounds(ReadOnlySpan<byte> data, ref int index)
    {
        var start = index;

        while (TryReadNextToken(data, ref index, out var token))
        {
            switch (token)
            {
                case (byte)']':
                    return new OperandSlice(start, index - 1);

                case (byte)'(':
                    _ = ReadLiteralStringBounds(data, ref index);
                    break;

                case (byte)'<':
                    if (PeekByte(data, index) == (byte)'<')
                    {
                        index++;
                        SkipDictionary(data, ref index);
                    }
                    else
                    {
                        _ = ReadHexStringBounds(data, ref index);
                    }

                    break;

                case (byte)'[':
                    SkipArray(data, ref index);
                    break;

                default:
                    if (IsNumberStart(token))
                    {
                        index--;
                        _ = ParseNumber(data, ref index);
                    }
                    else
                    {
                        index--;
                        _ = ReadToken(data, ref index);
                    }

                    break;
            }
        }

        throw new EndOfStreamException("Unterminated array in content stream.");
    }

    private static void SkipArray(ReadOnlySpan<byte> data, ref int index)
    {
        while (TryReadNextToken(data, ref index, out var token))
        {
            switch (token)
            {
                case (byte)']':
                    return;

                case (byte)'(':
                    _ = ReadLiteralStringBounds(data, ref index);
                    break;

                case (byte)'<':
                    if (PeekByte(data, index) == (byte)'<')
                    {
                        index++;
                        SkipDictionary(data, ref index);
                    }
                    else
                    {
                        _ = ReadHexStringBounds(data, ref index);
                    }

                    break;

                case (byte)'[':
                    SkipArray(data, ref index);
                    break;
            }
        }
    }

    private static void SkipDictionary(ReadOnlySpan<byte> data, ref int index)
    {
        while (TryReadNextToken(data, ref index, out var token))
        {
            switch (token)
            {
                case (byte)'(':
                    _ = ReadLiteralStringBounds(data, ref index);
                    break;

                case (byte)'<':
                    if (PeekByte(data, index) == (byte)'<')
                    {
                        index++;
                        SkipDictionary(data, ref index);
                    }
                    else
                    {
                        _ = ReadHexStringBounds(data, ref index);
                    }

                    break;

                case (byte)'>':
                    if (PeekByte(data, index) == (byte)'>')
                    {
                        index++;
                        return;
                    }

                    break;

                case (byte)'[':
                    SkipArray(data, ref index);
                    break;

                default:
                    if (!IsNumberStart(token))
                    {
                        index--;
                        _ = ReadToken(data, ref index);
                    }

                    break;
            }
        }
    }

    private static void SkipInlineImage(ReadOnlySpan<byte> data, ref int index)
    {
        while (TryReadNextToken(data, ref index, out var token))
        {
            switch (token)
            {
                case (byte)'/':
                    _ = ReadNameBounds(data, ref index);
                    continue;

                case (byte)'(':
                    _ = ReadLiteralStringBounds(data, ref index);
                    continue;

                case (byte)'<':
                    if (PeekByte(data, index) == (byte)'<')
                    {
                        index++;
                        SkipDictionary(data, ref index);
                    }
                    else
                    {
                        _ = ReadHexStringBounds(data, ref index);
                    }

                    continue;

                case (byte)'[':
                    SkipArray(data, ref index);
                    continue;

                default:
                    index--;
                    if (ReadToken(data, ref index).SequenceEqual("ID"u8))
                    {
                        break;
                    }

                    continue;
            }

            while (index < data.Length && IsWhite(data[index]))
            {
                index++;
            }

            while (index + 2 < data.Length)
            {
                if (data[index] == (byte)'E'
                    && data[index + 1] == (byte)'I'
                    && IsWhite(data[index + 2]))
                {
                    return;
                }

                index++;
            }

            return;
        }
    }

    private static bool TryReadNextToken(ReadOnlySpan<byte> data, ref int index, out byte token)
    {
        while (index < data.Length)
        {
            token = data[index++];

            if (IsWhite(token))
            {
                continue;
            }

            if (token == (byte)'%')
            {
                SkipComment(data, ref index);
                continue;
            }

            return true;
        }

        token = default;
        return false;
    }

    private static void SkipComment(ReadOnlySpan<byte> data, ref int index)
    {
        while (index < data.Length)
        {
            var current = data[index++];
            if (current is (byte)'\r' or (byte)'\n')
            {
                return;
            }
        }
    }

    private static double ParseNumber(ReadOnlySpan<byte> data, ref int index)
    {
        var token = ReadToken(data, ref index);
        if (Utf8Parser.TryParse(token, out double value, out var consumed)
            && consumed == token.Length)
        {
            return value;
        }

        return double.Parse(Ascii.GetString(token), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static ReadOnlySpan<byte> ReadToken(ReadOnlySpan<byte> data, ref int index)
    {
        SkipIgnoredContent(data, ref index);
        var start = index;

        while (index < data.Length && !IsDelimiterOrWhitespace(data[index]))
        {
            index++;
        }

        return data.Slice(start, index - start);
    }

    private static void SkipIgnoredContent(ReadOnlySpan<byte> data, ref int index)
    {
        while (index < data.Length)
        {
            while (index < data.Length && IsWhite(data[index]))
            {
                index++;
            }

            if (index >= data.Length || data[index] != (byte)'%')
            {
                return;
            }

            index++;
            SkipComment(data, ref index);
        }
    }

    private static bool TryGetBuffer(Stream stream, out ArraySegment<byte> buffer)
    {
        if (stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var segment))
        {
            buffer = new ArraySegment<byte>(segment.Array!, segment.Offset, (int)memoryStream.Length);
            return true;
        }

        buffer = default;
        return false;
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int length)
    {
        var read = 0;
        while (read < length)
        {
            var chunk = await stream.ReadAsync(buffer.AsMemory(read, length - read));
            if (chunk == 0)
            {
                throw new EndOfStreamException("Unexpected end of stream while reading a content stream.");
            }

            read += chunk;
        }
    }

    private static byte PeekByte(ReadOnlySpan<byte> data, int index)
        => index < data.Length ? data[index] : (byte)0;

    private static bool IsWhite(byte value) => value is 0x00 or 0x09 or 0x0A or 0x0C or 0x0D or 0x20;

    private static bool IsNumberStart(byte value)
        => value is (byte)'+' or (byte)'-' or (byte)'.' or >= (byte)'0' and <= (byte)'9';

    private static bool IsDelimiterOrWhitespace(byte value)
        => value is
            (byte)'(' or (byte)')' or (byte)'<' or (byte)'>' or
            (byte)'[' or (byte)']' or (byte)'{' or (byte)'}' or
            (byte)'/' or (byte)'%' or
            0x00 or 0x09 or 0x0A or 0x0C or 0x0D or 0x20;

    private static int HexValue(byte value) => value switch
    {
        >= (byte)'0' and <= (byte)'9' => value - (byte)'0',
        >= (byte)'A' and <= (byte)'F' => 10 + (value - (byte)'A'),
        >= (byte)'a' and <= (byte)'f' => 10 + (value - (byte)'a'),
        _ => -1
    };

    private enum OperandKind
    {
        Number,
        Name,
        LiteralString,
        HexString,
        Array
    }

    private readonly record struct OperandSlice(int Start, int EndExclusive)
    {
        public int Length => EndExclusive - Start;
    }

    private readonly record struct Operand(OperandKind Kind, double NumberValue, int Start, int EndExclusive)
    {
        public static Operand ForNumber(double value) => new(OperandKind.Number, value, 0, 0);
        public static Operand ForName(OperandSlice slice) => new(OperandKind.Name, 0, slice.Start, slice.EndExclusive);
        public static Operand ForLiteralString(OperandSlice slice) => new(OperandKind.LiteralString, 0, slice.Start, slice.EndExclusive);
        public static Operand ForHexString(OperandSlice slice) => new(OperandKind.HexString, 0, slice.Start, slice.EndExclusive);
        public static Operand ForArray(OperandSlice slice) => new(OperandKind.Array, 0, slice.Start, slice.EndExclusive);
    }

    private record struct RunCapture(
        int PageNumber,
        string FontName,
        float FontSize,
        List<PositionedGlyph>? Glyphs)
    {
        public bool HasGlyph { get; set; }
        public bool AllWhitespace { get; set; } = true;
        public char? PreviousCharacter { get; set; }
        public float StartX { get; set; }
        public float StartY { get; set; }
        public float EndX { get; set; }
        public float Height { get; set; }
    }

    private sealed class PlainTextCollector
    {
        private const float YTolerance = 2f;
        private const float GapFactor = 0.2f;

        private readonly StringBuilder _destination;
        private bool _hasRun;
        private bool _lastRunWasWhitespaceOnly;
        private float _lastY;
        private float _lastEndX;
        private float _lastHeight;

        public PlainTextCollector(StringBuilder destination)
        {
            _destination = destination;
        }

        public void AppendRun(string text, float startX, float startY, float endX, float height, bool allWhitespace)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!_hasRun)
            {
                _destination.Append(text);
                _hasRun = true;
                _lastRunWasWhitespaceOnly = allWhitespace;
                _lastY = startY;
                _lastEndX = endX;
                _lastHeight = height;
                return;
            }

            if (Math.Abs(_lastY - startY) >= YTolerance)
            {
                if (_destination.Length != 0 && _destination[^1] != '\n')
                {
                    _destination.Append('\n');
                }
            }
            else if (!_lastRunWasWhitespaceOnly && !allWhitespace)
            {
                var gap = startX - _lastEndX;
                var threshold = _lastHeight * GapFactor;
                if (gap > threshold && _destination.Length != 0 && _destination[^1] is not ('\n' or ' '))
                {
                    _destination.Append(' ');
                }
            }

            _destination.Append(text);
            _lastRunWasWhitespaceOnly = allWhitespace;
            _lastY = startY;
            _lastEndX = endX;
            _lastHeight = height;
        }
    }

    private sealed class LowLevelTextState
    {
        private const float MinimumScale = 0.0001f;

        private readonly ResolvedFontResourceSet _fontResources;

        private float _textE;
        private float _textF;
        private float _lineA = 1f;
        private float _lineD = 1f;
        private float _lineE;
        private float _lineF;

        public LowLevelTextState(ResolvedFontResourceSet fontResources)
        {
            _fontResources = fontResources;
        }

        public float FontSize { get; private set; } = 12f;
        public float CharSpacing { get; private set; }
        public float WordSpacing { get; private set; }
        public float HorizontalScaling { get; private set; } = 100f;
        public float TextRise { get; private set; }
        public float TextLeading { get; private set; }
        public ResolvedFont CurrentFont { get; private set; } = ResolvedFont.Fallback(string.Empty);
        public string CurrentFontName => CurrentFont.FontName;
        public float EffectiveFontSizeVertical => FontSize * MathF.Abs(MathF.Abs(_lineD) < MinimumScale ? 1f : _lineD);

        public void BeginTextObject()
        {
            _textE = 0f;
            _textF = 0f;
            _lineA = 1f;
            _lineD = 1f;
            _lineE = 0f;
            _lineF = 0f;
        }

        public void EndTextObject()
        {
        }

        public void SetCharSpacing(float charSpacing) => CharSpacing = charSpacing;
        public void SetWordSpacing(float wordSpacing) => WordSpacing = wordSpacing;
        public void SetHorizontalScaling(float horizontalScaling) => HorizontalScaling = horizontalScaling;
        public void SetTextLeading(float textLeading) => TextLeading = textLeading;
        public void SetTextRise(float textRise) => TextRise = textRise;

        public void SetFont(string fontResourceName, float fontSize)
        {
            CurrentFont = _fontResources.GetFont(fontResourceName);
            FontSize = fontSize;
            TextLeading = fontSize * 1.2f;
            _textE = _lineE;
            _textF = _lineF;
        }

        public void MoveTextPositionAndSetLeading(float tx, float ty)
        {
            TextLeading = -ty;
            MoveTextPosition(tx, ty);
        }

        public void MoveToStartOfNextLine() => MoveTextPosition(0, -TextLeading);

        public void SetTextMatrix(float a, float b, float c, float d, float e, float f)
        {
            _lineA = a;
            _lineD = d;
            _lineE = e;
            _lineF = f;
            _textE = e;
            _textF = f;
        }

        public void ApplyTextArrayAdjustment(double adjust)
        {
            _textE += (float)(-adjust / 1000d * FontSize * (HorizontalScaling / 100f));
        }

        public (float x, float y, float width) Advance(char character, char? previousCharacter)
        {
            var glyphWidth = CurrentFont.DefaultWidth;
            if (CurrentFont.Metrics != null)
            {
                if (!CurrentFont.Metrics.Widths.TryGetValue(character, out glyphWidth) || glyphWidth == 0)
                {
                    glyphWidth = CurrentFont.DefaultWidth;
                }

                if (previousCharacter.HasValue
                    && CurrentFont.Metrics.KerningPairs.TryGetValue((previousCharacter.Value, character), out var kerning))
                {
                    glyphWidth += kerning;
                }
            }

            var textAdvance = (glyphWidth / 1000f) * FontSize + CharSpacing;
            if (character == ' ')
            {
                textAdvance += WordSpacing;
            }

            var scaledAdvance = textAdvance * (HorizontalScaling / 100f);
            var x = _textE;
            var y = _textF + TextRise;

            _textE += scaledAdvance;

            var matrixScale = MathF.Abs(_lineA) < MinimumScale ? 1f : MathF.Abs(_lineA);
            return (x, y, MathF.Abs(scaledAdvance * matrixScale));
        }

        public void MoveTextPosition(float tx, float ty)
        {
            _textE = _lineE + tx;
            _textF = _lineF + ty;
            _lineE = _textE;
            _lineF = _textF;
        }
    }
}

internal sealed class ResolvedFontResourceSet
{
    private static readonly ConcurrentDictionary<string, Task<ResolvedFont>> ResolvedFontCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ResolvedFont> _fonts;

    private ResolvedFontResourceSet(Dictionary<string, ResolvedFont> fonts)
    {
        _fonts = fonts;
    }

    public static ResolvedFontResourceSet Empty { get; } = new([]);

    public static async Task<ResolvedFontResourceSet> CreateAsync(
        IPdf pdf,
        Dictionary fontResources,
        IReadOnlyList<IFontMetricsProvider> baseProviders,
        ISet<string>? selectedResourceNames = null,
        bool includeDisplayName = true,
        bool includeMetrics = true)
    {
        using var trace = PerformanceTrace.Measure("ResolvedFontResourceSet.CreateAsync");
        var fonts = new Dictionary<string, ResolvedFont>(fontResources.InnerDictionary.Count, StringComparer.Ordinal);

        foreach (var entry in fontResources)
        {
            if (selectedResourceNames != null
                && selectedResourceNames.Count != 0
                && !selectedResourceNames.Contains(entry.Key))
            {
                continue;
            }

            fonts[entry.Key] = await ResolveFontCachedAsync(pdf, entry.Key, entry.Value, baseProviders, includeDisplayName, includeMetrics);
        }

        return new ResolvedFontResourceSet(fonts);
    }

    public ResolvedFont GetFont(string resourceName)
        => _fonts.TryGetValue(resourceName, out var font)
            ? font
            : ResolvedFont.Fallback(resourceName);

    public static void ClearGlobalCache() => ResolvedFontCache.Clear();

    private static Task<ResolvedFont> ResolveFontCachedAsync(
        IPdf pdf,
        string resourceName,
        IPdfObject fontObject,
        IReadOnlyList<IFontMetricsProvider> baseProviders,
        bool includeDisplayName,
        bool includeMetrics)
    {
        var cacheKey = CreateResolvedFontCacheKey(fontObject, includeDisplayName, includeMetrics);
        return ResolvedFontCache.GetOrAdd(
            cacheKey,
            static (_, state) => ResolveFontAsync(state.pdf, state.resourceName, state.fontObject, state.baseProviders, state.includeDisplayName, state.includeMetrics),
            (pdf, resourceName, fontObject, baseProviders, includeDisplayName, includeMetrics));
    }

    private static string CreateResolvedFontCacheKey(IPdfObject fontObject, bool includeDisplayName, bool includeMetrics)
    {
        var mode = $"{(includeDisplayName ? "display" : "plain")}:{(includeMetrics ? "metrics" : "nometrics")}";
        return fontObject switch
        {
            IndirectObjectReference reference => $"ref:{reference.Id.Index}:{reference.Id.GenerationNumber}:{mode}",
            _ => $"direct:{RuntimeHelpers.GetHashCode(fontObject)}:{mode}"
        };
    }

    private static async Task<ResolvedFont> ResolveFontAsync(
        IPdf pdf,
        string resourceName,
        IPdfObject fontObject,
        IReadOnlyList<IFontMetricsProvider> baseProviders,
        bool includeDisplayName,
        bool includeMetrics)
    {
        using var trace = PerformanceTrace.Measure("ResolvedFontResourceSet.ResolveFontAsync");
        var fontDictionary = await ResolveFontDictionaryAsync(pdf, fontObject);
        if (fontDictionary is null)
        {
            return ResolvedFont.Fallback(resourceName);
        }

        var fontDescriptor = await fontDictionary.FontDescriptor.GetAsync();
        var baseFont = await fontDictionary.BaseFont.GetAsync();
        var displayName = includeDisplayName
            ? await ResolveDisplayNameAsync(resourceName, fontDescriptor, baseFont)
            : (baseFont?.Value ?? resourceName);
        var metrics = includeMetrics
            ? await ResolveMetricsAsync(fontDictionary, fontDescriptor, baseFont, baseProviders)
            : null;
        var encoding = await ResolveEncodingAsync(fontDictionary, fontDescriptor);
        var toUnicodeCMap = await ResolveToUnicodeCMapAsync(fontDictionary);
        var defaultWidth = await ResolveDefaultWidthAsync(pdf, fontDictionary, fontDescriptor, metrics);

        return new ResolvedFont(resourceName, displayName, encoding, toUnicodeCMap, metrics, defaultWidth);
    }

    private static async Task<FontDictionary?> ResolveFontDictionaryAsync(IPdf pdf, IPdfObject fontObject)
    {
        return fontObject switch
        {
            IndirectObjectReference reference => await pdf.Objects.GetAsync<FontDictionary>(reference),
            FontDictionary fontDictionary => fontDictionary,
            _ => null
        };
    }

    private static async Task<string> ResolveDisplayNameAsync(
        string resourceName,
        FontDescriptorDictionary? fontDescriptor,
        Name? baseFont)
    {
        if (fontDescriptor != null)
        {
            var fontFamily = await fontDescriptor.FontFamily.GetAsync();
            if (fontFamily != null)
            {
                return fontFamily.Decode();
            }

            var fontName = await fontDescriptor.FontName.GetAsync();
            if (fontName != null)
            {
                return fontName.Value;
            }
        }

        return baseFont?.Value ?? resourceName;
    }

    private static async Task<FontMetrics?> ResolveMetricsAsync(
        FontDictionary fontDictionary,
        FontDescriptorDictionary? fontDescriptor,
        Name? baseFont,
        IReadOnlyList<IFontMetricsProvider> baseProviders)
    {
        if (baseFont != null)
        {
            for (var i = 0; i < baseProviders.Count; i++)
            {
                var provider = baseProviders[i];
                if (provider.IsSupported(baseFont.Value))
                {
                    return provider.GetFontMetrics(baseFont.Value);
                }
            }
        }

        if (fontDescriptor == null)
        {
            return null;
        }

        var widthsArray = await fontDictionary.Widths.GetAsync();
        var firstChar = await fontDictionary.FirstChar.GetAsync();
        if (widthsArray is null || firstChar is null)
        {
            return null;
        }

        var widths = new Dictionary<char, int>(widthsArray.Count());
        var code = (int)firstChar.Value;
        foreach (var widthObject in widthsArray)
        {
            if (widthObject is Number width)
            {
                widths[(char)code] = (int)width.Value;
            }

            code++;
        }

        return await fontDescriptor.ToFontMetricsAsync(widths);
    }

    private static async Task<Encoding?> ResolveEncodingAsync(FontDictionary fontDictionary, FontDescriptorDictionary? fontDescriptor)
    {
        if (fontDictionary is Type0FontDictionary)
        {
            return null;
        }

        var encoding = await fontDictionary.Encoding.GetAsync();
        if (encoding.Value == null)
        {
            return Encoding.GetEncoding(PDFEncoding.PDFDoc);
        }

        if (encoding.Type1 != null)
        {
            return ResolveNamedEncoding(encoding.Type1.Value);
        }

        if (encoding.Type2 == null)
        {
            return Encoding.GetEncoding(PDFEncoding.PDFDoc);
        }

        var encodingDictionary = encoding.Type2;
        Encoding baseEncoding;

        var baseEncodingName = await encodingDictionary.BaseEncoding.GetAsync();
        if (baseEncodingName != null)
        {
            baseEncoding = ResolveNamedEncoding(baseEncodingName.Value);
        }
        else
        {
            var embeddedFont = fontDescriptor != null
                && (await fontDescriptor.FontFile.GetAsync() != null
                    || await fontDescriptor.FontFile2.GetAsync() != null
                    || await fontDescriptor.FontFile3.GetAsync() != null);

            if (embeddedFont)
            {
                return null;
            }

            baseEncoding = ResolveNamedEncoding(PDFEncoding.Standard);
        }

        var differences = await encodingDictionary.Differences.GetAsync();
        if (differences == null)
        {
            return baseEncoding;
        }

        var mappedDifferences = new Dictionary<byte, char>();
        byte currentCode = 0;

        foreach (var entry in differences)
        {
            switch (entry)
            {
                case Number code:
                    currentCode = (byte)code.Value;
                    break;

                case Name glyphName:
                    mappedDifferences[currentCode] = GlyphToUnicodeTranslator.Translate(glyphName.Value);
                    currentCode++;
                    break;
            }
        }

        return new DerivedEncoding(baseEncoding, mappedDifferences);
    }

    private static async Task<CMap?> ResolveToUnicodeCMapAsync(FontDictionary fontDictionary)
    {
        using var trace = PerformanceTrace.Measure("ResolvedFontResourceSet.ResolveToUnicodeCMapAsync");
        var toUnicode = await fontDictionary.ToUnicode.GetAsync();
        if (toUnicode == null)
        {
            return null;
        }

        var cmap = CMapParser.Parse(await toUnicode.GetDecompressedDataAsync());
        return cmap != null && cmap.MappingCount != 0 ? cmap : null;
    }

    private static async Task<int> ResolveDefaultWidthAsync(IPdf pdf, FontDictionary fontDictionary, FontDescriptorDictionary? fontDescriptor, FontMetrics? metrics)
    {
        if (metrics?.StandardHorizontalWidth is int standardWidth && standardWidth > 0)
        {
            return standardWidth;
        }

        if (fontDictionary is Type0FontDictionary type0Font)
        {
            var descendantFonts = type0Font.GetAs<ArrayObject>("DescendantFonts");
            if (descendantFonts != null)
            {
                foreach (var descendant in descendantFonts)
                {
                    var cidFont = descendant switch
                    {
                        IndirectObjectReference reference => await pdf.Objects.GetAsync<CIDFontDictionary>(reference),
                        CIDFontDictionary direct => direct,
                        _ => null
                    };

                    if (cidFont == null)
                    {
                        continue;
                    }

                    var defaultCidWidth = await cidFont.DW.GetAsync();
                    if (defaultCidWidth != null && defaultCidWidth.Value > 0)
                    {
                        return (int)defaultCidWidth.Value;
                    }
                }
            }

            return 1000;
        }

        if (fontDescriptor != null)
        {
            var missingWidth = await fontDescriptor.MissingWidth.GetAsync();
            if (missingWidth != null && missingWidth.Value > 0)
            {
                return (int)missingWidth.Value;
            }

            var averageWidth = await fontDescriptor.AvgWidth.GetAsync();
            if (averageWidth != null && averageWidth.Value > 0)
            {
                return (int)averageWidth.Value;
            }
        }

        return 500;
    }

    private static Encoding ResolveNamedEncoding(string name)
    {
        return name switch
        {
            PDFEncoding.Standard => Encoding.GetEncoding(PDFEncoding.Standard),
            PDFEncoding.PDFDoc => Encoding.GetEncoding(PDFEncoding.PDFDoc),
            PDFEncoding.WinAnsi => Encoding.GetEncoding(1252),
            PDFEncoding.MacRoman => Encoding.GetEncoding(10000),
            _ => throw new NotSupportedException($"Unsupported encoding name: {name}")
        };
    }
}

internal sealed record ResolvedFont(
    string ResourceName,
    string FontName,
    Encoding? Encoding,
    CMap? ToUnicodeCMap,
    FontMetrics? Metrics,
    int DefaultWidth)
{
    public static ResolvedFont Fallback(string resourceName)
        => new(resourceName, string.IsNullOrEmpty(resourceName) ? "Unknown" : resourceName, null, null, null, 500);
}
