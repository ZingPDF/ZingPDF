using System.Runtime.CompilerServices;
using System.Text;
using ZingPDF.Diagnostics;
using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Elements.Drawing.Text.Extraction;

/// <summary>
/// Extracts text from PDF page content streams using a dedicated low-level scanner.
/// </summary>
public class TextExtractor : ITextExtractor
{
    private readonly IPdf _pdf;
    private readonly IReadOnlyList<IFontMetricsProvider> _baseProviders;
    private readonly Dictionary<string, ResolvedFontResourceSet> _fontResourceSetCache = [];
    private readonly Dictionary<PageExtractionCacheKey, IReadOnlyList<ExtractedText>> _pageExtractionCache = [];
    private readonly Dictionary<PageExtractionCacheKey, string> _pagePlainTextCache = [];

    public TextExtractor(IPdf pdf, IEnumerable<IFontMetricsProvider>? baseProviders = null)
    {
        _pdf = pdf;
        _baseProviders = (baseProviders ?? [new PDFStandardFontMetricsProvider()]).ToArray();
    }

    public async Task<IEnumerable<GlyphRun>> ExtractGlyphRunsAsync()
    {
        var glyphRuns = new List<GlyphRun>();
        var pageNumber = 0;

        await foreach (var page in _pdf.Objects.PageTree.EnumeratePagesAsync())
        {
            pageNumber++;
            glyphRuns.AddRange(await ExtractPageGlyphRunsAsync((PageDictionary)page.Object, pageNumber));
        }

        return glyphRuns;
    }

    public async Task<IEnumerable<ExtractedText>> ExtractTextAsync()
    {
        return (await ExtractTextAsync(new TextExtractionOptions())).Segments!;
    }

