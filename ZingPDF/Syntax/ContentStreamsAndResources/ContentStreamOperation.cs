namespace ZingPDF.Syntax.ContentStreamsAndResources;

public record ContentStreamOperation
{
    public required string Operator { get; init; }
    public List<IPdfObject>? Operands { get; set; }
}
