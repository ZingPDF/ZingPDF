namespace ZingPDF.Objects
{
    public class PdfSaveOptions
    {
        public bool Linearize { get; set; }

        public static PdfSaveOptions Default = new();
    }
}