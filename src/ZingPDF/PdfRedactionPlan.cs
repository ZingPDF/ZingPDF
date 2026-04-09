using System.Numerics;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp.PixelFormats;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Fonts;
using ZingPDF.Graphics;
using ZingPDF.Graphics.Images;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Filters;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text;
using ZingPDF.Extensions;

namespace ZingPDF;

/// <summary>
/// Collects text and region marks and applies them as structural redactions on supported page content.
/// </summary>
public sealed class PdfRedactionPlan
{
    private readonly Pdf _pdf;
    private readonly IParser<ContentStream> _contentStreamParser;
    private readonly List<PdfRedactionMark> _marks = [];

    internal PdfRedactionPlan(Pdf pdf, IParser<ContentStream> contentStreamParser)
    {
        _pdf = pdf ?? throw new ArgumentNullException(nameof(pdf));
        _contentStreamParser = contentStreamParser ?? throw new ArgumentNullException(nameof(contentStreamParser));
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
            Bounds = CloneRectangle(bounds),
            Kind = PdfRedactionKind.Region
        });

        return this;
    }

    /// <summary>
    /// Adds redaction marks for exact text matches found in supported page content streams.
    /// </summary>
    public async Task<int> MarkTextAsync(string text, StringComparison comparison = StringComparison.Ordinal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var count = 0;
        var pageCount = await _pdf.GetPageCountAsync();

        for (var pageNumber = 1; pageNumber <= pageCount; pageNumber++)
        {
            var page = await _pdf.GetPageAsync(pageNumber);
            var analysis = await AnalyzePageAsync(page.Dictionary, pageNumber);

            foreach (var operation in analysis.TextOperations)
            {
                var searchStart = 0;

                while (true)
                {
                    var matchIndex = operation.Text.IndexOf(text, searchStart, comparison);
                    if (matchIndex < 0)
                    {
                        break;
                    }

                    if (TryCreateTextMark(operation, matchIndex, text.Length, PdfRedactionKind.TextMatch, text, out var mark))
                    {
                        _marks.Add(mark);
                        count++;
                    }

                    searchStart = matchIndex + Math.Max(text.Length, 1);
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Adds redaction marks for regular-expression matches found in supported page content streams.
    /// </summary>
    public async Task<int> MarkTextAsync(Regex regex)
    {
        ArgumentNullException.ThrowIfNull(regex);

        var count = 0;
        var pageCount = await _pdf.GetPageCountAsync();

        for (var pageNumber = 1; pageNumber <= pageCount; pageNumber++)
        {
            var page = await _pdf.GetPageAsync(pageNumber);
            var analysis = await AnalyzePageAsync(page.Dictionary, pageNumber);

            foreach (var operation in analysis.TextOperations)
            {
                foreach (Match match in regex.Matches(operation.Text))
                {
                    if (!match.Success || match.Length == 0)
                    {
                        continue;
                    }

                    if (TryCreateTextMark(operation, match.Index, match.Length, PdfRedactionKind.RegexMatch, match.Value, out var mark))
                    {
                        _marks.Add(mark);
                        count++;
                    }
                }
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

        PdfFont? overlayFont = null;
        if (!string.IsNullOrWhiteSpace(options.OverlayText))
        {
            overlayFont = await _pdf.RegisterStandardFontAsync(options.OverlayFontName);
        }

        var pagesTouched = new List<int>();

        foreach (var pageGroup in _marks.GroupBy(static mark => mark.PageNumber).OrderBy(static group => group.Key))
        {
            var pageNumber = pageGroup.Key;
            var page = await _pdf.GetPageAsync(pageNumber);
            var analysis = await AnalyzePageAsync(page.Dictionary, pageNumber);
            var marks = pageGroup.ToList();
            var replacementPlan = BuildReplacementPlan(analysis, marks);
            var rewrittenStreams = analysis.Streams.ToDictionary(
                static stream => stream.StreamIndex,
                static stream => (ContentStream)stream.ContentStream.Clone());

            var contentModified = ApplyReplacementPlanToStreams(rewrittenStreams, replacementPlan);
            contentModified |= await ApplyRegionStreamRedactionsAsync(page, marks, rewrittenStreams);

            if (contentModified)
            {
                await RewritePageContentsAsync(page, analysis.RawContents, rewrittenStreams);
            }

            foreach (var mark in marks)
            {
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

            pagesTouched.Add(pageNumber);
        }

        await _pdf.RemoveHistoryAsync();

        return new PdfRedactionReport
        {
            AppliedMarkCount = _marks.Count,
            PagesTouched = pagesTouched.Distinct().OrderBy(static page => page).ToArray(),
            Warnings =
            [
                "This version removes matched text from supported page content streams and forces rewritten-file save behavior by default.",
                "Region redaction removes supported text operators, page-level vector painting operators, image XObjects, and form XObjects in place.",
                "Region redaction still refuses inline images, shading, and other unsupported painted content."
            ]
        };
    }

    private async Task<PageContentAnalysis> AnalyzePageAsync(PageDictionary pageDictionary, int pageNumber)
    {
        var rawContents = (await pageDictionary.Contents.GetRawValueAsync()) is { } capturedContents
            ? (IPdfObject)capturedContents.Clone()
            : null;
        var contents = await ResolveContentsAsync(rawContents);
        if (contents.Count == 0)
        {
            return new PageContentAnalysis(rawContents, [], [], false);
        }

        var extractedLetters = await _pdf.ExtractTextAsync(pageNumber, new TextExtractionOptions
        {
            OutputKind = TextExtractionOutputKind.Letters
        });
        var glyphRuns = extractedLetters.Letters?.ToList() ?? [];

        var streams = new List<ParsedContentStreamInfo>();
        var textOperationSlots = new List<(int StreamIndex, int OperationIndex, string Operator)>();
        var hasUnsupportedNonTextContent = false;

        var streamIndex = 0;
        foreach (var content in contents)
        {
            if (content is not StreamObject<StreamDictionary> streamObject)
            {
                streamIndex++;
                continue;
            }

            using var data = await streamObject.GetDecompressedDataAsync();
            var parsedStream = await _contentStreamParser.ParseAsync(data, ObjectContext.WithOrigin(ObjectOrigin.ParsedContentStream));
            streams.Add(new ParsedContentStreamInfo(streamIndex, parsedStream));

            for (var operationIndex = 0; operationIndex < parsedStream.Operations.Count; operationIndex++)
            {
                var operation = parsedStream.Operations[operationIndex];
                if (IsUnsupportedNonTextOperator(operation.Operator))
                {
                    hasUnsupportedNonTextContent = true;
                }

                if (!IsTextShowingOperator(operation.Operator))
                {
                    continue;
                }

                textOperationSlots.Add((streamIndex, operationIndex, operation.Operator));
            }

            streamIndex++;
        }

        if (textOperationSlots.Count != glyphRuns.Count)
        {
            throw new InvalidOperationException($"Unable to build a reliable structural redaction map for page {pageNumber}. The current redaction path only proceeds when extracted glyph runs can be mapped back to text-showing operators exactly.");
        }

        var textOperations = new List<TextOperationInfo>(glyphRuns.Count);
        for (var index = 0; index < glyphRuns.Count; index++)
        {
            var glyphRun = glyphRuns[index];
            if (glyphRun.Glyphs.Count == 0)
            {
                continue;
            }

            var slot = textOperationSlots[index];
            textOperations.Add(new TextOperationInfo(
                slot.StreamIndex,
                slot.OperationIndex,
                slot.Operator,
                glyphRun,
                new string(glyphRun.Glyphs.Select(static glyph => glyph.Character).ToArray()),
                GetGlyphRangeBounds(glyphRun, 0, glyphRun.Glyphs.Count)));
        }

        return new PageContentAnalysis(rawContents, streams, textOperations, hasUnsupportedNonTextContent);
    }

    private async Task<IReadOnlyList<StreamObject<StreamDictionary>>> ResolveContentsAsync(IPdfObject? rawContents)
    {
        switch (rawContents)
        {
            case null:
                return [];

            case IndirectObjectReference reference:
                return [await _pdf.Objects.GetAsync<StreamObject<StreamDictionary>>(reference)];

            case StreamObject<StreamDictionary> streamObject:
                return [streamObject];

            case ArrayObject array:
            {
                var streams = new List<StreamObject<StreamDictionary>>();

                foreach (var item in array)
                {
                    var resolved = item is IndirectObjectReference itemReference
                        ? (await _pdf.Objects.GetAsync(itemReference)).Object
                        : item;

                    if (resolved is not StreamObject<StreamDictionary> contentStream)
                    {
                        throw new InvalidOperationException("Page contents must resolve to stream objects.");
                    }

                    streams.Add(contentStream);
                }

                return streams;
            }

            default:
                throw new InvalidOperationException("Page contents must be a stream or an array of streams.");
        }
    }

    private Dictionary<(int StreamIndex, int OperationIndex), List<TextRange>> BuildReplacementPlan(
        PageContentAnalysis analysis,
        IReadOnlyList<PdfRedactionMark> pageMarks)
    {
        var replacementPlan = new Dictionary<(int StreamIndex, int OperationIndex), List<TextRange>>();

        foreach (var mark in pageMarks)
        {
            if (mark.Kind == PdfRedactionKind.Region)
            {
                foreach (var operation in analysis.TextOperations)
                {
                    if (!Intersects(operation.Bounds, mark.Bounds))
                    {
                        continue;
                    }

                    foreach (var range in GetIntersectingRanges(operation.GlyphRun, mark.Bounds))
                    {
                        AddReplacementRange(replacementPlan, operation.StreamIndex, operation.OperationIndex, range);
                    }
                }

                continue;
            }

            if (mark.StreamIndex is null || mark.OperationIndex is null || mark.TextStartIndex is null || mark.TextLength is null)
            {
                throw new InvalidOperationException("Text redaction marks are missing structural target information.");
            }

            AddReplacementRange(
                replacementPlan,
                mark.StreamIndex.Value,
                mark.OperationIndex.Value,
                new TextRange(mark.TextStartIndex.Value, mark.TextLength.Value));
        }

        foreach (var key in replacementPlan.Keys.ToList())
        {
            replacementPlan[key] = MergeRanges(replacementPlan[key]);
        }

        return replacementPlan;
    }

    private static bool ApplyReplacementPlanToStreams(
        IDictionary<int, ContentStream> rewrittenStreams,
        Dictionary<(int StreamIndex, int OperationIndex), List<TextRange>> replacementPlan)
    {
        var modified = false;

        foreach (var replacement in replacementPlan
            .OrderBy(static entry => entry.Key.StreamIndex)
            .ThenBy(static entry => entry.Key.OperationIndex))
        {
            ApplyReplacementToOperation(rewrittenStreams[replacement.Key.StreamIndex].Operations[replacement.Key.OperationIndex], replacement.Value);
            modified = true;
        }

        return modified;
    }

    private async Task RewritePageContentsAsync(Page page, IPdfObject? originalContents, IDictionary<int, ContentStream> rewrittenStreams)
    {
        var existingReferences = GetContentReferences(originalContents);

        if (existingReferences.Count == 0 && originalContents is not null)
        {
            DeleteSupersededContentObjects(originalContents);
        }

        var rewrittenContents = new ShorthandArrayObject(ObjectContext.UserCreated);
        var orderedStreams = rewrittenStreams.OrderBy(static stream => stream.Key).Select(static stream => stream.Value).ToList();

        for (var index = 0; index < orderedStreams.Count; index++)
        {
            var streamObject = await new ContentStreamFactory([orderedStreams[index]])
                .CreateAsync(new StreamDictionary(_pdf, ObjectContext.UserCreated), ObjectContext.UserCreated);

            if (index < existingReferences.Count)
            {
                var reference = existingReferences[index];
                _pdf.Objects.Update(new IndirectObject(reference.Id, streamObject));
                rewrittenContents.Add(new IndirectObjectReference(reference.Id, ObjectContext.UserCreated));
                continue;
            }

            var newObject = await _pdf.Objects.AddAsync(streamObject);
            rewrittenContents.Add(newObject.Reference);
        }

        for (var index = orderedStreams.Count; index < existingReferences.Count; index++)
        {
            _pdf.Objects.Delete(existingReferences[index].Id);
        }

        page.Dictionary.Set(Constants.DictionaryKeys.PageTree.Page.Contents, rewrittenContents);
        _pdf.Objects.Update(page.IndirectObject);
    }

    private void DeleteSupersededContentObjects(IPdfObject? rawContents)
    {
        switch (rawContents)
        {
            case IndirectObjectReference reference:
                _pdf.Objects.Delete(reference.Id);
                break;

            case ArrayObject array:
                foreach (var item in array)
                {
                    if (item is IndirectObjectReference elementReference)
                    {
                        _pdf.Objects.Delete(elementReference.Id);
                    }
                }

                break;
        }
    }

    private static List<IndirectObjectReference> GetContentReferences(IPdfObject? rawContents)
    {
        var references = new List<IndirectObjectReference>();

        switch (rawContents)
        {
            case IndirectObjectReference reference:
                references.Add(reference);
                break;

            case ArrayObject array:
                references.AddRange(array.OfType<IndirectObjectReference>());
                break;
        }

        return references;
    }

    private async Task<bool> ApplyRegionStreamRedactionsAsync(
        Page page,
        IReadOnlyList<PdfRedactionMark> pageMarks,
        IDictionary<int, ContentStream> rewrittenStreams)
    {
        var regionMarks = pageMarks.Where(static mark => mark.Kind == PdfRedactionKind.Region).ToList();
        if (regionMarks.Count == 0)
        {
            return false;
        }

        var rawResources = await page.Dictionary.Resources.GetAsync()
            ?? throw new InvalidOperationException("Page resources are missing.");
        var resources = ResourceDictionary.FromDictionary(rawResources);
        var regions = regionMarks.Select(static mark => mark.Bounds).ToList();
        var mutated = false;

        foreach (var stream in rewrittenStreams.OrderBy(static entry => entry.Key))
        {
            mutated |= await ApplyRegionMutationsToContentStreamAsync(stream.Value, resources, regions, Matrix3x2.Identity);
        }

        if (mutated)
        {
            page.Dictionary.SetResources(resources);
            _pdf.Objects.Update(page.IndirectObject);
        }

        return mutated;
    }

    private async Task<bool> ApplyRegionMutationsToContentStreamAsync(
        ContentStream contentStream,
        ResourceDictionary resources,
        IReadOnlyList<Rectangle> regions,
        Matrix3x2 initialTransform)
    {
        var mutated = false;
        var transform = initialTransform;
        var transformStack = new Stack<Matrix3x2>();
        PathBounds? currentPath = null;

        for (var operationIndex = 0; operationIndex < contentStream.Operations.Count; operationIndex++)
        {
            var operation = contentStream.Operations[operationIndex];

            switch (operation.Operator)
            {
                case ContentStream.Operators.GeneralGraphicsState.q:
                    transformStack.Push(transform);
                    currentPath = currentPath?.Clone();
                    continue;

                case ContentStream.Operators.GeneralGraphicsState.Q:
                    transform = transformStack.Count > 0 ? transformStack.Pop() : initialTransform;
                    currentPath = null;
                    continue;

                case ContentStream.Operators.SpecialGraphicsState.cm:
                    transform = Matrix3x2.Multiply(GetMatrixOperand(operation), transform);
                    continue;

                case ContentStream.Operators.PathConstruction.m:
                    currentPath = new PathBounds();
                    currentPath.Include(operation.GetOperand<Number>(0), operation.GetOperand<Number>(1));
                    continue;

                case ContentStream.Operators.PathConstruction.l:
                    currentPath ??= new PathBounds();
                    currentPath.Include(operation.GetOperand<Number>(0), operation.GetOperand<Number>(1));
                    continue;

                case ContentStream.Operators.PathConstruction.c:
                    currentPath ??= new PathBounds();
                    currentPath.Include(operation.GetOperand<Number>(0), operation.GetOperand<Number>(1));
                    currentPath.Include(operation.GetOperand<Number>(2), operation.GetOperand<Number>(3));
                    currentPath.Include(operation.GetOperand<Number>(4), operation.GetOperand<Number>(5));
                    continue;

                case ContentStream.Operators.PathConstruction.v:
                    currentPath ??= new PathBounds();
                    currentPath.Include(operation.GetOperand<Number>(0), operation.GetOperand<Number>(1));
                    currentPath.Include(operation.GetOperand<Number>(2), operation.GetOperand<Number>(3));
                    continue;

                case ContentStream.Operators.PathConstruction.y:
                    currentPath ??= new PathBounds();
                    currentPath.Include(operation.GetOperand<Number>(0), operation.GetOperand<Number>(1));
                    currentPath.Include(operation.GetOperand<Number>(2), operation.GetOperand<Number>(3));
                    continue;

                case ContentStream.Operators.PathConstruction.re:
                    currentPath ??= new PathBounds();
                    IncludeRectangle(currentPath, operation);
                    continue;

                case ContentStream.Operators.PathConstruction.h:
                    continue;

                case ContentStream.Operators.PathPainting.S:
                case ContentStream.Operators.PathPainting.s:
                case ContentStream.Operators.PathPainting.F:
                case ContentStream.Operators.PathPainting.f:
                case ContentStream.Operators.PathPainting.fStar:
                case ContentStream.Operators.PathPainting.B:
                case ContentStream.Operators.PathPainting.BStar:
                case ContentStream.Operators.PathPainting.b:
                case ContentStream.Operators.PathPainting.bStar:
                    if (currentPath is not null)
                    {
                        var pathBounds = TransformBounds(currentPath.ToRectangle(), transform);
                        if (regions.Any(region => Intersects(pathBounds, region)))
                        {
                            contentStream.Operations[operationIndex] = new ContentStreamOperation
                            {
                                Operator = ContentStream.Operators.PathPainting.n,
                                Operands = null
                            };
                            mutated = true;
                        }
                    }

                    currentPath = null;
                    continue;

                case ContentStream.Operators.PathPainting.n:
                    currentPath = null;
                    continue;

                case ContentStream.Operators.XObjects.Do:
                    mutated |= await RedactInvokedXObjectAsync(operation, resources, regions, transform);
                    currentPath = null;
                    continue;

                case ContentStream.Operators.InlineImages.BI:
                case ContentStream.Operators.InlineImages.ID:
                case ContentStream.Operators.InlineImages.EI:
                case ContentStream.Operators.ShadingPatterns.sh:
                    throw new InvalidOperationException("Region redaction does not yet support inline images or shading content.");
            }
        }

        return mutated;
    }

    private async Task<bool> RedactInvokedXObjectAsync(
        ContentStreamOperation operation,
        ResourceDictionary resources,
        IReadOnlyList<Rectangle> regions,
        Matrix3x2 transform)
    {
        var resourceName = operation.GetOperand<Name>(0).Value;
        var xObjectDictionary = await resources.XObject.GetAsync();
        if (xObjectDictionary is null || !xObjectDictionary.ContainsKey(resourceName))
        {
            return false;
        }

        var value = xObjectDictionary.GetAs<IPdfObject>(resourceName);
        if (value is null)
        {
            return false;
        }

        var resolved = value is IndirectObjectReference reference
            ? (await _pdf.Objects.GetAsync(reference)).Object
            : value;

        if (resolved is StreamObject<ImageDictionary> imageStream)
        {
            var imageBounds = TransformBounds(Rectangle.FromDimensions(1, 1), transform);
            if (!regions.Any(region => Intersects(imageBounds, region)))
            {
                return false;
            }

            var redactedImage = await CreateRedactedImageAsync(imageStream, regions, transform);
            if (value is IndirectObjectReference imageReference)
            {
                _pdf.Objects.Update(new IndirectObject(imageReference.Id, redactedImage));
            }
            else
            {
                xObjectDictionary.Set(resourceName, redactedImage);
                resources.Set(Constants.DictionaryKeys.Resource.XObject, xObjectDictionary);
            }

            return true;
        }

        if (resolved is StreamObject<ZingPDF.Graphics.FormXObjects.Type1FormDictionary> formStream)
        {
            var formMatrix = await GetFormMatrixAsync(formStream.Dictionary);
            var formTransform = Matrix3x2.Multiply(formMatrix, transform);
            var formBounds = TransformBounds(await formStream.Dictionary.BBox.GetAsync(), formTransform);

            if (regions.Any(region => Intersects(formBounds, region)))
            {
                var rewrittenForm = await RedactFormXObjectAsync(formStream, value as IndirectObjectReference, resources, regions, formTransform);
                if (rewrittenForm is not null && value is not IndirectObjectReference)
                {
                    xObjectDictionary.Set(resourceName, rewrittenForm);
                    resources.Set(Constants.DictionaryKeys.Resource.XObject, xObjectDictionary);
                }

                return rewrittenForm is not null;
            }
        }

        return false;
    }

    private async Task<StreamObject<ZingPDF.Graphics.FormXObjects.Type1FormDictionary>?> RedactFormXObjectAsync(
        StreamObject<ZingPDF.Graphics.FormXObjects.Type1FormDictionary> formStream,
        IndirectObjectReference? formReference,
        ResourceDictionary parentResources,
        IReadOnlyList<Rectangle> pageRegions,
        Matrix3x2 formTransform)
    {
        if (!Matrix3x2.Invert(formTransform, out var inverseTransform))
        {
            throw new InvalidOperationException("Region redaction encountered a non-invertible form XObject transform.");
        }

        var localRegions = pageRegions
            .Select(region => TransformBounds(region, inverseTransform))
            .Where(static region => region.Width > 0 && region.Height > 0)
            .ToList();

        if (localRegions.Count == 0)
        {
            return null;
        }

        using var formData = await formStream.GetDecompressedDataAsync();
        var formContent = await _contentStreamParser.ParseAsync(formData, ObjectContext.WithOrigin(ObjectOrigin.ParsedContentStream));
        var formResources = await GetFormResourcesAsync(formStream.Dictionary) ?? parentResources;

        var mutated = await ApplyRegionMutationsToContentStreamAsync(formContent, formResources, localRegions, Matrix3x2.Identity);
        if (!mutated)
        {
            return null;
        }

        var rewrittenForm = await new ContentStreamFactory([formContent])
            .CreateAsync(CloneFormDictionary(formStream.Dictionary), ObjectContext.UserCreated);

        if (formReference is not null)
        {
            _pdf.Objects.Update(new IndirectObject(formReference.Id, rewrittenForm));
        }

        return rewrittenForm;
    }

    private async Task<StreamObject<ImageDictionary>> CreateRedactedImageAsync(
        StreamObject<ImageDictionary> imageStream,
        IReadOnlyList<Rectangle> regions,
        Matrix3x2 transform)
    {
        using var image = await LoadEditableImageAsync(imageStream);
        var width = image.Width;
        var height = image.Height;

        foreach (var region in regions)
        {
            var localRegion = ProjectRegionToImageSpace(region, transform, width, height);
            if (localRegion.Left >= localRegion.Right || localRegion.Top >= localRegion.Bottom)
            {
                continue;
            }

            image.ProcessPixelRows(accessor =>
            {
                for (var y = localRegion.Top; y < localRegion.Bottom; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (var x = localRegion.Left; x < localRegion.Right; x++)
                    {
                        row[x] = new Rgba32(0, 0, 0, 255);
                    }
                }
            });
        }

        var rgbBytes = new byte[width * height * 3];
        var offset = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    rgbBytes[offset++] = pixel.R;
                    rgbBytes[offset++] = pixel.G;
                    rgbBytes[offset++] = pixel.B;
                }
            }
        });

        using var rawData = new MemoryStream(rgbBytes, writable: false);
        var compressedData = new FlateDecodeFilter(null).Encode(rawData);
        var dictionary = new ImageDictionary(
            _pdf,
            ObjectContext.UserCreated,
            width,
            height,
            ColorSpace.DeviceRGB.ToString(),
            8,
            new ShorthandArrayObject([(Name)Constants.Filters.Flate], ObjectContext.UserCreated),
            null);

        return new StreamObject<ImageDictionary>(compressedData, dictionary, ObjectContext.UserCreated);
    }

    private static async Task<SixLabors.ImageSharp.Image<Rgba32>> LoadEditableImageAsync(StreamObject<ImageDictionary> imageStream)
    {
        var filters = await imageStream.Dictionary.Filter.GetAsync();
        var firstFilterName = filters?.OfType<Name>().Select(static filter => filter.Value).FirstOrDefault();

        if (string.Equals(firstFilterName, Constants.Filters.DCT, StringComparison.Ordinal)
            || string.Equals(firstFilterName, Constants.Filters.JPX, StringComparison.Ordinal))
        {
            imageStream.Data.Position = 0;
            return await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(imageStream.Data);
        }

        if (firstFilterName is null || string.Equals(firstFilterName, Constants.Filters.Flate, StringComparison.Ordinal))
        {
            var width = (int)await imageStream.Dictionary.Width.GetAsync();
            var height = (int)await imageStream.Dictionary.Height.GetAsync();
            var colorSpaceName = (await imageStream.Dictionary.ColorSpace.GetAsync() as Name)?.Value;
            var bitsPerComponent = await imageStream.Dictionary.BitsPerComponent.GetAsync();

            if ((int?)bitsPerComponent != 8)
            {
                throw new InvalidOperationException("Region redaction only supports 8-bit image XObjects.");
            }

            await using var decoded = await imageStream.GetDecompressedDataAsync();
            using var decodedCopy = new MemoryStream();
            await decoded.CopyToAsync(decodedCopy);
            var rawBytes = decodedCopy.ToArray();

            if (string.Equals(colorSpaceName, ColorSpace.DeviceRGB.ToString(), StringComparison.Ordinal))
            {
                return SixLabors.ImageSharp.Image.LoadPixelData<Rgb24>(rawBytes, width, height).CloneAs<Rgba32>();
            }

            if (string.Equals(colorSpaceName, ColorSpace.DeviceGray.ToString(), StringComparison.Ordinal))
            {
                return SixLabors.ImageSharp.Image.LoadPixelData<L8>(rawBytes, width, height).CloneAs<Rgba32>();
            }
        }

        throw new InvalidOperationException("Region redaction encountered an unsupported image XObject encoding.");
    }

    private static PixelRectangle ProjectRegionToImageSpace(Rectangle region, Matrix3x2 transform, int width, int height)
    {
        if (!Matrix3x2.Invert(transform, out var inverse))
        {
            throw new InvalidOperationException("Region redaction encountered a non-invertible image transform.");
        }

        var projected = TransformCorners(region, inverse);
        var minX = Clamp(projected.Min(static point => point.X), 0, 1);
        var minY = Clamp(projected.Min(static point => point.Y), 0, 1);
        var maxX = Clamp(projected.Max(static point => point.X), 0, 1);
        var maxY = Clamp(projected.Max(static point => point.Y), 0, 1);

        var left = Clamp((int)Math.Floor(minX * width), 0, width);
        var right = Clamp((int)Math.Ceiling(maxX * width), 0, width);
        var bottom = Clamp((int)Math.Floor(minY * height), 0, height);
        var top = Clamp((int)Math.Ceiling(maxY * height), 0, height);

        return new PixelRectangle(left, Clamp(height - top, 0, height), right, Clamp(height - bottom, 0, height));
    }

    private static void ApplyReplacementToOperation(ContentStreamOperation operation, IReadOnlyList<TextRange> ranges)
    {
        if (ranges.Count == 0)
        {
            return;
        }

        switch (operation.Operator)
        {
            case "Tj":
            case "'":
                MaskStringOperand(operation, 0, ranges);
                return;

            case "\"":
                MaskStringOperand(operation, 2, ranges);
                return;

            case "TJ":
                MaskTextArrayOperand(operation, ranges);
                return;

            default:
                throw new InvalidOperationException($"Unsupported text-showing operator for redaction: {operation.Operator}");
        }
    }

    private static void MaskStringOperand(ContentStreamOperation operation, int operandIndex, IReadOnlyList<TextRange> ranges)
    {
        var original = operation.GetOperand<PdfString>(operandIndex).AsText();
        var decoded = original.DecodeText();
        var masked = MaskText(decoded, ranges);
        operation.Operands![operandIndex] = CreateMaskedPdfString(masked, original);
    }

    private static void MaskTextArrayOperand(ContentStreamOperation operation, IReadOnlyList<TextRange> ranges)
    {
        var textArray = operation.GetOperand<ArrayObject>(0);
        var textIndex = 0;

        for (var elementIndex = 0; elementIndex < textArray.Count(); elementIndex++)
        {
            if (textArray[elementIndex] is not PdfString pdfString)
            {
                continue;
            }

            var original = pdfString.AsText();
            var decoded = original.DecodeText();
            if (decoded.Length == 0)
            {
                continue;
            }

            var localRanges = ProjectRanges(ranges, textIndex, decoded.Length);
            if (localRanges.Count != 0)
            {
                textArray[elementIndex] = CreateMaskedPdfString(MaskText(decoded, localRanges), original);
            }

            textIndex += decoded.Length;
        }
    }

    private static PdfString CreateMaskedPdfString(string maskedText, PdfString original)
    {
        var originalText = original.AsText();
        return originalText.TextEncoding is PdfTextEncoding encoding
            ? PdfString.FromText(maskedText, encoding, originalText.Syntax, ObjectContext.UserCreated)
            : PdfString.FromTextAuto(maskedText, ObjectContext.UserCreated, syntax: originalText.Syntax);
    }

    private static string MaskText(string text, IReadOnlyList<TextRange> ranges)
    {
        var chars = text.ToCharArray();

        foreach (var range in ranges)
        {
            var end = Math.Min(chars.Length, range.Start + range.Length);
            for (var index = Math.Max(0, range.Start); index < end; index++)
            {
                if (!char.IsWhiteSpace(chars[index]))
                {
                    chars[index] = ' ';
                }
            }
        }

        return new string(chars);
    }

    private static List<TextRange> ProjectRanges(IReadOnlyList<TextRange> ranges, int segmentStart, int segmentLength)
    {
        var projected = new List<TextRange>();
        var segmentEnd = segmentStart + segmentLength;

        foreach (var range in ranges)
        {
            var overlapStart = Math.Max(segmentStart, range.Start);
            var overlapEnd = Math.Min(segmentEnd, range.Start + range.Length);

            if (overlapStart >= overlapEnd)
            {
                continue;
            }

            projected.Add(new TextRange(overlapStart - segmentStart, overlapEnd - overlapStart));
        }

        return projected;
    }

    private static void AddReplacementRange(
        IDictionary<(int StreamIndex, int OperationIndex), List<TextRange>> replacementPlan,
        int streamIndex,
        int operationIndex,
        TextRange range)
    {
        if (!replacementPlan.TryGetValue((streamIndex, operationIndex), out var ranges))
        {
            ranges = [];
            replacementPlan[(streamIndex, operationIndex)] = ranges;
        }

        ranges.Add(range);
    }

    private static List<TextRange> MergeRanges(IReadOnlyList<TextRange> ranges)
    {
        if (ranges.Count == 0)
        {
            return [];
        }

        var ordered = ranges.OrderBy(static range => range.Start).ThenBy(static range => range.Length).ToList();
        var merged = new List<TextRange> { ordered[0] };

        for (var index = 1; index < ordered.Count; index++)
        {
            var current = ordered[index];
            var last = merged[^1];
            var lastEnd = last.Start + last.Length;
            var currentEnd = current.Start + current.Length;

            if (current.Start <= lastEnd)
            {
                merged[^1] = new TextRange(last.Start, Math.Max(lastEnd, currentEnd) - last.Start);
            }
            else
            {
                merged.Add(current);
            }
        }

        return merged;
    }

    private static IEnumerable<TextRange> GetIntersectingRanges(GlyphRun run, Rectangle region)
    {
        var ranges = new List<TextRange>();
        var currentStart = -1;

        for (var index = 0; index < run.Glyphs.Count; index++)
        {
            var glyph = run.Glyphs[index];
            var glyphBounds = Rectangle.FromCoordinates(
                new Coordinate(glyph.X, glyph.Y),
                new Coordinate(glyph.X + glyph.Width, glyph.Y + glyph.Height));

            if (Intersects(glyphBounds, region))
            {
                if (currentStart < 0)
                {
                    currentStart = index;
                }

                continue;
            }

            if (currentStart >= 0)
            {
                ranges.Add(new TextRange(currentStart, index - currentStart));
                currentStart = -1;
            }
        }

        if (currentStart >= 0)
        {
            ranges.Add(new TextRange(currentStart, run.Glyphs.Count - currentStart));
        }

        return ranges;
    }

    private static List<TextRange> GetIntersectingRanges(GlyphRun run, Matrix3x2 transform, IReadOnlyList<Rectangle> regions)
    {
        var ranges = new List<TextRange>();
        var currentStart = -1;

        for (var index = 0; index < run.Glyphs.Count; index++)
        {
            var glyph = run.Glyphs[index];
            var glyphBounds = Rectangle.FromCoordinates(
                new Coordinate(glyph.X, glyph.Y),
                new Coordinate(glyph.X + glyph.Width, glyph.Y + glyph.Height));
            var transformedBounds = TransformBounds(glyphBounds, transform);
            var intersects = regions.Any(region => Intersects(transformedBounds, region));

            if (intersects)
            {
                if (currentStart < 0)
                {
                    currentStart = index;
                }

                continue;
            }

            if (currentStart >= 0)
            {
                ranges.Add(new TextRange(currentStart, index - currentStart));
                currentStart = -1;
            }
        }

        if (currentStart >= 0)
        {
            ranges.Add(new TextRange(currentStart, run.Glyphs.Count - currentStart));
        }

        return ranges;
    }

    private static bool TryCreateTextMark(
        TextOperationInfo operation,
        int startIndex,
        int length,
        PdfRedactionKind kind,
        string sourceText,
        out PdfRedactionMark mark)
    {
        mark = default!;

        if (length <= 0 || startIndex < 0 || startIndex + length > operation.GlyphRun.Glyphs.Count)
        {
            return false;
        }

        mark = new PdfRedactionMark
        {
            PageNumber = operation.GlyphRun.PageNumber,
            Bounds = GetGlyphRangeBounds(operation.GlyphRun, startIndex, length),
            Kind = kind,
            SourceText = sourceText,
            StreamIndex = operation.StreamIndex,
            OperationIndex = operation.OperationIndex,
            TextStartIndex = startIndex,
            TextLength = length
        };

        return true;
    }

    private static Rectangle GetGlyphRangeBounds(GlyphRun run, int startIndex, int length)
    {
        var glyphs = run.Glyphs.Skip(startIndex).Take(length).ToArray();
        var minX = glyphs.Min(static glyph => glyph.X);
        var minY = glyphs.Min(static glyph => glyph.Y);
        var maxX = glyphs.Max(static glyph => glyph.X + glyph.Width);
        var maxY = glyphs.Max(static glyph => glyph.Y + glyph.Height);

        return Rectangle.FromCoordinates(
            new Coordinate(minX, minY),
            new Coordinate(maxX, maxY));
    }

    private static bool Intersects(Rectangle left, Rectangle right)
        => left.LowerLeft.X < right.UpperRight.X
            && left.UpperRight.X > right.LowerLeft.X
            && left.LowerLeft.Y < right.UpperRight.Y
            && left.UpperRight.Y > right.LowerLeft.Y;

    private static Rectangle TransformBounds(Rectangle bounds, Matrix3x2 transform)
    {
        var points = TransformCorners(bounds, transform);
        return Rectangle.FromCoordinates(
            new Coordinate(points.Min(static point => point.X), points.Min(static point => point.Y)),
            new Coordinate(points.Max(static point => point.X), points.Max(static point => point.Y)));
    }

    private static Vector2[] TransformCorners(Rectangle bounds, Matrix3x2 transform)
    {
        return
        [
            Vector2.Transform(new Vector2((float)bounds.LowerLeft.X, (float)bounds.LowerLeft.Y), transform),
            Vector2.Transform(new Vector2((float)bounds.UpperRight.X, (float)bounds.LowerLeft.Y), transform),
            Vector2.Transform(new Vector2((float)bounds.UpperRight.X, (float)bounds.UpperRight.Y), transform),
            Vector2.Transform(new Vector2((float)bounds.LowerLeft.X, (float)bounds.UpperRight.Y), transform)
        ];
    }

    private static Matrix3x2 GetMatrixOperand(ContentStreamOperation operation)
        => new(
            operation.GetOperand<Number>(0),
            operation.GetOperand<Number>(1),
            operation.GetOperand<Number>(2),
            operation.GetOperand<Number>(3),
            operation.GetOperand<Number>(4),
            operation.GetOperand<Number>(5));

    private static void IncludeRectangle(PathBounds bounds, ContentStreamOperation operation)
    {
        var x = (double)operation.GetOperand<Number>(0);
        var y = (double)operation.GetOperand<Number>(1);
        var width = (double)operation.GetOperand<Number>(2);
        var height = (double)operation.GetOperand<Number>(3);

        bounds.Include(x, y);
        bounds.Include(x + width, y + height);
    }

    private static async Task<Matrix3x2> GetFormMatrixAsync(ZingPDF.Graphics.FormXObjects.Type1FormDictionary formDictionary)
    {
        var matrix = await formDictionary.Matrix.GetAsync();
        if (matrix is null || matrix.Count() != 6)
        {
            return Matrix3x2.Identity;
        }

        var values = matrix.Cast<Number>().ToArray();
        return new Matrix3x2(values[0], values[1], values[2], values[3], values[4], values[5]);
    }

    private ZingPDF.Graphics.FormXObjects.Type1FormDictionary CloneFormDictionary(ZingPDF.Graphics.FormXObjects.Type1FormDictionary dictionary)
    {
        var clone = dictionary.ToDictionary(
            static entry => entry.Key,
            static entry => (IPdfObject)entry.Value.Clone());

        return ZingPDF.Graphics.FormXObjects.Type1FormDictionary.FromDictionary(clone, _pdf, ObjectContext.UserCreated);
    }

    private static async Task<ResourceDictionary?> GetFormResourcesAsync(ZingPDF.Graphics.FormXObjects.Type1FormDictionary formDictionary)
    {
        var rawResources = await formDictionary.Resources.GetRawValueAsync();
        return rawResources switch
        {
            null => null,
            ResourceDictionary resourceDictionary => resourceDictionary,
            Dictionary dictionary => ResourceDictionary.FromDictionary(dictionary),
            _ => throw new InvalidOperationException("Form resources must be a resource dictionary.")
        };
    }

    private static bool IsUnsupportedNonTextOperator(string @operator)
        => @operator is
            ContentStream.Operators.ShadingPatterns.sh
            or ContentStream.Operators.InlineImages.BI
            or ContentStream.Operators.InlineImages.ID
            or ContentStream.Operators.InlineImages.EI
            or ContentStream.Operators.XObjects.Do;

    private static int Clamp(int value, int min, int max) => Math.Min(max, Math.Max(min, value));
    private static float Clamp(float value, float min, float max) => MathF.Min(max, MathF.Max(min, value));

    private static bool IsTextShowingOperator(string @operator)
        => @operator == "Tj"
            || @operator == "TJ"
            || @operator == "'"
            || @operator == "\"";

    private static Rectangle CloneRectangle(Rectangle bounds)
        => Rectangle.FromCoordinates(
            new Coordinate(bounds.LowerLeft.X, bounds.LowerLeft.Y),
            new Coordinate(bounds.UpperRight.X, bounds.UpperRight.Y));

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

    private sealed record PageContentAnalysis(
        IPdfObject? RawContents,
        IReadOnlyList<ParsedContentStreamInfo> Streams,
        IReadOnlyList<TextOperationInfo> TextOperations,
        bool HasUnsupportedNonTextContent);

    private sealed record ParsedContentStreamInfo(int StreamIndex, ContentStream ContentStream);

    private sealed record TextOperationInfo(
        int StreamIndex,
        int OperationIndex,
        string Operator,
        GlyphRun GlyphRun,
        string Text,
        Rectangle Bounds);

    private readonly record struct TextRange(int Start, int Length);

    private sealed class PathBounds
    {
        private double? _minX;
        private double? _minY;
        private double? _maxX;
        private double? _maxY;

        public void Include(double x, double y)
        {
            _minX = _minX is null ? x : Math.Min(_minX.Value, x);
            _minY = _minY is null ? y : Math.Min(_minY.Value, y);
            _maxX = _maxX is null ? x : Math.Max(_maxX.Value, x);
            _maxY = _maxY is null ? y : Math.Max(_maxY.Value, y);
        }

        public Rectangle ToRectangle()
            => Rectangle.FromCoordinates(
                new Coordinate(_minX ?? 0, _minY ?? 0),
                new Coordinate(_maxX ?? 0, _maxY ?? 0));

        public PathBounds Clone()
        {
            return new PathBounds
            {
                _minX = _minX,
                _minY = _minY,
                _maxX = _maxX,
                _maxY = _maxY
            };
        }
    }

    private readonly record struct PixelRectangle(int Left, int Top, int Right, int Bottom);
}
