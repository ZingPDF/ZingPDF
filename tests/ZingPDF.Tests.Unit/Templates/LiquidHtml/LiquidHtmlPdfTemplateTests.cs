using FluentAssertions;
using ZingPDF.Templates;
using ZingPDF.Templates.LiquidHtml;
using Xunit;

namespace ZingPDF.Tests.Unit.Templates.LiquidHtml;

public class LiquidHtmlPdfTemplateTests
{
    [Fact]
    public async Task RenderHtmlAsync_RendersModelPropertiesAndLoops()
    {
        var template = LiquidHtmlPdfTemplate.FromString("""
            <h1>Invoice {{ Number }}</h1>
            {% for item in Items %}
            <p>{{ item.Description }}: {{ item.Total }}</p>
            {% endfor %}
            """);

        var html = await template.RenderHtmlAsync(new
        {
            Number = "INV-1001",
            Items = new[]
            {
                new { Description = "Consulting", Total = 240 },
                new { Description = "Support", Total = 80 }
            }
        });

        html.Should().Contain("Invoice INV-1001");
        html.Should().Contain("Consulting: 240");
        html.Should().Contain("Support: 80");
    }

    [Fact]
    public async Task RenderHtmlAsync_ResolvesIncludesFromTemplateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ZingPDF-template-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var templatePath = Path.Combine(directory, "invoice.liquid.html");
            var partialPath = Path.Combine(directory, "line-item.liquid");

            await File.WriteAllTextAsync(templatePath, """
                <h1>{{ Number }}</h1>
                {% for item in Items %}
                {% include 'line-item.liquid' %}
                {% endfor %}
                """);

            await File.WriteAllTextAsync(partialPath, "<p>{{ item }}</p>");

            var template = LiquidHtmlPdfTemplate.FromFile(templatePath);
            var html = await template.RenderHtmlAsync(new
            {
                Number = "INV-1002",
                Items = new[] { "Consulting", "Support" }
            });

            html.Should().Contain("<h1>INV-1002</h1>");
            html.Should().Contain("<p>Consulting</p>");
            html.Should().Contain("<p>Support</p>");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RenderAsync_UsesHtmlConverterAndWritesPdfBytes()
    {
        var converter = new RecordingHtmlToPdfConverter("%PDF-test");
        var template = LiquidHtmlPdfTemplate.FromSource(
            PdfTemplateSource.FromString("<h1>{{ Title }}</h1>"),
            converter);

        await using var output = new MemoryStream();
        await template.RenderAsync(new { Title = "Statement" }, output);

        converter.Html.Should().Contain("<h1>Statement</h1>");
        output.ToArray().Should().Equal("%PDF-test"u8.ToArray());
    }

    [Fact]
    public async Task RenderHtmlAsync_ThrowsTemplateExceptionForInvalidLiquid()
    {
        var template = LiquidHtmlPdfTemplate.FromString("{% if Customer %}Missing end tag");

        var act = () => template.RenderHtmlAsync(new { Customer = "Ada" });

        var exception = await act.Should().ThrowAsync<PdfTemplateRenderException>();
        exception.Which.Diagnostics.Should().ContainSingle(x => x.Severity == PdfTemplateDiagnosticSeverity.Error);
    }

    private sealed class RecordingHtmlToPdfConverter(string pdfText) : IHtmlToPdfConverter
    {
        public string? Html { get; private set; }

        public Task<Stream> ConvertAsync(string html, CancellationToken cancellationToken = default)
        {
            Html = html;
            Stream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(pdfText));
            return Task.FromResult(stream);
        }
    }
}
