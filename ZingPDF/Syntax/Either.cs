namespace ZingPDF.Syntax;

public class Either<T1, T2> : PdfObject
    where T1 : class?, IPdfObject?
    where T2 : class?, IPdfObject?
{
    public Either(T1 value)
    {
        Value = value!;
    }

    public Either(T2 value)
    {
        Value = value!;
    }

    public IPdfObject? Value { get; }

    public T1? Type1 => Value as T1;
    public T2? Type2 => Value as T2;

    protected override Task WriteOutputAsync(Stream stream) => Value?.WriteAsync(stream) ?? throw new InvalidOperationException("Attempt to write null property");
}
