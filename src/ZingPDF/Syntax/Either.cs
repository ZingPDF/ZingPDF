namespace ZingPDF.Syntax;

public class Either<T1, T2> : PdfObject
    where T1 : class?, IPdfObject?
    where T2 : class?, IPdfObject?
{
    public Either(T1 value, ObjectContext context)
        : base(context)
    {
        Value = value;
    }

    public Either(T2 value, ObjectContext context)
        : base(context)
    {
        Value = value;
    }

    public IPdfObject? Value { get; }

    public T1? Type1 => Value as T1;
    public T2? Type2 => Value as T2;

    protected override Task WriteOutputAsync(Stream stream) => Value?.WriteAsync(stream) ?? throw new InvalidOperationException("Attempt to write null property");

    public override object Clone()
    {
        return Value switch
        {
            T1 t1 => new Either<T1, T2>((T1)t1.Clone(), Context),
            T2 t2 => new Either<T1, T2>((T2)t2.Clone(), Context),
            _ => throw new InvalidOperationException("Attempt to clone an invalid Either object")
        };
    }
}
