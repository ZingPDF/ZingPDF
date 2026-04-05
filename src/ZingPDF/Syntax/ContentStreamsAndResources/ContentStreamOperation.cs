namespace ZingPDF.Syntax.ContentStreamsAndResources;

public class ContentStreamOperation : ICloneable
{
    public required string Operator { get; init; }
    public List<IPdfObject>? Operands { get; set; }

    public T GetOperand<T>(int index) where T : class, IPdfObject
        => Operands?[index] as T ?? throw new InvalidOperationException();

    public object Clone()
    {
        var clone = (ContentStreamOperation)MemberwiseClone();

        if (Operands is not null)
        {
            clone.Operands = [.. Operands.Select(o => (IPdfObject)o.Clone())];
        }

        return clone;
    }
}
