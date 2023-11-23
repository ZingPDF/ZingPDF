using FluentAssertions;
using System.Text;
using Xunit;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.ObjectGroupParsers
{
    public class PdfObjectGroupParserTests
    {
        [Fact]
        public async Task ParseMultilineNameAndDictionary()
        {
            var contentString = "/Pattern <</P6 6 0 R\r\n" +
                "/P7 7 0 R\r\n" +
                "/P8 8 0 R\r\n" +
                "/P9 9 0 R>>\r\n";

            using var input = contentString.ToStream();

            var output = await new PdfObjectGroupParser().ParseAsync(input);

            output.Objects.Should().HaveCount(2);

            output.Get<Name>(0).Should().NotBeNull();
            output.Get<Dictionary>(1).Should().NotBeNull();
        }

        [Fact]
        public async Task ParseMultipleMultilineNamesAndDictionaries()
        {
            var contentString = "/ExtGState <</G3 3 0 R>>\r\n" +
                "/Pattern <</P6 6 0 R\r\n" +
                "/P7 7 0 R\r\n" +
                "/P8 8 0 R\r\n" +
                "/P9 9 0 R>>\r\n";

            using var input = contentString.ToStream();

            var output = await new PdfObjectGroupParser().ParseAsync(input);

            output.Objects.Should().HaveCount(4);

            output.Get<Name>(0).Should().NotBeNull();
            output.Get<Dictionary>(1).Should().NotBeNull();

            output.Get<Name>(2).Should().NotBeNull();
            output.Get<Dictionary>(3).Should().NotBeNull();
        }

        [Fact]
        public async Task ParseOpeningBlockOfLinearizedPdf()
        {
            var streamData = new byte[] {
                0x68, 0xDE, 0xEC, 0xD2, 0x31, 0x0E, 0x41, 0x41, 0x14, 0x85, 0xE1, 0x3B,
                0xC3, 0x13, 0x11, 0x23, 0xF2, 0x42, 0x2B, 0x51, 0x6A, 0x88, 0x44, 0x21,
                0x74, 0x2A, 0x61, 0x09, 0xA2, 0xB1, 0x08, 0xBD, 0x05, 0x88, 0x15, 0x68,
                0x15, 0x6C, 0x40, 0x34, 0x4A, 0xA1, 0xB7, 0x0F, 0xA1, 0x10, 0xD5, 0xF3,
                0xCE, 0xD9, 0x80, 0x86, 0xCA, 0x69, 0xBE, 0xDC, 0xDC, 0x4C, 0x26, 0x93,
                0xC9, 0xEF, 0x9D, 0x59, 0xD9, 0xBC, 0x59, 0xA7, 0x01, 0x7D, 0x04, 0xF3,
                0x55, 0x98, 0xBD, 0xC2, 0x68, 0x49, 0x9F, 0x94, 0xFB, 0xE8, 0x42, 0x07,
                0xF4, 0x41, 0xDB, 0x30, 0xD7, 0x87, 0x85, 0x09, 0x6C, 0xB6, 0x60, 0x66,
                0x0C, 0xEB, 0x23, 0xDE, 0x3C, 0x84, 0x95, 0x3D, 0x8C, 0x6F, 0xA9, 0x2E,
                0x7E, 0xF1, 0xFC, 0x14, 0x86, 0x1E, 0x2C, 0x9D, 0x39, 0x6F, 0x60, 0x91,
                0x06, 0xBE, 0x21, 0xCC, 0xB9, 0x39, 0x71, 0xDE, 0xD2, 0x5A, 0x6A, 0xB2,
                0x38, 0x98, 0x77, 0x6E, 0xD6, 0xC5, 0xC6, 0xCC, 0x7D, 0xDF, 0xF8, 0xFE,
                0xAB, 0x9B, 0xE5, 0x3F, 0xEB, 0xD6, 0xFA, 0x07, 0xA9, 0xAE, 0xA4, 0xBA,
                0x92, 0xEA, 0x4A, 0x4A, 0x75, 0x25, 0xD5, 0x95, 0x54, 0x57, 0x52, 0xAA,
                0x2B, 0xA9, 0xAE, 0xE4, 0x07, 0x93, 0x64, 0xB7, 0x3A, 0xBE, 0x05, 0x18,
                0x00, 0x27, 0x50, 0x29, 0x51
            };

            var contentString = "%PDF-1.7\r\n" +
                "%����\r\n" +
                "90793 0 obj\r\n" +
                "<</Linearized 1/L 14721088/O 90795/E 100639/N 1003/T 14709646/H [ 3414 9955]>>\r\n" +
                "endobj\r\n" +
                " \r\n" +
                "90824 0 obj\r\n" +
                "<</DecodeParms<</Columns 5/Predictor 12>>/Filter/FlateDecode/ID[<2B551D2AFE52654494F9720283CFF1C4><3CDA8BB6D5834E41A5E2AA16C35E4C47>]/Index[90793 1014]/Info 90792 0 R/Length 185/Prev 14709647/Root 90794 0 R/Size 91807/Type/XRef/W[1 3 1]>>stream\r\n";
            
            var contentString2 = "endstream\r\n" +
                "endobj\r\n" +
                "startxref\r\n" +
                "0\r\n" +
                "%%EOF\r\n" +
                "                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  ";

            using var input = new MemoryStream();
            await input.WriteAsync(Encoding.ASCII.GetBytes(contentString));
            await input.WriteAsync(streamData);
            await input.WriteAsync(Encoding.ASCII.GetBytes(contentString2));

            var output = await new PdfObjectGroupParser().ParseAsync(input);
        }
    }
}
