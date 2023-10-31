using FluentAssertions;
using Xunit;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    public class IndirectObjectParserTests
    {
        [Fact]
        public async Task ParseAsyncBasic()
        {
            // TODO: make this (and all tests) work without depending on nested static parsers

            var contentString = "12 0 obj\r\n" +
                "<< " +
                "/Type /Page " +
                "/Parent 1 0 R " +
                "/LastModified (D:20230922161207+10'00') " +
                "/Resources 2 0 R " +
                "/MediaBox [0.000000 0.000000 595.276000 841.890000] " +
                "/CropBox [0.000000 0.000000 595.276000 841.890000] " +
                "/BleedBox [0.000000 0.000000 595.276000 841.890000] " +
                "/TrimBox [0.000000 0.000000 595.276000 841.890000] " +
                "/ArtBox [0.000000 0.000000 595.276000 841.890000] " +
                "/Contents 13 0 R " +
                "/Rotate 0 " +
                "/Group << /Type /Group /S /Transparency /CS /DeviceRGB >> " +
                "/Annots [ 9 0 R 10 0 R ] " +
                "/PZ 1 " +
                ">>\r\n" +
                "endobj";

            var output = await new IndirectObjectParser().ParseAsync(contentString.ToStream());

            output.Id.Index.Should().Be(12);
            output.Id.GenerationNumber.Should().Be(0);
            output.Children.Should().HaveCount(1);
        }
    }
}
