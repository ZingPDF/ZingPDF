namespace ZingPDF.ObjectModel.ContentStreamsAndResources
{
    internal interface IContentStreamInstruction
    {
        string Operator { get; }
        IPdfObject Operand { get; }
    }
}
