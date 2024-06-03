namespace ZingPDF
{
    public class PdfSaveOptions
    {
        private static readonly PdfSaveOptions _default = new();

        public bool Linearize { get; set; }

        public static readonly PdfSaveOptions Default = _default;
    }
}