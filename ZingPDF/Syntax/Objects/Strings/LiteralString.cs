using Microsoft.VisualBasic;
using System.Text;
using ZingPDF.Extensions;
using ZingPDF.Text.Encoding;

namespace ZingPDF.Syntax.Objects.Strings;

/// <summary>
/// ISO 32000-2:2020 7.3.4.2 - Literal strings
/// </summary>
public class LiteralString : PdfObject
{
    private LiteralString(byte[] rawBytes, ObjectContext context)
        : base(context)
    {
        ArgumentNullException.ThrowIfNull(rawBytes, nameof(rawBytes));

        RawBytes = rawBytes;
    }

    public byte[] RawBytes { get; }

    public override object Clone() => new LiteralString([.. RawBytes], Context);

    public static LiteralString FromString(string? value, ObjectContext context)
    {
        if (string.IsNullOrEmpty(value))
            return new LiteralString([], context);

        // Get your registered PDFDocEncoding with *exception* fallback so we don't get '?' silently.
        var pdfDoc = Encoding.GetEncoding(
            PDFEncoding.PDFDoc,
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);

        try
        {
            // Prefer PDFDocEncoding whenever possible.
            var bytes = pdfDoc.GetBytes(value);
            return new LiteralString(bytes, context);
        }
        catch (EncoderFallbackException)
        {
            // Fallback to UTF-16BE with BOM
            var be = Encoding.BigEndianUnicode; // no BOM automatically
            var beBytes = be.GetBytes(value);
            var withBom = new byte[beBytes.Length + 2];
            withBom[0] = 0xFE; withBom[1] = 0xFF;
            Buffer.BlockCopy(beBytes, 0, withBom, 2, beBytes.Length);
            return new LiteralString(withBom, context);
        }
    }

    public static LiteralString FromBytes(byte[] rawBytes, ObjectContext context)
        => new(rawBytes, context);

    protected override async Task WriteOutputAsync(Stream stream)
    {
        await stream.WriteCharsAsync(Constants.Characters.LeftParenthesis);

        await stream.WriteAsync(RawBytes.ToArray());

        await stream.WriteCharsAsync(Constants.Characters.RightParenthesis);
    }

    public static implicit operator LiteralString(string? value) => FromString(value, ObjectContext.FromImplicitOperator);

    // Only structural strings(not content stream text) can be decoded via implicit conversion.
    public static implicit operator string?(LiteralString? value) => value?.Decode();
}
