using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Text;
using ZingPDF.Extensions;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Drawing.Text.Extraction;

internal sealed class TextContentStreamWalker : ITextContentStreamWalker
{
    private static readonly Encoding Ascii = Encoding.ASCII;

    public async Task<List<TextRun>> ExtractTextRunsAsync(Stream stream, TextDrawingState state, int pageNumber)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        if (TryGetBuffer(stream, out var directBuffer))
        {
            return ExtractTextRuns(directBuffer.AsSpan(), state, pageNumber);
        }

        byte[]? rented = null;

        try
        {
            if (stream.CanSeek && stream.Length <= int.MaxValue)
            {
                var length = (int)stream.Length;
                if (length == 0)
                {
                    return [];
                }

                rented = ArrayPool<byte>.Shared.Rent(length);
                await ReadExactlyAsync(stream, rented, length);
                return ExtractTextRuns(rented.AsSpan(0, length), state, pageNumber);
            }

            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            return ExtractTextRuns(copy.GetBuffer().AsSpan(0, (int)copy.Length), state, pageNumber);
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static List<TextRun> ExtractTextRuns(ReadOnlySpan<byte> data, TextDrawingState state, int pageNumber)
    {
        var runs = new List<TextRun>();
        var operands = new List<TextOperand>(8);
        var index = 0;

        while (TryReadNextToken(data, ref index, out var token))
        {
            switch (token)
            {
                case (byte)'/':
                    operands.Add(TextOperand.ForName(ParseName(data, ref index)));
                    break;

                case (byte)'(':
                    operands.Add(TextOperand.ForString(ParseLiteralBytes(data, ref index)));
                    break;

                case (byte)'<':
                    if (PeekByte(data, index) == (byte)'<')
                    {
                        index++;
                        SkipDictionary(data, ref index);
                    }
                    else
                    {
                        operands.Add(TextOperand.ForString(ParseHexBytes(data, ref index)));
                    }

                    break;

                case (byte)'[':
                    operands.Add(TextOperand.ForArraySlice(ParseArrayBounds(data, ref index)));
                    break;

                default:
                    if (IsNumberStart(token))
                    {
                        index--;
                        operands.Add(TextOperand.ForNumber(ParseNumber(data, ref index)));
                    }
                    else
                    {
                        index--;
                        HandleOperator(ReadToken(data, ref index), operands, state, pageNumber, runs, data, ref index);
                        operands.Clear();
                    }

                    break;
            }
        }

        return runs;
    }

    private static void HandleOperator(
        ReadOnlySpan<byte> @operator,
        List<TextOperand> operands,
        TextDrawingState state,
        int pageNumber,
        List<TextRun> runs,
        ReadOnlySpan<byte> data,
        ref int index)
    {
        if (@operator.SequenceEqual("BT"u8))
        {
            state.BeginTextObject();
            return;
        }

        if (@operator.SequenceEqual("ET"u8))
        {
            state.EndTextObject();
            return;
        }

        if (@operator.SequenceEqual("Tc"u8))
        {
            if (TryGetNumber(operands, 0, out var charSpacing))
            {
                state.SetCharSpacing((float)charSpacing);
            }

            return;
        }

        if (@operator.SequenceEqual("Tw"u8))
        {
            if (TryGetNumber(operands, 0, out var wordSpacing))
            {
                state.SetWordSpacing((float)wordSpacing);
            }

            return;
        }

        if (@operator.SequenceEqual("Tz"u8))
        {
            if (TryGetNumber(operands, 0, out var horizontalScaling))
            {
                state.SetHorizontalScaling((float)horizontalScaling);
            }

            return;
        }

        if (@operator.SequenceEqual("TL"u8))
        {
            if (TryGetNumber(operands, 0, out var leading))
            {
                state.SetTextLeading((float)leading);
            }

            return;
        }

        if (@operator.SequenceEqual("Tf"u8))
        {
            if (TryGetName(operands, 0, out var fontResourceName)
                && TryGetNumber(operands, 1, out var fontSize))
            {
                state.SetFont(fontResourceName, (float)fontSize);
            }

            return;
        }

        if (@operator.SequenceEqual("Ts"u8))
        {
            if (TryGetNumber(operands, 0, out var textRise))
            {
                state.SetTextRise((float)textRise);
            }

            return;
        }

        if (@operator.SequenceEqual("Td"u8))
        {
            if (TryGetNumber(operands, 0, out var tx)
                && TryGetNumber(operands, 1, out var ty))
            {
                state.MoveTextPosition((float)tx, (float)ty);
            }

            return;
        }

        if (@operator.SequenceEqual("TD"u8))
        {
            if (TryGetNumber(operands, 0, out var tx)
                && TryGetNumber(operands, 1, out var ty))
            {
                state.MoveTextPositionAndSetLeading((float)tx, (float)ty);
            }

            return;
        }

        if (@operator.SequenceEqual("Tm"u8))
        {
            if (TryGetNumbers(operands, out var matrix, 6))
            {
                state.SetTextMatrix(
                    (float)matrix[0],
                    (float)matrix[1],
                    (float)matrix[2],
                    (float)matrix[3],
                    (float)matrix[4],
                    (float)matrix[5]);
            }

            return;
        }

        if (@operator.SequenceEqual("T*"u8))
        {
            state.MoveToStartOfNextLine();
            return;
        }

        if (@operator.SequenceEqual("Tj"u8))
        {
            if (TryGetString(operands, 0, out var textBytes))
            {
                AddIfNotNull(runs, state.ShowText(textBytes, pageNumber));
            }

            return;
        }

        if (@operator.SequenceEqual("TJ"u8))
        {
            if (TryGetArraySlice(operands, 0, out var start, out var end))
            {
                AddIfNotNull(runs, HandleTextArray(data.Slice(start, end - start), state, pageNumber));
            }

            return;
        }

        if (@operator.SequenceEqual("'"u8))
        {
            if (TryGetString(operands, 0, out var textBytes))
            {
                state.MoveToStartOfNextLine();
                AddIfNotNull(runs, state.ShowText(textBytes, pageNumber));
            }

            return;
        }

        if (@operator.SequenceEqual("\""u8))
        {
            if (TryGetNumber(operands, 0, out var wordSpacing)
                && TryGetNumber(operands, 1, out var charSpacing)
                && TryGetString(operands, 2, out var textBytes))
            {
                state.SetWordSpacing((float)wordSpacing);
                state.SetCharSpacing((float)charSpacing);
                state.MoveToStartOfNextLine();
                AddIfNotNull(runs, state.ShowText(textBytes, pageNumber));
            }

            return;
        }

        if (@operator.SequenceEqual("BI"u8))
        {
            SkipInlineImage(data, ref index);
        }
    }

    private static void AddIfNotNull(List<TextRun> runs, TextRun? run)
    {
        if (run != null)
        {
            runs.Add(run.Value);
        }
    }

    private static TextArraySlice ParseArrayBounds(ReadOnlySpan<byte> data, ref int index)
    {
        var start = index;

        while (TryReadNextToken(data, ref index, out var token))
        {
            switch (token)
            {
                case (byte)']':
                    return new TextArraySlice(start, index - 1);

                case (byte)'(':
                    SkipLiteralString(data, ref index);
                    break;

                case (byte)'<':
                    if (PeekByte(data, index) == (byte)'<')
                    {
                        index++;
                        SkipDictionary(data, ref index);
                    }
                    else
                    {
                        SkipHexString(data, ref index);
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

    private static TextRun? HandleTextArray(ReadOnlySpan<byte> data, TextDrawingState state, int pageNumber)
    {
        var builder = new StringBuilder();
        char? prev = null;
        var hasGlyph = false;
        var allWhitespace = true;
        var startX = 0f;
        var startY = 0f;
        var endX = 0f;
        var height = state.EffectiveFontSizeVertical;
        var index = 0;
        var fontName = state.CurrentFontName;

        while (TryReadNextToken(data, ref index, out var token))
        {
            switch (token)
            {
                case (byte)'(':
                    AppendDecodedText(ParseLiteralBytes(data, ref index), state, builder, ref prev, ref hasGlyph, ref allWhitespace, ref startX, ref startY, ref endX, ref height);
                    break;

                case (byte)'<':
                    if (PeekByte(data, index) == (byte)'<')
                    {
                        index++;
                        SkipDictionary(data, ref index);
                    }
                    else
                    {
                        AppendDecodedText(ParseHexBytes(data, ref index), state, builder, ref prev, ref hasGlyph, ref allWhitespace, ref startX, ref startY, ref endX, ref height);
                    }

                    break;

                case (byte)'[':
                    SkipArray(data, ref index);
                    break;

                default:
                    if (IsNumberStart(token))
                    {
                        index--;
                        state.ApplyTextArrayAdjustment(ParseNumber(data, ref index));
                    }
                    else
                    {
                        index--;
                        _ = ReadToken(data, ref index);
                    }

                    break;
            }
        }

        return hasGlyph
            ? new TextRun(pageNumber, builder.ToString(), startX, startY, endX, height, fontName, state.FontSize, allWhitespace)
            : null;
    }

    private static void AppendDecodedText(
        byte[] textBytes,
        TextDrawingState state,
        StringBuilder builder,
        ref char? prev,
        ref bool hasGlyph,
        ref bool allWhitespace,
        ref float startX,
        ref float startY,
        ref float endX,
        ref float height)
    {
        var unicode = state.MapCharacterCode(textBytes);
        if (unicode.Length == 0)
        {
            return;
        }

        builder.Append(unicode);

        foreach (var ch in unicode)
        {
            var (x, y, adv) = state.CalculateNextCharPosition(ch, prev);
            if (!hasGlyph)
            {
                startX = x;
                startY = y;
                height = state.EffectiveFontSizeVertical;
                hasGlyph = true;
            }

            endX = x + adv;
            allWhitespace &= char.IsWhiteSpace(ch);
            prev = ch;
        }
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
                    SkipLiteralString(data, ref index);
                    break;
                case (byte)'<':
                    if (PeekByte(data, index) == (byte)'<')
                    {
                        index++;
                        SkipDictionary(data, ref index);
                    }
                    else
                    {
                        SkipHexString(data, ref index);
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
                    SkipLiteralString(data, ref index);
                    break;

                case (byte)'<':
                    if (PeekByte(data, index) == (byte)'<')
                    {
                        index++;
                        SkipDictionary(data, ref index);
                    }
                    else
                    {
                        SkipHexString(data, ref index);
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
                    _ = ParseName(data, ref index);
                    continue;

                case (byte)'(':
                    SkipLiteralString(data, ref index);
                    continue;

                case (byte)'<':
                    if (PeekByte(data, index) == (byte)'<')
                    {
                        index++;
                        SkipDictionary(data, ref index);
                    }
                    else
                    {
                        SkipHexString(data, ref index);
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

            for (; index + 2 < data.Length; index++)
            {
                if (data[index] == (byte)'E'
                    && data[index + 1] == (byte)'I'
                    && IsWhite(data[index + 2]))
                {
                    return;
                }
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

        token = 0;
        return false;
    }

    private static void SkipComment(ReadOnlySpan<byte> data, ref int index)
    {
        while (index < data.Length)
        {
            var current = data[index++];
            if (current is (byte)'\n' or (byte)'\r')
            {
                return;
            }
        }
    }

    private static string ParseName(ReadOnlySpan<byte> data, ref int index)
        => Ascii.GetString(ReadToken(data, ref index)).ReplaceHexCodes();

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

    private static byte[] ParseLiteralBytes(ReadOnlySpan<byte> data, ref int index)
    {
        var buffer = new List<byte>(64);
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
                switch (escaped)
                {
                    case (byte)'\r':
                        if (PeekByte(data, index) == (byte)'\n')
                        {
                            index++;
                        }

                        continue;
                    case (byte)'\n':
                        continue;
                    case (byte)'n':
                        buffer.Add(0x0A);
                        continue;
                    case (byte)'r':
                        buffer.Add(0x0D);
                        continue;
                    case (byte)'t':
                        buffer.Add(0x09);
                        continue;
                    case (byte)'b':
                        buffer.Add(0x08);
                        continue;
                    case (byte)'f':
                        buffer.Add(0x0C);
                        continue;
                    case (byte)'(':
                    case (byte)')':
                    case (byte)'\\':
                        buffer.Add(escaped);
                        continue;
                    case >= (byte)'0' and <= (byte)'7':
                        var octal = escaped - (byte)'0';
                        for (var i = 0; i < 2; i++)
                        {
                            var peek = PeekByte(data, index);
                            if (peek is >= (byte)'0' and <= (byte)'7')
                            {
                                index++;
                                octal = (octal << 3) + (peek - (byte)'0');
                            }
                            else
                            {
                                break;
                            }
                        }

                        buffer.Add((byte)(octal & 0xFF));
                        continue;
                    default:
                        buffer.Add(escaped);
                        continue;
                }
            }

            if (current == (byte)'(')
            {
                depth++;
                buffer.Add(current);
                continue;
            }

            if (current == (byte)')')
            {
                depth--;
                if (depth == 0)
                {
                    return [.. buffer];
                }

                buffer.Add(current);
                continue;
            }

            buffer.Add(current);
        }

        throw new EndOfStreamException("Unterminated literal string.");
    }

    private static byte[] ParseHexBytes(ReadOnlySpan<byte> data, ref int index)
    {
        var nibbles = new List<int>(64);

        while (index < data.Length)
        {
            var current = data[index++];
            if (IsWhite(current))
            {
                continue;
            }

            if (current == (byte)'>')
            {
                break;
            }

            var hex = HexValue(current);
            if (hex < 0)
            {
                throw new InvalidOperationException($"Invalid hex digit '{(char)current}' in content stream string.");
            }

            nibbles.Add(hex);
        }

        if ((nibbles.Count & 1) == 1)
        {
            nibbles.Add(0);
        }

        var bytes = new byte[nibbles.Count / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)((nibbles[i * 2] << 4) | nibbles[(i * 2) + 1]);
        }

        return bytes;
    }

    private static void SkipLiteralString(ReadOnlySpan<byte> data, ref int index)
    {
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
                    return;
                }
            }
        }

        throw new EndOfStreamException("Unterminated literal string.");
    }

    private static void SkipHexString(ReadOnlySpan<byte> data, ref int index)
    {
        while (index < data.Length)
        {
            if (data[index++] == (byte)'>')
            {
                return;
            }
        }

        throw new EndOfStreamException("Unterminated hex string.");
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

    private static bool TryGetName(List<TextOperand> operands, int index, out string value)
    {
        if (index < operands.Count && operands[index].Kind == TextOperandKind.Name)
        {
            value = operands[index].NameValue!;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetNumber(List<TextOperand> operands, int index, out double value)
    {
        if (index < operands.Count && operands[index].Kind == TextOperandKind.Number)
        {
            value = operands[index].NumberValue;
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryGetString(List<TextOperand> operands, int index, out byte[] value)
    {
        if (index < operands.Count && operands[index].Kind == TextOperandKind.String)
        {
            value = operands[index].StringBytes!;
            return true;
        }

        value = [];
        return false;
    }

    private static bool TryGetArraySlice(List<TextOperand> operands, int index, out int start, out int end)
    {
        if (index < operands.Count && operands[index].Kind == TextOperandKind.ArraySlice)
        {
            start = operands[index].ArrayStart;
            end = operands[index].ArrayEnd;
            return true;
        }

        start = 0;
        end = 0;
        return false;
    }

    private static bool TryGetNumbers(List<TextOperand> operands, out double[] values, int count)
    {
        if (operands.Count < count)
        {
            values = [];
            return false;
        }

        values = new double[count];
        for (var i = 0; i < count; i++)
        {
            if (!TryGetNumber(operands, i, out values[i]))
            {
                values = [];
                return false;
            }
        }

        return true;
    }

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

    private enum TextOperandKind
    {
        Number,
        Name,
        String,
        ArraySlice
    }

    private readonly record struct TextOperand(
        TextOperandKind Kind,
        double NumberValue,
        string? NameValue,
        byte[]? StringBytes,
        int ArrayStart,
        int ArrayEnd)
    {
        public static TextOperand ForNumber(double value) => new(TextOperandKind.Number, value, null, null, 0, 0);
        public static TextOperand ForName(string value) => new(TextOperandKind.Name, 0, value, null, 0, 0);
        public static TextOperand ForString(byte[] value) => new(TextOperandKind.String, 0, null, value, 0, 0);
        public static TextOperand ForArraySlice(TextArraySlice slice) => new(TextOperandKind.ArraySlice, 0, null, null, slice.Start, slice.End);
    }

    private readonly record struct TextArraySlice(int Start, int End);
}
