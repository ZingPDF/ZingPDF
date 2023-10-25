//using FluentAssertions;
//using System.Text;
//using Xunit;

//namespace ZingPdf.Core.Objects
//{
//    /// <summary>
//    /// ISO 32000-2:2020 7.5.2
//    /// </summary>
//    public class HeaderTests
//    {
//        /// <summary>
//        /// "The file header shall consist of “%PDF–1.n” or “%PDF–2.n” followed by a single EOL marker, 
//        /// where ‘n’ is a single digit number between 0 (30h) and 9 (39h)."
//        /// </summary>
//        [Fact]
//        public async Task OutputStartsWithPdfVersion()
//        {
//            using var s = await new Pdf().ToStreamAsync();

//            byte[] output = new byte[9];

//            await s.ReadAsync(output.AsMemory(0, 9));

//            Encoding.ASCII.GetString(output).Should().Be("%PDF-2.0\n");
//        }

//        // TODO: test for when arbitrary bytes appear prior to version information
//        // "The PDF file begins with the 5 characters “%PDF–” and byte offsets shall be calculated from the PERCENT SIGN (25h).
//        // NOTE 1 This provision allows for arbitrary bytes preceding the %PDF- without impacting the viability of the PDF file and its byte offsets."

//        /// <summary>
//        /// "If a PDF file contains binary data, as most do (see 7.2, "Lexical conventions"), 
//        /// the header line shall be immediately followed by a comment line containing 
//        /// at least four binary characters–that is, characters whose codes are 128 or greater. 
//        /// This ensures proper behaviour of file transfer applications that inspect data near 
//        /// the beginning of a file to determine whether to treat the file’s contents as text or as binary."
//        /// </summary>
//        [Fact]
//        public async Task OutputContainsArbitraryByteData()
//        {
//            using var s = await new Pdf().ToStreamAsync();

//            byte[] output = new byte[4];

//            // Skip PDF version information
//            s.Seek(10, SeekOrigin.Begin);

//            // Read the next 4 bytes into the buffer
//            await s.ReadAsync(output.AsMemory(0, 4));

//            // PDFs produced by this library should contain a line of characters such as %����
//            // immediately following the version line.
//            output.Should().BeEquivalentTo(Constants.BinaryCharacters);
//        }
//    }
//}