    public async Task<IEnumerable<ExtractedText>> ExtractTextAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));

        var page = await _pdf.Objects.PageTree.GetPageAsync(pageNumber);
        if (page is null)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} must reference an existing page.");
        }

        return (await GetExtractedTextForPageAsync((PageDictionary)page.Object, pageNumber)).ToList();
    }

    public async Task<TextExtractionResult> ExtractTextAsync(TextExtractionOptions options)
    {
        using var trace = PerformanceTrace.Measure("TextExtractor.ExtractTextAsync(options)");
        ArgumentNullException.ThrowIfNull(options);

        return options.OutputKind switch
        {
            TextExtractionOutputKind.PlainText => await ExtractPlainTextResultAsync(),
            TextExtractionOutputKind.Segments => await ExtractSegmentsResultAsync(),
            TextExtractionOutputKind.Letters => await ExtractLettersResultAsync(),
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
    }

    public async Task<TextExtractionResult> ExtractTextAsync(int pageNumber, TextExtractionOptions options)
    {
        using var trace = PerformanceTrace.Measure("TextExtractor.ExtractTextAsync(pageNumber,options)");
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));
        ArgumentNullException.ThrowIfNull(options);

        var page = await _pdf.Objects.PageTree.GetPageAsync(pageNumber);
        if (page is null)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} must reference an existing page.");
        }

        var pageDictionary = (PageDictionary)page.Object;

        return options.OutputKind switch
        {
            TextExtractionOutputKind.PlainText => new TextExtractionResult
            {
                OutputKind = TextExtractionOutputKind.PlainText,
                PlainText = await GetPlainTextForPageAsync(pageDictionary, pageNumber)
            },
            TextExtractionOutputKind.Segments => new TextExtractionResult
            {
                OutputKind = TextExtractionOutputKind.Segments,
                Segments = await GetExtractedTextForPageAsync(pageDictionary, pageNumber)
            },
            TextExtractionOutputKind.Letters => new TextExtractionResult
            {
                OutputKind = TextExtractionOutputKind.Letters,
                Letters = await ExtractPageGlyphRunsAsync(pageDictionary, pageNumber)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
    }

    private async Task<List<GlyphRun>> ExtractPageGlyphRunsAsync(PageDictionary pageDictionary, int pageNumber)
    {
        var contents = await pageDictionary.Contents.GetAsync();
        if (contents is null)
        {
            return [];
        }

        var fontResources = await GetFontResourceSetAsync(pageDictionary);
        using var pageExtractor = new LowLevelTextPageExtractor(fontResources);

        var glyphRuns = new List<GlyphRun>();
        foreach (var content in contents)
        {
            if (content is not StreamObject<StreamDictionary> streamObject)
            {
                continue;
            }

            using var data = await streamObject.GetDecompressedDataAsync();
            await pageExtractor.AppendGlyphRunsAsync(data, pageNumber, glyphRuns);
        }

        return glyphRuns;
    }

    private async Task<List<TextRun>> ExtractPageTextRunsAsync(PageDictionary pageDictionary, int pageNumber)
    {
        var contents = await pageDictionary.Contents.GetAsync();
        if (contents is null)
        {
            return [];
        }

        var fontResources = await GetFontResourceSetAsync(pageDictionary);
        using var pageExtractor = new LowLevelTextPageExtractor(fontResources);

        var textRuns = new List<TextRun>();
        foreach (var content in contents)
        {
            if (content is not StreamObject<StreamDictionary> streamObject)
            {
                continue;
            }

            using var data = await streamObject.GetDecompressedDataAsync();
            await pageExtractor.AppendTextRunsAsync(data, pageNumber, textRuns);
        }

        return textRuns;
    }

    private async Task<ResolvedFontResourceSet> GetFontResourceSetAsync(
        PageDictionary pageDictionary,
        ISet<string>? selectedResourceNames = null,
        bool includeDisplayName = true,
        bool includeMetrics = true)
    {
        using var trace = PerformanceTrace.Measure("TextExtractor.GetFontResourceSetAsync");
        Dictionary? rawResources = await pageDictionary.Resources.GetAsync();
        if (rawResources is null)
        {
            return ResolvedFontResourceSet.Empty;
        }

        var resourceDictionary = Syntax.ContentStreamsAndResources.ResourceDictionary.FromDictionary(rawResources);
        var fontResources = await resourceDictionary.Font.GetAsync();
        if (fontResources is null || fontResources.InnerDictionary.Count == 0)
        {
            return ResolvedFontResourceSet.Empty;
        }

        var cacheKey = CreateFontResourceCacheKey(fontResources, selectedResourceNames, includeDisplayName, includeMetrics);
        if (_fontResourceSetCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var resolved = await ResolvedFontResourceSet.CreateAsync(_pdf, fontResources, _baseProviders, selectedResourceNames, includeDisplayName, includeMetrics);
        _fontResourceSetCache[cacheKey] = resolved;
        return resolved;
    }

    private async Task<IReadOnlyList<ExtractedText>> GetExtractedTextForPageAsync(PageDictionary pageDictionary, int pageNumber)
    {
        var cacheKey = new PageExtractionCacheKey(_pdf.Objects.ChangeVersion, pageNumber);
        if (_pageExtractionCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var pageRuns = await ExtractPageTextRunsAsync(pageDictionary, pageNumber);
        var extracted = BuildExtractedText(pageNumber, pageRuns);
        _pageExtractionCache[cacheKey] = extracted;
        return extracted;
    }

    private async Task<string> GetPlainTextForPageAsync(PageDictionary pageDictionary, int pageNumber)
    {
        using var trace = PerformanceTrace.Measure("TextExtractor.GetPlainTextForPageAsync");
        var cacheKey = new PageExtractionCacheKey(_pdf.Objects.ChangeVersion, pageNumber);
        if (_pagePlainTextCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var contents = await pageDictionary.Contents.GetAsync();
        if (contents is null)
        {
            _pagePlainTextCache[cacheKey] = string.Empty;
            return string.Empty;
        }

        var usedFontResourceNames = new HashSet<string>(StringComparer.Ordinal);
        using (var scanner = new LowLevelTextPageExtractor(ResolvedFontResourceSet.Empty))
        {
            foreach (var content in contents)
            {
                if (content is not StreamObject<StreamDictionary> streamObject)
                {
                    continue;
                }

                using var data = await streamObject.GetDecompressedDataAsync();
                await scanner.AppendUsedFontResourceNamesAsync(data, usedFontResourceNames);
            }
        }

        var fontResources = await GetFontResourceSetAsync(pageDictionary, usedFontResourceNames, includeDisplayName: false, includeMetrics: false);
        using var pageExtractor = new LowLevelTextPageExtractor(fontResources);
        var builder = new StringBuilder();

        foreach (var content in contents)
        {
            if (content is not StreamObject<StreamDictionary> streamObject)
            {
                continue;
            }

            using var data = await streamObject.GetDecompressedDataAsync();
            await pageExtractor.AppendPlainTextAsync(data, builder);
        }

        var plainText = builder.ToString();
        _pagePlainTextCache[cacheKey] = plainText;
        return plainText;
    }

    private async Task<TextExtractionResult> ExtractSegmentsResultAsync()
    {
        var extracted = new List<ExtractedText>();
        var pageNumber = 0;

        await foreach (var page in _pdf.Objects.PageTree.EnumeratePagesAsync())
        {
            pageNumber++;
            extracted.AddRange(await GetExtractedTextForPageAsync((PageDictionary)page.Object, pageNumber));
        }

        return new TextExtractionResult
        {
            OutputKind = TextExtractionOutputKind.Segments,
            Segments = extracted
        };
    }

    private async Task<TextExtractionResult> ExtractPlainTextResultAsync()
    {
        using var trace = PerformanceTrace.Measure("TextExtractor.ExtractPlainTextResultAsync");
        var builder = new StringBuilder();
        var pageNumber = 0;

        await foreach (var page in _pdf.Objects.PageTree.EnumeratePagesAsync())
        {
            pageNumber++;
            var pageText = await GetPlainTextForPageAsync((PageDictionary)page.Object, pageNumber);
            if (pageText.Length == 0)
            {
                continue;
            }

            if (builder.Length != 0)
            {
                builder.Append('\n');
            }

            builder.Append(pageText);
        }

        return new TextExtractionResult
        {
            OutputKind = TextExtractionOutputKind.PlainText,
            PlainText = builder.ToString()
        };
    }

    private async Task<TextExtractionResult> ExtractLettersResultAsync()
    {
        var letters = (await ExtractGlyphRunsAsync()).ToList();
        return new TextExtractionResult
        {
            OutputKind = TextExtractionOutputKind.Letters,
            Letters = letters
        };
    }

    private static string CreateFontResourceCacheKey(
        Dictionary fontResources,
        ISet<string>? selectedResourceNames,
        bool includeDisplayName,
        bool includeMetrics)
    {
        var builder = new StringBuilder();
        builder.Append(includeDisplayName ? "display|" : "plain|");
        builder.Append(includeMetrics ? "metrics|" : "nometrics|");

        if (selectedResourceNames != null && selectedResourceNames.Count != 0)
        {
            builder.Append("selected=");
            foreach (var selected in selectedResourceNames.OrderBy(static x => x, StringComparer.Ordinal))
            {
                builder.Append(selected).Append(',');
            }

            builder.Append('|');
        }

        foreach (var entry in fontResources.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            builder.Append(entry.Key).Append('=');
            if (entry.Value is IndirectObjectReference reference)
            {
                builder.Append(reference.Id.Index).Append(':').Append(reference.Id.GenerationNumber);
            }
            else
            {
                builder.Append("direct:").Append(RuntimeHelpers.GetHashCode(entry.Value));
            }

            builder.Append(';');
        }

        return builder.ToString();
    }

    private static IReadOnlyList<ExtractedText> BuildExtractedText(int pageNumber, IReadOnlyList<TextRun> runs)
    {
        const float yTolerance = 2f;
        const float gapFactor = 0.2f;

        var lineGroups = new List<List<TextRun>>();

        for (var i = 0; i < runs.Count; i++)
        {
            var run = runs[i];
            if (string.IsNullOrEmpty(run.Text))
            {
                continue;
            }

            List<TextRun>? line = null;
            for (var lineIndex = 0; lineIndex < lineGroups.Count; lineIndex++)
            {
                var candidate = lineGroups[lineIndex];
                if (Math.Abs(candidate[0].Y - run.Y) < yTolerance)
                {
                    line = candidate;
                    break;
                }
            }

            if (line is null)
            {
                lineGroups.Add([run]);
            }
            else
            {
                line.Add(run);
            }
        }

        var extracted = new List<ExtractedText>();
        var textBuilder = new StringBuilder();

        for (var lineIndex = 0; lineIndex < lineGroups.Count; lineIndex++)
        {
            var line = lineGroups[lineIndex];
            line.Sort(static (left, right) => left.X.CompareTo(right.X));

            var segmentStart = 0;
            while (segmentStart < line.Count)
            {
                var segmentEnd = segmentStart + 1;
                var hasNonWhitespace = !line[segmentStart].AllWhitespace;

                while (segmentEnd < line.Count)
                {
                    var previous = line[segmentEnd - 1];
                    var current = line[segmentEnd];
                    var gap = current.X - previous.EndX;
                    var threshold = previous.Height * gapFactor;
                    if (gap > threshold)
                    {
                        break;
                    }

                    hasNonWhitespace |= !current.AllWhitespace;
                    segmentEnd++;
                }

                if (hasNonWhitespace)
                {
                    textBuilder.Clear();
                    for (var runIndex = segmentStart; runIndex < segmentEnd; runIndex++)
                    {
                        textBuilder.Append(line[runIndex].Text);
                    }

                    var firstRun = line[segmentStart];
                    extracted.Add(new ExtractedText
                    {
                        PageNumber = pageNumber,
                        Text = textBuilder.ToString(),
                        FontName = firstRun.FontName,
                        FontSize = firstRun.FontSize,
                        X = firstRun.X,
                        Y = firstRun.Y
                    });
                }

                segmentStart = segmentEnd;
            }
        }

        return extracted;
    }

    private readonly record struct PageExtractionCacheKey(long ChangeVersion, int PageNumber);
}
