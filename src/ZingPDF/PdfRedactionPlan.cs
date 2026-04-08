using System.Text.RegularExpressions;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Fonts;
using ZingPDF.Graphics;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Text;

namespace ZingPDF;

/// <summary>
/// Collects text and region marks and applies them as redaction overlays.
/// </summary>
public sealed class PdfRedactionPlan
{
    private readonly Pdf _pdf;
    private readonly List<PdfRedactionMark> _marks = [];

    internal PdfRedactionPlan(Pdf pdf)
    {
        _pdf = pdf ?? throw new ArgumentNullException(nameof(pdf));
    }

    /// <summary>
    /// Returns the currently marked redaction regions.
    /// </summary>
    public IReadOnlyList<PdfRedactionMark> GetMarks() => [.. _marks];

    /// <summary>
    /// Removes all pending redaction marks.
    /// </summary>
    public void Clear() => _marks.Clear();

    /// <summary>
    /// Adds an explicit page region as a redaction mark.
    /// </summary>
    public PdfRedactionPlan MarkRegion(int pageNumber, Rectangle bounds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentNullException.ThrowIfNull(bounds);

        _marks.Add(new PdfRedactionMark
        {
            PageNumber = pageNumber,
            Bounds = Rectangle.FromCoordinates(
                new Coordinate(bounds.LowerLeft.X, bounds.LowerLeft.Y),
                new Coordinate(bounds.UpperRight.X, bounds.UpperRight.Y)),
            Kind = PdfRedactionKind.Region
        });

        return this;
    }

    /// <summary>
    /// Adds redaction marks for exact text matches found in extracted glyph runs.
    /// </summary>
    public async Task<int> MarkTextAsync(string text, StringComparison comparison = StringComparison.Ordinal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var letters = await _pdf.ExtractTextAsync(new TextExtractionOptions
        {
            OutputKind = TextExtractionOutputKind.Letters
        });

        var count = 0;
        foreach (var run in letters.Letters ?? [])
        {
            var runText = new string(run.Glyphs.Select(static glyph => glyph.Character).ToArray());
            var searchStart = 0;

            while (true)
            {
                var matchIndex = runText.IndexOf(text, searchStart, comparison);
                if (matchIndex < 0)
                {
                    break;
                }

                AddGlyphRangeMark(run, matchIndex, text.Length, PdfRedactionKind.TextMatch, text);
                count++;
                searchStart = matchIndex + text.Length;
            }
        }

        return count;
    }

    /// <summary>
    /// Adds redaction marks for regular-expression matches found in extracted glyph runs.
    /// </summary>
    public async Task<int> MarkTextAsync(Regex regex)
    {
        ArgumentNullException.ThrowIfNull(regex);

        var letters = await _pdf.ExtractTextAsync(new TextExtractionOptions
        {
            OutputKind = TextExtractionOutputKind.Letters
        });

        var count = 0;
        foreach (var run in letters.Letters ?? [])
        {
            var runText = new string(run.Glyphs.Select(static glyph => glyph.Character).ToArray());

            foreach (Match match in regex.Matches(runText))
            {
                if (!match.Success || match.Length == 0)
                {
                    continue;
                }

                AddGlyphRangeMark(run, match.Index, match.Length, PdfRedactionKind.RegexMatch, match.Value);
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Applies the pending redaction marks to the PDF and prepares the save model.
    /// </summary>
    public async Task<PdfRedactionReport> ApplyAsync(PdfRedactionOptions? options = null)
    {
        options ??= new PdfRedactionOptions();

        if (options.RequireRewriteForSave && !options.RewriteFile)
        {
            throw new InvalidOperationException("Redaction requires rewritten-file save behavior unless you explicitly opt out.");
        }

        PdfFont? overlayFont = null;
        if (!string.IsNullOrWhiteSpace(options.OverlayText))
        {
            overlayFont = await _pdf.RegisterStandardFontAsync(options.OverlayFontName);
        }

        foreach (var mark in _marks.OrderBy(static mark => mark.PageNumber))
        {
            var page = await _pdf.GetPageAsync(mark.PageNumber);
            await ApplyFillOverlayAsync(page, mark.Bounds, options.FillColor);

            if (overlayFont is not null)
            {
                await page.AddTextAsync(
                    options.OverlayText!,
                    mark.Bounds,
                    overlayFont,
                    options.OverlayFontSize,
                    options.OverlayTextColor,
                    new TextLayoutOptions
                    {
                        HorizontalAlignment = TextHorizontalAlignment.Center,
                        VerticalAlignment = TextVerticalAlignment.Middle,
                        Overflow = TextOverflowMode.ShrinkToFit,
                        MinFontSize = 4,
                        Padding = TextPadding.None
                    });
            }
        }

        if (options.RewriteFile)
        {
            await _pdf.RemoveHistoryAsync();
        }

        return new PdfRedactionReport
        {
            AppliedMarkCount = _marks.Count,
            PagesTouched = _marks.Select(static mark => mark.PageNumber).Distinct().OrderBy(static page => page).ToArray(),
            Warnings =
            [
                "This version applies redaction overlays and forces rewritten-file save behavior by default.",
                "High-level redaction does not yet rewrite every arbitrary painted operator or rasterize image content for region removal."
            ]
        };
    }

    private void AddGlyphRangeMark(GlyphRun run, int startIndex, int length, PdfRedactionKind kind, string sourceText)
    {
        var glyphs = run.Glyphs.Skip(startIndex).Take(length).ToArray();
        if (glyphs.Length == 0)
        {
            return;
        }

        var minX = glyphs.Min(static glyph => glyph.X);
        var minY = glyphs.Min(static glyph => glyph.Y);
        var maxX = glyphs.Max(static glyph => glyph.X + glyph.Width);
        var maxY = glyphs.Max(static glyph => glyph.Y + glyph.Height);

        _marks.Add(new PdfRedactionMark
        {
            PageNumber = run.PageNumber,
            Bounds = Rectangle.FromCoordinates(
                new Coordinate(minX, minY),
                new Coordinate(maxX, maxY)),
            Kind = kind,
            SourceText = sourceText
        });
    }

    private static Task ApplyFillOverlayAsync(Page page, Rectangle bounds, RGBColour fillColor)
    {
        var path = new ZingPDF.Elements.Drawing.Path(
            strokeOptions: null,
            fillOptions: new FillOptions(fillColor),
            type: PathType.Linear,
            points:
            [
                new Coordinate(bounds.LowerLeft.X, bounds.LowerLeft.Y),
                new Coordinate(bounds.UpperRight.X, bounds.LowerLeft.Y),
                new Coordinate(bounds.UpperRight.X, bounds.UpperRight.Y),
                new Coordinate(bounds.LowerLeft.X, bounds.UpperRight.Y),
                new Coordinate(bounds.LowerLeft.X, bounds.LowerLeft.Y)
            ]);

        return page.AddPathAsync(path);
    }
}
