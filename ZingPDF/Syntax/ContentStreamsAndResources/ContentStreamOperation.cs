namespace ZingPDF.Syntax.ContentStreamsAndResources;

public record ContentStreamOperation
{
    public required string Operator { get; init; }
    public List<IPdfObject>? Operands { get; set; }

    public T GetOperand<T>(int index) where T : class, IPdfObject
        => Operands?[index] as T ?? throw new InvalidOperationException();
}
