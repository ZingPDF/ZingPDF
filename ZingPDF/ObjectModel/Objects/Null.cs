namespace ZingPDF.ObjectModel.Objects
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.9 - Null object
    /// </summary>
    internal class Null : PdfObject
    {
        protected override Task WriteOutputAsync(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
