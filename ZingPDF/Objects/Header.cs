using ZingPDF;
using ZingPDF.Extensions;

namespace ZingPDF.Objects
{
    /// <summary>
    /// ISO 32000-2:2020 7.5.2
    /// </summary>
    internal class Header : PdfObject
    {
        public Header(double pdfVersion = 2.0)
        {
            if (!ZingPDF.PdfVersion.All.Contains(pdfVersion)) throw new ArgumentOutOfRangeException(nameof(pdfVersion), $"Invalid PDF version specified: {pdfVersion}");

            PdfVersion = pdfVersion;
        }

        public double PdfVersion { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            // Write PDF version number
            await stream.WriteTextAsync($"{Constants.Comment}{Constants.PdfVersionPrefix}{PdfVersion:0.0}");
            await stream.WriteNewLineAsync();

            // Write binary bytes to cater for readers which try to detect whether file contains binary data
            await stream.WriteCharsAsync(Constants.Comment);
            await stream.WriteAsync(Constants.BinaryCharacters);

            await stream.WriteNewLineAsync();
        }
    }
}
