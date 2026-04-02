using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers;

public class ContentStreamParserTests
{
    private static ContentStreamParser CreateParser()
    {
        var parserResolver = new ServiceCollection()
            .AddContext(A.Dummy<IPdf>())
            .AddParsers()
            .BuildServiceProvider()
            .GetRequiredService<IParserResolver>();

        return new ContentStreamParser(parserResolver, new TokenTypeIdentifier());
    }

    [Fact]
    public async Task ParseAsync_GroupsOperatorsWithTheirOperands()
    {
        using var input = "/F1 12 Tf (Hello) Tj 0 -14 Td [(A) 120 (B)] TJ".ToStream();

        var output = await CreateParser().ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.ParsedContentStream));

        output.Operations.Should().HaveCount(4);

        output.Operations[0].Operator.Should().Be(ContentStream.Operators.TextState.Tf);
        output.Operations[0].Operands.Should().HaveCount(2);
        output.Operations[0].GetOperand<Name>(0).Value.Should().Be("F1");
        ((double)output.Operations[0].GetOperand<Number>(1)).Should().Be(12d);

        output.Operations[1].Operator.Should().Be(ContentStream.Operators.TextShowing.Tj);
        output.Operations[1].GetOperand<PdfString>(0).Bytes.Should().Equal("Hello"u8.ToArray());

        output.Operations[2].Operator.Should().Be(ContentStream.Operators.TextPositioning.Td);
        output.Operations[2].Operands.Should().HaveCount(2);

        output.Operations[3].Operator.Should().Be(ContentStream.Operators.TextShowing.TJ);
        var array = output.Operations[3].GetOperand<ArrayObject>(0);
        array.Should().HaveCount(3);
        array[0].Should().BeOfType<PdfString>();
        array[1].Should().BeOfType<Number>();
        array[2].Should().BeOfType<PdfString>();
    }

    [Fact]
    public async Task ParseAsync_IgnoresComments()
    {
        using var input = "% leading comment\r\n/F1 12 Tf % font change\r\n(Hello) Tj % trailing".ToStream();

        var output = await CreateParser().ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.ParsedContentStream));

        output.Operations.Should().HaveCount(2);
        output.Operations[0].Operator.Should().Be(ContentStream.Operators.TextState.Tf);
        output.Operations[0].Operands.Should().NotContainItemsAssignableTo<Comment>();
        output.Operations[1].Operator.Should().Be(ContentStream.Operators.TextShowing.Tj);
        output.Operations[1].Operands.Should().NotContainItemsAssignableTo<Comment>();
        input.Position.Should().Be(input.Length);
    }

    [Fact]
    public async Task ParseAsync_KeepsKeywordOperandsThatAreNotOperators()
    {
        using var input = "/Span null BDC EMC".ToStream();

        var output = await CreateParser().ParseAsync(input, ObjectContext.WithOrigin(ObjectOrigin.ParsedContentStream));

        output.Operations.Should().HaveCount(2);
        output.Operations[0].Operator.Should().Be(ContentStream.Operators.MarkedContent.BDC);
        output.Operations[0].Operands.Should().HaveCount(2);
        output.Operations[0].GetOperand<Name>(0).Value.Should().Be("Span");
        output.Operations[0].GetOperand<Keyword>(1).Value.Should().Be(Constants.Null);

        output.Operations[1].Operator.Should().Be(ContentStream.Operators.MarkedContent.EMC);
        output.Operations[1].Operands.Should().BeNull();
    }
}
