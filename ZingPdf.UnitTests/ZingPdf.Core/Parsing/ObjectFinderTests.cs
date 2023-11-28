using FluentAssertions;
using Xunit;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Parsing
{
    public class ObjectFinderTests
    {
        [Fact]
        public async Task FindAsyncBasicAsync()
        {
            var contentString = "49 0 obj\r\n<< /Type /Catalog /Version /1.7 /Pages 1 0 R " +
                "/Names << >> /ViewerPreferences << /Direction /L2R >> " +
                "/PageLayout /SinglePage /PageMode /UseNone /OpenAction [12 0 R /FitH null] " +
                "/Metadata 48 0 R >>\r\nendobj\r\nxref\r\n0 50\r\n0000000000 65535 f \r\n0000004797 00000 n " +
                "\r\n0000141942 00000 n \r\n0000092042 00000 n \r\n0000096211 00000 n \r\n0000100424 00000 n " +
                "\r\n0000105717 00000 n \r\n0000110988 00000 n \r\n0000116278 00000 n \r\n0000142129 00000 n " +
                "\r\n0000142441 00000 n \r\n0000142895 00000 n \r\n0000000015 00000 n \r\n0000000492 00000 n " +
                "\r\n0000002891 00000 n \r\n0000003362 00000 n \r\n0000004864 00000 n \r\n0000023927 00000 n " +
                "\r\n0000046456 00000 n \r\n0000069393 00000 n \r\n0000092192 00000 n \r\n0000093943 00000 n " +
                "\r\n0000094937 00000 n \r\n0000095214 00000 n \r\n0000096372 00000 n \r\n0000098123 00000 n " +
                "\r\n0000099137 00000 n \r\n0000099425 00000 n \r\n0000100583 00000 n \r\n0000102334 00000 n " +
                "\r\n0000104424 00000 n \r\n0000104718 00000 n \r\n0000105871 00000 n \r\n0000107622 00000 n " +
                "\r\n0000109707 00000 n \r\n0000109989 00000 n \r\n0000111147 00000 n \r\n0000112898 00000 n " +
                "\r\n0000114985 00000 n \r\n0000115279 00000 n \r\n0000116432 00000 n \r\n0000118183 00000 n " +
                "\r\n0000120273 00000 n \r\n0000120554 00000 n \r\n0000121553 00000 n \r\n0000123005 00000 n " +
                "\r\n0000126392 00000 n \r\n0000143168 00000 n \r\n0000143513 00000 n \r\n0000147866 00000 n " +
                "\r\ntrailer\r\n<< /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> " +
                "<66dbd809c84b6f6bd19bb2f8865b77cc> ] >>\r\nstartxref\r\n148076\r\n%%EOF\r\n";

            using var input = contentString.ToStream();

            await new ObjectFinder().FindAsync(input, Constants.Trailer, forwards: false);

            input.Position.Should().Be(1275);
        }
    }
}
