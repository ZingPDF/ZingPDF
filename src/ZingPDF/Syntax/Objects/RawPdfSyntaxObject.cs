using System.Text;

namespace ZingPDF.Syntax.Objects;

internal sealed class RawPdfSyntaxObject(string asciiText, ObjectContext context) : PdfObject(context)
{
    private readonly byte[] _bytes = Encoding.ASCII.GetBytes(asciiText ?? throw new ArgumentNullException(nameof(asciiText)));

    protected override Task WriteOutputAsync(Stream stream) => stream.WriteAsync(_bytes).AsTask();

    public override object Clone() => new RawPdfSyntaxObject(Encoding.ASCII.GetString(_bytes), Context);
}
