
namespace ZingPDF.ObjectModel.ContentStreamsAndResources
{
    internal class ContentStreamInstruction
    {
        public ContentStreamInstruction(string @operator, IPdfObject operand)
        {
            Operator = @operator ?? throw new ArgumentNullException(nameof(@operator));
            Operand = operand ?? throw new ArgumentNullException(nameof(operand));
        }

        public string Operator { get; }
        public IPdfObject Operand { get; }
    }
}
