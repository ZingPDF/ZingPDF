using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Parsing.Parsers.FileStructure;

public class TrailerParserTests
{
    [Fact]
    public async Task ParseBasic()
    {
        var input = "trailer\r\n" +
            "<</Size 22\r\n" +
            "/Root 2 0 R\r\n" +
            "/Info 1 0 R\r\n" +
            "/ID [<81b14aafa313db63dbd6f981e49f94f4>\r\n" +
            "<81b14aafa313db63dbd6f981e49f94f4>\r\n" +
            "]\r\n" +
            ">>\r\n" +
            "startxref\r\n" +
            "18799\r\n" +
            "%%EOF\r\n";

        var stream = input.ToStream();

        var services = new ServiceCollection()
            .AddContext(stream)
            .AddParsers()
            .BuildServiceProvider();

        using var scope = services.CreateScope();

        var dictionaryParser = scope.ServiceProvider.GetRequiredService<IParser<Dictionary>>();
        var keywordParser = scope.ServiceProvider.GetRequiredService<IParser<Keyword>>();
        var numberParser = scope.ServiceProvider.GetRequiredService<IParser<Number>>();

        var trailer = await new TrailerParser(dictionaryParser, keywordParser, numberParser)
            .ParseAsync(input.ToStream(), ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

        trailer.Dictionary.Size.Value.Should().Be(22);

        trailer.Dictionary.Root.Id.Index.Should().Be(2);
        trailer.Dictionary.Root.Id.GenerationNumber.Should().Be(0);

        trailer.Dictionary.Info.Should().NotBeNull();
        trailer.Dictionary.Info!.Id.Index.Should().Be(1);
        trailer.Dictionary.Info!.Id.GenerationNumber.Should().Be(0);

        trailer.Dictionary.ID.Should().NotBeNull();
        trailer.Dictionary.ID!.Should().HaveCount(2);

        trailer.XrefTableByteOffset.Should().Be(18799);
    }
}
