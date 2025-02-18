namespace ZingPDF.Syntax;

public class Either<T1, T2> : PdfObject where T1 : class, IPdfObject where T2 : class, IPdfObject
{
    public Either(T1 value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Value = value;
    }

    public Either(T2 value)
    {
        ArgumentNullException.ThrowIfNull(value);
        
        Value = value;
    }

    public IPdfObject Value { get; }

    protected override Task WriteOutputAsync(Stream stream) => Value.WriteAsync(stream);
}
