using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects
{
    /// <summary>
    /// ISO 32000-2:2020 7.5.2
    /// </summary>
    internal class Header : PdfObject
    {
        private readonly double _pdfVersion;

        public Header(double pdfVersion = 2.0)
        {
            if (!PdfVersion.All.Contains(pdfVersion)) throw new ArgumentOutOfRangeException(nameof(pdfVersion), $"Invalid PDF version specified: {pdfVersion}");

            _pdfVersion = pdfVersion;
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            // Write PDF version number
            await stream.WriteTextAsync($"{Constants.Comment}{Constants.PdfVersionPrefix}{_pdfVersion:0.0}");
            await stream.WriteNewLineAsync();

            // Write binary bytes to cater for readers which try to detect whether file contains binary data
            await stream.WriteCharsAsync(Constants.Comment);
            await stream.WriteAsync(Constants.BinaryCharacters);

            await stream.WriteNewLineAsync();
        }
    }
}
