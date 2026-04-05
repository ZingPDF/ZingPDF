using System.Buffers;
using System.Text;

namespace ZingPDF.Syntax.Objects.Strings;

public enum PdfStringSyntax { Literal, Hex }
public enum PdfStringKind { Text, Ascii, Byte }                 // semantic intent
public enum PdfTextEncoding { PdfDoc, Utf16BE_BOM, Utf8_BOM }    // for Kind == Text

/// <summary>
/// String object with syntax + semantic intent separated.
/// Syntax per ISO 32000-2 §7.3.4 (literal vs hex).
/// Semantics per §7.9.2 (text / ASCII / byte) and text encodings (PDF 2.0).
/// </summary>
public sealed class PdfString : PdfObject
{
    private static readonly Encoding _pdfDoc =
        Encoding.GetEncoding("PDFDocEncoding",
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);

    public PdfStringSyntax Syntax { get; }
    public PdfStringKind Kind { get; }
    public PdfTextEncoding? TextEncoding { get; } // only when Kind == Text
    public byte[] Bytes { get; }

    // -------- Construction --------

    private PdfString(byte[] bytes, PdfStringSyntax syntax, PdfStringKind kind, PdfTextEncoding? enc, ObjectContext ctx)
        : base(ctx)
    {
        Bytes = bytes ?? [];
        Syntax = syntax;
        Kind = kind;
        TextEncoding = kind == PdfStringKind.Text ? enc : null;
    }

    // Creation for known semantics (deterministic)
    public static PdfString FromText(string text, PdfTextEncoding enc, PdfStringSyntax syntax, ObjectContext ctx)
        => new(EncodeText(text ?? string.Empty, enc), syntax, PdfStringKind.Text, enc, ctx);

    public static PdfString FromTextAuto(string text, ObjectContext ctx, bool preferUtf8ForPdf20 = false,
                                         PdfStringSyntax syntax = PdfStringSyntax.Literal)
        => AllCharsInPdfDoc(text)
            ? FromText(text, PdfTextEncoding.PdfDoc, syntax, ctx)
            : FromText(text, preferUtf8ForPdf20 ? PdfTextEncoding.Utf8_BOM : PdfTextEncoding.Utf16BE_BOM, syntax, ctx);

    public static PdfString FromAscii(string ascii, PdfStringSyntax syntax, ObjectContext ctx)
    {
        ascii ??= string.Empty;

        if (ascii.Any(ch => ch > 0x7F))
            throw new ArgumentException("ASCII string contains non-ASCII characters.", nameof(ascii));

        return new(Encoding.ASCII.GetBytes(ascii), syntax, PdfStringKind.Ascii, null, ctx);
    }

    public static PdfString FromBytes(byte[] bytes, PdfStringSyntax syntax, ObjectContext ctx)
        => new(bytes ?? [], syntax, PdfStringKind.Byte, null, ctx);

    // Promote after parse when the surrounding context is known
    public PdfString AsText(bool pdf20AllowUtf8 = true)
    {
        if (Kind == PdfStringKind.Text)
        {
            return this;
        }

        if (Kind == PdfStringKind.Ascii)
        {
            return new PdfString(Bytes, Syntax, PdfStringKind.Text, PdfTextEncoding.PdfDoc, Context);
        }

        // Byte → Text: detect BOM else PdfDoc
        if (HasUtf16Bom(Bytes))
        {
            return new PdfString(Bytes.AsSpan(2).ToArray(), Syntax, PdfStringKind.Text, PdfTextEncoding.Utf16BE_BOM, Context);
        }

        if (pdf20AllowUtf8 && HasUtf8Bom(Bytes))
        {
            return new PdfString(Bytes.AsSpan(3).ToArray(), Syntax, PdfStringKind.Text, PdfTextEncoding.Utf8_BOM, Context);
        }

        return new PdfString(Bytes, Syntax, PdfStringKind.Text, PdfTextEncoding.PdfDoc, Context);
    }

    public PdfString AsAscii()
    {
        if (Kind == PdfStringKind.Ascii)
        {
            return this;
        }

        if (Bytes.Any(b => b > 0x7F))
        {
            throw new InvalidOperationException("Cannot mark as ASCII: contains non-ASCII bytes.");
        }

        return new PdfString(Bytes, Syntax, PdfStringKind.Ascii, null, Context);
    }

    public PdfString AsBytes()
        => Kind == PdfStringKind.Byte ? this : new PdfString(Bytes, Syntax, PdfStringKind.Byte, null, Context);

