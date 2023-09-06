using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects
{
    /// <summary>
    /// PDF 32000-1:2008 7.5.2
    /// </summary>
    internal class Header : PdfObject
    {
        private readonly double _pdfVersion;

        public Header(double pdfVersion = 1.7)
        {
            if (pdfVersion < 1 || pdfVersion > 1.7) throw new ArgumentOutOfRangeException(nameof(pdfVersion), "Version must be between 1.0 and 1.7");

            _pdfVersion = pdfVersion;
        }

        public override async Task WriteOutputAsync(Stream stream)
        {
            // Write PDF version number
            await stream.WriteTextAsync($"%PDF-{_pdfVersion:0.#}");
            await stream.WriteNewLineAsync();

            // Write binary bytes to cater for readers which try to detect whether file contains binary data
            await stream.WriteTextAsync("%");
            await stream.WriteAsync(new byte[] { 129, 130, 131, 132 });

            await stream.WriteNewLineAsync();
        }
    }
}
