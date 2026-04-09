using System.Reflection;
using FluentAssertions;
using ZingPDF.Parsing;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using Xunit;

namespace ZingPDF.Tests.Unit.Parsing.Parsers;

public class StrictContentStreamParserTests
{
    [Fact]
    public async Task ParseAsync_PreservesRgbStrokeOperator()
    {
        using var pdf = Pdf.Create();
        var services = (IServiceProvider?)typeof(Pdf)
            .GetField("_services", BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(pdf);

        var parser = new StrictContentStreamParser(
            (IParserResolver)services!.GetService(typeof(IParserResolver))!,
            (ITokenTypeIdentifier)services.GetService(typeof(ITokenTypeIdentifier))!);

        using var stream = new MemoryStream("0 0 0 RG 1 w"u8.ToArray());

        var content = await parser.ParseAsync(stream, ObjectContext.UserCreated);

        content.Operations.Should().HaveCount(2);
        var debugOperands = content.Operations[0].Operands is null
            ? "<null>"
            : string.Join(", ", content.Operations[0].Operands.Select(x => $"{x.GetType().Name}:{x}"));
        content.Operations[0].Operator.Should().Be(ContentStream.Operators.Colour.RG, debugOperands);
        content.Operations[0].Operands.Should().HaveCount(3);
        content.Operations[1].Operator.Should().Be(ContentStream.Operators.GeneralGraphicsState.w);
    }
}
