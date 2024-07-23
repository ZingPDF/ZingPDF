using FluentAssertions;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Parsing.Parsers.Objects;

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

    [Fact]
    public async Task ParseIndirectObjectStream()
    {
        var contentString = "90824 0 obj\r\n" +
            "<</DecodeParms<</Columns 5/Predictor 12>>/Filter/FlateDecode/ID[<2B551D2AFE52654494F9720283CFF1C4><3CDA8BB6D5834E41A5E2AA16C35E4C47>]/Index[90793 1014]/Info 90792 0 R/Length 185/Prev 14709647/Root 90794 0 R/Size 91807/Type/XRef/W[1 3 1]>>stream\r\n" +
            "h���1\u000eAA\u0014��;�\u0013\u0011#�B+Qj�D!t*a\t��\b�\u0005�\u0015h\u0015l@4J��\u000f�\u0010���ـ��i���L&���YټY�\u0001}\u0004�U���hI����B\a�A�0ׇ�\tl�`f\f�#�<��=�o�.~��\u0014�\u001e,�9o`�\u0006�!̹9q��Zj�8�wn����}������?���\a������JJu%ՕTWR�+���\a�d�:�\u0005\u0018\0'P)Q\r\n" +
            "endstream\r\n" +
            "endobj\r\n";

        var output = await new IndirectObjectParser().ParseAsync(contentString.ToStream());

        output.Id.Index.Should().Be(90824);
        output.Id.GenerationNumber.Should().Be(0);
        output.Children.Should().HaveCount(1);
        output.Children.First().Should().BeAssignableTo<IStreamObject<IStreamDictionary>>();
    }
}
