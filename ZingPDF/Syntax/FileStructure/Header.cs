using ZingPDF.Extensions;

namespace ZingPDF.Syntax.FileStructure
{
    /// <summary>
    /// ISO 32000-2:2020 7.5.2
    /// </summary>
    public class Header : PdfObject
    {
        public Header(double pdfVersion, ObjectOrigin objectOrigin)
            : base(objectOrigin)
        {
            if (!Constants.PdfVersion.All.Contains(pdfVersion)) throw new ArgumentOutOfRangeException(nameof(pdfVersion), $"Invalid PDF version specified: {pdfVersion}");

            PdfVersion = pdfVersion;
        }

        public double PdfVersion { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            // Write PDF version number
            await stream.WriteTextAsync($"{Constants.Characters.Percent}{Constants.PdfVersionPrefix}{PdfVersion:0.0}");
            await stream.WriteNewLineAsync();

            // Write binary bytes to cater for readers which try to detect whether file contains binary data
            await stream.WriteCharsAsync(Constants.Characters.Percent);
            await stream.WriteAsync(Constants.BinaryCharacters);

            await stream.WriteNewLineAsync();
        }

        public override object Clone()
        {
            return new Header(PdfVersion, Origin);
        }
    }
}
