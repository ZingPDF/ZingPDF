using System.Text;
using ZingPDF.Logging;
using ZingPDF.Syntax.CommonDataStructures.Strings;

namespace ZingPDF.Parsing.Parsers.Objects.LiteralStrings
{
    // TODO: This may be duplicated as EncodingDetector.

    /// <summary>
    /// Detects text encoding of a PDF literal string based on BOM:
    /// per PDF spec, only UTF-16BE (0xFEFF) and UTF-8 BOM (0xEFBBBF) are valid. Falls back to PDFDocEncoding.
    /// </summary>
    internal static class PdfStringEncodingDetector
    {
        public struct DetectionResult
        {
            public Encoding Encoding { get; set; }
            public int PreambleLength { get; set; }
        }

        public static DetectionResult Detect(byte[] raw)
        {
            // UTF-16BE BOM (FE FF)
            if (raw.Length >= 2 && raw[0] == 0xFE && raw[1] == 0xFF)
                return new DetectionResult { Encoding = new UnicodeEncoding(true, true), PreambleLength = 2 };
            // UTF-8 BOM (EF BB BF)
            if (raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF)
                return new DetectionResult { Encoding = new UTF8Encoding(true), PreambleLength = 3 };

            // Any other BOM (e.g. FF FE) is non-conformant; log and treat as PDFDocEncoding
            Logger.Log(LogLevel.Warn, "Non-conformant BOM in literal string; defaulting to PDFDocEncoding.");

            return new DetectionResult { Encoding = new PdfDocEncoding(), PreambleLength = 0 };
        }
    }
}
