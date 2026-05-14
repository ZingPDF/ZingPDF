using System.Globalization;
using Fluid;

namespace ZingPDF.Templates.LiquidHtml;

/// <summary>
/// Configures Liquid HTML template rendering before HTML is converted to PDF.
/// </summary>
public sealed class LiquidHtmlPdfTemplateOptions
{
    /// <summary>
    /// Gets or sets the culture used by Liquid filters that format locale-sensitive values.
    /// </summary>
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// Gets or sets the base path for Liquid includes.
    /// File-backed templates default to the template directory.
    /// </summary>
    public string? BasePath { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of Liquid evaluation steps before rendering is stopped.
    /// </summary>
    public int? MaxSteps { get; set; }

    /// <summary>
    /// Gets or sets a callback that can register Fluid filters, member access rules, or other template options.
    /// </summary>
    public Action<TemplateOptions>? ConfigureTemplateOptions { get; set; }
}
