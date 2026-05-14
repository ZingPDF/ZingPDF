using System.Text.Encodings.Web;
using System.Collections;
using Fluid;
using Microsoft.Extensions.FileProviders;
using ZingPDF.Templates;

namespace ZingPDF.Templates.LiquidHtml;

/// <summary>
/// Renders a Liquid HTML template to PDF using Fluid and ZingPDF.FromHTML.
/// </summary>
public sealed class LiquidHtmlPdfTemplate
{
    private readonly PdfTemplateSource _source;
    private readonly IHtmlToPdfConverter _converter;

    private LiquidHtmlPdfTemplate(PdfTemplateSource source, IHtmlToPdfConverter converter)
    {
        _source = source;
        _converter = converter;
    }

    /// <summary>
    /// Creates a Liquid HTML PDF template from a file path.
    /// </summary>
    public static LiquidHtmlPdfTemplate FromFile(string path)
        => FromSource(PdfTemplateSource.FromFile(path));

    /// <summary>
    /// Creates a Liquid HTML PDF template from an in-memory template string.
    /// </summary>
    public static LiquidHtmlPdfTemplate FromString(string template, string? displayName = null)
        => FromSource(PdfTemplateSource.FromString(template, displayName));

    /// <summary>
    /// Creates a Liquid HTML PDF template from a template source.
    /// </summary>
    public static LiquidHtmlPdfTemplate FromSource(PdfTemplateSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new LiquidHtmlPdfTemplate(source, new ZingPdfHtmlToPdfConverter());
    }

    /// <summary>
    /// Renders the template model to HTML without converting it to PDF.
    /// This is useful for diagnostics, previews, and tests.
    /// </summary>
    public async Task<string> RenderHtmlAsync<TModel>(
        TModel model,
        LiquidHtmlPdfTemplateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var templateText = await ReadTemplateAsync(cancellationToken);
        var template = ParseTemplate(templateText);
        var renderOptions = CreateTemplateOptions(options);
        RegisterModelTypes(renderOptions, model);
        var context = new TemplateContext(model, renderOptions, true);

        if (options?.Culture is not null)
        {
            context.CultureInfo = options.Culture;
        }

        if (options?.MaxSteps is not null)
        {
            context.MaxSteps = options.MaxSteps.Value;
        }

        try
        {
            return await template.RenderAsync(context, HtmlEncoder.Default);
        }
        catch (Exception ex) when (ex is not PdfTemplateRenderException)
        {
            throw new PdfTemplateRenderException(
                $"Unable to render Liquid template '{_source.DisplayName}'.",
                ex);
        }
    }

    /// <summary>
    /// Renders the template model to a PDF stream.
    /// </summary>
    public async Task RenderAsync<TModel>(
        TModel model,
        Stream output,
        LiquidHtmlPdfTemplateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (!output.CanWrite)
        {
            throw new ArgumentException("The output stream must be writable.", nameof(output));
        }

        var html = await RenderHtmlAsync(model, options, cancellationToken);
        await using var pdf = await _converter.ConvertAsync(html, cancellationToken);
        await pdf.CopyToAsync(output, cancellationToken);
    }

    internal static LiquidHtmlPdfTemplate FromSource(PdfTemplateSource source, IHtmlToPdfConverter converter)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(converter);

        return new LiquidHtmlPdfTemplate(source, converter);
    }

    private async Task<string> ReadTemplateAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _source.ReadAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not PdfTemplateRenderException)
        {
            throw new PdfTemplateRenderException(
                $"Unable to read Liquid template '{_source.DisplayName}'.",
                [
                    new PdfTemplateDiagnostic(
                        PdfTemplateDiagnosticSeverity.Error,
                        ex.Message,
                        _source.DisplayName)
                ],
                ex);
        }
    }

    private IFluidTemplate ParseTemplate(string templateText)
    {
        var parser = new FluidParser();

        if (!parser.TryParse(templateText, out var template, out var error))
        {
            var diagnostic = new PdfTemplateDiagnostic(
                PdfTemplateDiagnosticSeverity.Error,
                error,
                _source.DisplayName);

            throw new PdfTemplateRenderException(
                $"Unable to parse Liquid template '{_source.DisplayName}'.",
                [diagnostic]);
        }

        return template;
    }

    private TemplateOptions CreateTemplateOptions(LiquidHtmlPdfTemplateOptions? options)
    {
        var templateOptions = new TemplateOptions();
        var basePath = options?.BasePath ?? _source.BasePath;

        if (!string.IsNullOrWhiteSpace(basePath))
        {
            templateOptions.FileProvider = new PhysicalFileProvider(Path.GetFullPath(basePath));
        }

        options?.ConfigureTemplateOptions?.Invoke(templateOptions);

        return templateOptions;
    }

    private static void RegisterModelTypes(TemplateOptions options, object? model)
    {
        var visited = new HashSet<Type>();
        RegisterModelTypes(options, model, visited);
    }

    private static void RegisterModelTypes(TemplateOptions options, object? value, HashSet<Type> visited)
    {
        if (value is null)
        {
            return;
        }

        if (value is string)
        {
            return;
        }

        var type = value.GetType();

        if (!ShouldRegister(type) || !visited.Add(type))
        {
            return;
        }

        options.MemberAccessStrategy.Register(type);

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                RegisterModelTypes(options, item, visited);
            }

            return;
        }

        foreach (var property in type.GetProperties())
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            RegisterModelTypes(options, property.GetValue(value), visited);
        }
    }

    private static bool ShouldRegister(Type type)
        => type != typeof(string)
           && !type.IsPrimitive
           && !type.IsEnum
           && type != typeof(decimal)
           && type != typeof(DateTime)
           && type != typeof(DateTimeOffset)
           && type != typeof(Guid);
}
