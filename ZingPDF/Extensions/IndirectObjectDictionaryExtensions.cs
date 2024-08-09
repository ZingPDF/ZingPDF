using ZingPDF.IncrementalUpdates;

namespace ZingPDF.Extensions
{
    public static class IndirectObjectDictionaryExtensions
    {
        public static void EnsureEditable(this IIndirectObjectDictionary indirectObjectDictionary)
        {
            if (indirectObjectDictionary is null || indirectObjectDictionary is not IndirectObjectManager)
            {
                throw new InvalidOperationException("PDF is immutable. To access update operations, ensure you've opened the PDF for editing, such as with PdfParser.OpenAsync");
            }
        }
    }
}