    // -------- Decoding (for Kind = Text / Ascii) --------

    public string DecodeText()
    {
        return Kind switch
        {
            PdfStringKind.Text => TextEncoding switch
            {
                PdfTextEncoding.PdfDoc => _pdfDoc.GetString(Bytes),
                PdfTextEncoding.Utf16BE_BOM => Encoding.BigEndianUnicode.GetString(Bytes),
                PdfTextEncoding.Utf8_BOM => Encoding.UTF8.GetString(Bytes),
                _ => throw new InvalidOperationException("Unknown text encoding.")
            },
            PdfStringKind.Ascii => Encoding.ASCII.GetString(Bytes),
            _ => throw new InvalidOperationException("Byte string requires font/CMap to decode.")
        };
    }

    // -------- Writing --------
    protected override Task WriteOutputAsync(Stream stream)
    {
        if (Syntax == PdfStringSyntax.Hex)
        {
            stream.WriteByte((byte)'<');
            WriteHex(stream, Bytes);
            stream.WriteByte((byte)'>');

            return Task.CompletedTask;
        }

        // Literal syntax: escape specials and use octal for control bytes
        stream.WriteByte((byte)'(');
        WriteLiteralEscaped(stream, Bytes);
        stream.WriteByte((byte)')');

        return Task.CompletedTask;
    }

    // -------- Helpers --------

    private static byte[] EncodeText(string s, PdfTextEncoding enc) => enc switch
    {
        PdfTextEncoding.PdfDoc => _pdfDoc.GetBytes(s),
        PdfTextEncoding.Utf16BE_BOM => new byte[] { 0xFE, 0xFF }.Concat(Encoding.BigEndianUnicode.GetBytes(s)).ToArray(),
        PdfTextEncoding.Utf8_BOM => new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(s)).ToArray(),
        _ => throw new ArgumentOutOfRangeException(nameof(enc))
    };

    private static bool AllCharsInPdfDoc(string s)
    {
        // Quick accept: U+0000..U+00FF then validate mapping (PdfDoc != Latin-1 for a few points)
        if (s.Any(ch => ch > 0xFF)) return false;
        try { _ = _pdfDoc.GetBytes(s); return true; }
        catch (EncoderFallbackException) { return false; }
    }

    private static bool HasUtf16Bom(byte[] b) => b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF;
    private static bool HasUtf8Bom(byte[] b) => b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF;

    private static void WriteHex(Stream s, byte[] bytes)
    {
        // Uppercase hex, no spaces. If caller wants spacing, they can control it.
        const string hex = "0123456789ABCDEF";
        foreach (var b in bytes)
        {
            s.WriteByte((byte)hex[b >> 4]);
            s.WriteByte((byte)hex[b & 0x0F]);
        }
    }

    private static void WriteLiteralEscaped(Stream s, byte[] bytes)
    {
        foreach (var b in bytes)
        {
            switch (b)
            {
                case (byte)'(':
                    s.WriteByte((byte)'\\'); s.WriteByte((byte)'('); break;
                case (byte)')':
                    s.WriteByte((byte)'\\'); s.WriteByte((byte)')'); break;
                case (byte)'\\':
                    s.WriteByte((byte)'\\'); s.WriteByte((byte)'\\'); break;

                // Standard escapes
                case 0x08: s.WriteByte((byte)'\\'); s.WriteByte((byte)'b'); break; // backspace
                case 0x09: s.WriteByte((byte)'\\'); s.WriteByte((byte)'t'); break; // tab
                case 0x0A: s.WriteByte((byte)'\\'); s.WriteByte((byte)'n'); break; // LF
                case 0x0C: s.WriteByte((byte)'\\'); s.WriteByte((byte)'f'); break; // FF
                case 0x0D: s.WriteByte((byte)'\\'); s.WriteByte((byte)'r'); break; // CR

                default:
                    if (b < 0x20)
                    {
                        // Use octal for other control codes, 3 digits
                        s.WriteByte((byte)'\\');
                        s.WriteByte((byte)('0' + ((b >> 6) & 0x07)));
                        s.WriteByte((byte)('0' + ((b >> 3) & 0x07)));
                        s.WriteByte((byte)('0' + (b & 0x07)));
                    }
                    else
                    {
                        s.WriteByte(b);
                    }
                    break;
            }
        }
    }

    public override object Clone() => new PdfString([.. Bytes], Syntax, Kind, TextEncoding, Context);
}
