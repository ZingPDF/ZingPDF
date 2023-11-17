using FluentAssertions;
using Xunit;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core
{
    public class PdfUpdateManagerTests
    {
        [Fact]
        public async Task SimpleIncrementalUpdate()
        {
            var manager = new IncrementalUpdateManager();

            var dummyObject = new LiteralString("");
            var dummyIndirectObject = new IndirectObject(new IndirectObjectId(1, 0), dummyObject);

            manager.AddObject(dummyIndirectObject);

            var inputStream = File.Open("TestFiles/minimal.pdf", FileMode.Open);
            var outputStream = File.Open("output.pdf", FileMode.Create);

            inputStream.CopyTo(outputStream);

            await manager.SaveAsync(outputStream);

            outputStream.Position = 0;

            var output = await outputStream.GetAsync();

            var expectedOutput = "%PDF-2.0\r\n" +
                "%����\r\n" +
                "1 0 obj\r\n" +
                "<</Type /Catalog/Pages 2 0 R>>\r\n" +
                "endobj\r\n" +
                "2 0 obj\r\n" +
                "<</Type /Pages/Kids [3 0 R]/Count 1>>\r\n" +
                "endobj\r\n" +
                "3 0 obj\r\n" +
                "<</Type /Page/Parent 2 0 R/Resources <<>>>>\r\n" +
                "endobj\r\n" +
                "xref\r\n" +
                "0 4\r\n" +
                "0000000000 65535 f\r\n" +
                "0000000017 00000 n\r\n" +
                "0000000066 00000 n\r\n" +
                "0000000122 00000 n\r\n" +
                "trailer\r\n" +
                "<</Size 4/Root 1 0 R/ID [<2045e2246d17437290c929c74954eb23> <2045e2246d17437290c929c74954eb23>]>>\r\n" +
                "startxref\r\n" +
                "184\r\n" +
                "%%EOF\r\n" +
                "1 0 obj\r\n" +
                "(﻿)\r\n" +
                "endobj\r\n" +
                "xref\r\n" +
                "0 2\r\n" +
                "0000000000 65535 f\r\n" +
                "0000000406 00000 n\r\n" +
                "trailer\r\n" +
                "<</Size 4/Root 1 0 R/ID [<2045e2246d17437290c929c74954eb23> <2045e2246d17437290c929c74954eb23>]/Prev 184>>\r\n" +
                "startxref\r\n" +
                "430\r\n" +
                "%%EOF\r\n";

            output.Should().Be(expectedOutput);
        }

        //[Fact]
        //public async Task GenerateCrossReferencesSplitSections()
        //{
        //    var manager = new PdfUpdateManager();

        //    var dummyObject = new LiteralString("");
        //    var dummyIndirectObject = new IndirectObject(new IndirectObjectId(1, 0), dummyObject);

        //    var dummyObject2 = new LiteralString("");
        //    var dummyIndirectObject2 = new IndirectObject(new IndirectObjectId(3, 0), dummyObject2);

        //    manager.AddObject(dummyIndirectObject);
        //    manager.AddObject(dummyIndirectObject2);

        //    var ms = new MemoryStream();

        //    await dummyIndirectObject.WriteAsync(ms);
        //    await dummyIndirectObject2.WriteAsync(ms);

        //    await manager.GenerateCrossReferences().WriteAsync(ms);

        //    ms.Position = 0;

        //    var output = await ms.GetAsync();

        //    var expectedOutput = "1 0 obj\r\n" +
        //        "(﻿)\r\n" +
        //        "endobj\r\n" +
        //        "3 0 obj\r\n" +
        //        "(﻿)\r\n" +
        //        "endobj\r\n" +
        //        "xref\r\n" +
        //        "0 2\r\n" +
        //        "0000000000 65535 f\r\n" +
        //        "0000000000 00000 n\r\n" +
        //        "3 1\r\n" +
        //        "0000000024 00000 n\r\n";

        //    output.Should().Be(expectedOutput);
        //}

        //[Fact]
        //public async Task GenerateCrossReferencesMultipleSplitSections()
        //{
        //    var manager = new PdfUpdateManager();

        //    var dummyObject = new LiteralString("");
        //    var dummyIndirectObject = new IndirectObject(new IndirectObjectId(1, 0), dummyObject);

        //    var dummyObject2 = new LiteralString("");
        //    var dummyIndirectObject2 = new IndirectObject(new IndirectObjectId(3, 0), dummyObject2);

        //    var dummyObject3 = new LiteralString("");
        //    var dummyIndirectObject3 = new IndirectObject(new IndirectObjectId(5, 0), dummyObject3);

        //    manager.AddObject(dummyIndirectObject);
        //    manager.AddObject(dummyIndirectObject2);
        //    manager.AddObject(dummyIndirectObject3);

        //    var ms = new MemoryStream();

        //    await dummyIndirectObject.WriteAsync(ms);
        //    await dummyIndirectObject2.WriteAsync(ms);
        //    await dummyIndirectObject3.WriteAsync(ms);

        //    await manager.GenerateCrossReferences().WriteAsync(ms);

        //    ms.Position = 0;

        //    var output = await ms.GetAsync();

        //    var expectedOutput = "1 0 obj\r\n" +
        //        "(﻿)\r\n" +
        //        "endobj\r\n" +
        //        "3 0 obj\r\n" +
        //        "(﻿)\r\n" +
        //        "endobj\r\n" +
        //        "5 0 obj\r\n" +
        //        "(﻿)\r\n" +
        //        "endobj\r\n" +
        //        "xref\r\n" +
        //        "0 2\r\n" +
        //        "0000000000 65535 f\r\n" +
        //        "0000000000 00000 n\r\n" +
        //        "3 1\r\n" +
        //        "0000000024 00000 n\r\n" +
        //        "5 1\r\n" +
        //        "0000000048 00000 n\r\n";

        //    output.Should().Be(expectedOutput);
        //}
    }
}
