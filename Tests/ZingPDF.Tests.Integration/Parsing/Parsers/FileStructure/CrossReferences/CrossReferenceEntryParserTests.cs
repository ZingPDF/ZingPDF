using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences;

public class CrossReferenceEntryParserTests
{
    [Theory]
    [InlineData("0000000000 65535 f\n", 0, 65535, false)]
    [InlineData("0000000017 00000 n\n", 17, 0, true)]
    public async Task ParseAsyncBasic(string input, long expectedOffset, ushort expectedGenNumber, bool expectedInUse)
    {
        var pdf = A.Fake<IPdf>();
        var stream = input.ToStream();

        A.CallTo(() => pdf.Data).Returns(stream);

        var services = new ServiceCollection()
            .AddContext(pdf)
            //.AddParsers()
            .BuildServiceProvider();

        using var scope = services.CreateScope();

        var numberParser = scope.ServiceProvider.GetRequiredService<IParser<Number>>();
        var keywordParser = scope.ServiceProvider.GetRequiredService<IParser<Keyword>>();

        var output = await new CrossReferenceEntryParser(numberParser, keywordParser)
            .ParseAsync(ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject));

        output.Value1.Should().Be(expectedOffset);
        output.Value2.Should().Be(expectedGenNumber);
        output.InUse.Should().Be(expectedInUse);
    }
}
