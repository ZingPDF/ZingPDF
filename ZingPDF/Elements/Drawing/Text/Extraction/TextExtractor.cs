using System.Text;
using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Elements.Drawing.Text.Extraction
{
    /// <summary>
    /// Orchestrates PDF parsing, font/CMap resolution, and emits text lines or glyphs.
    /// </summary>
    public class TextExtractor : ITextExtractor
    {
        private readonly IPdf _pdf;
        private readonly IParser<ContentStream> _contentStreamParser;
        private readonly IEnumerable<IFontMetricsProvider> _baseProviders;

        public TextExtractor(
            IPdf pdf,
            IParser<ContentStream> contentStreamParser,
            IEnumerable<IFontMetricsProvider>? baseProviders = null)
        {
            _pdf = pdf;
            _contentStreamParser = contentStreamParser;
            _baseProviders = baseProviders ?? [new PDFStandardFontMetricsProvider()];
        }

        /// <summary>
        /// Extracts raw glyph runs (one run per Tj/TJ/'/" operator) in document order.
        /// </summary>
        public async Task<IEnumerable<GlyphRun>> ExtractGlyphRunsAsync()
        {
            var pages = await _pdf.Objects.PageTree.GetPagesAsync();
            var glyphRuns = new List<GlyphRun>();

            for (int i = 0; i < pages.Count; i++)
            {
                int pageNum = i + 1;
                var pageDict = (PageDictionary)pages[i].Object;
                glyphRuns.AddRange(await ExtractPageGlyphRunsAsync(pageDict, pageNum));
            }

            return glyphRuns;
        }

        /// <summary>
        /// Groups glyph runs by page and line, then concatenates them into <see cref="ExtractedText"/> values.
        /// </summary>
        public async Task<IEnumerable<ExtractedText>> ExtractTextAsync()
        {
            var texts = new List<ExtractedText>();
            var pages = await _pdf.Objects.PageTree.GetPagesAsync();

            for (int i = 0; i < pages.Count; i++)
            {
                int pageNum = i + 1;
                var pageDict = (PageDictionary)pages[i].Object;
                var pageRuns = await ExtractPageGlyphRunsAsync(pageDict, pageNum);
                texts.AddRange(BuildExtractedText(pageNum, pageRuns));
            }

            return texts;
        }

        private async Task<List<GlyphRun>> ExtractPageGlyphRunsAsync(PageDictionary pageDict, int pageNum)
        {
            ArrayObject? contents = await pageDict.Contents.GetAsync();
            if (contents == null)
            {
                return [];
            }

            Dictionary? rawRes = await pageDict.Resources.GetAsync();
            var resDict = rawRes != null ? ResourceDictionary.FromDictionary(rawRes) : null;
            var providers = new List<IFontMetricsProvider>(_baseProviders);
            if (resDict != null)
            {
                var dynamicProviders = await resDict.GetFontMetricsProvidersAsync(_pdf.Objects);
                providers.AddRange(dynamicProviders);
            }

            var fontResources = resDict != null
                ? await resDict.Font.GetAsync() ?? new Dictionary(_pdf, ObjectContext.None)
                : new Dictionary(_pdf, ObjectContext.None);

            var state = new TextDrawingState(providers, fontResources);
            var context = ObjectContext.WithOrigin(ObjectOrigin.ParsedContentStream);
            var glyphRuns = new List<GlyphRun>();

            foreach (var streamObject in contents.Cast<StreamObject<StreamDictionary>>())
            {
                using var data = await streamObject.GetDecompressedDataAsync();
                var ops = (await _contentStreamParser.ParseAsync(data, context)).Operations;

                foreach (ContentStreamOperation op in ops)
                {
                    var emittedGlyphs = await state.ProcessOperatorAsync(op, pageNum);
                    if (emittedGlyphs != null)
                    {
                        glyphRuns.Add(emittedGlyphs);
                    }
                }
            }

            return glyphRuns;
        }

        private static IEnumerable<ExtractedText> BuildExtractedText(int pageNumber, IReadOnlyList<GlyphRun> runs)
        {
            const float yTolerance = 2f;
            const float gapFactor = 0.2f;  // threshold relative to glyph height

            var lineGroups = new List<List<GlyphRun>>();

            foreach (var run in runs)
            {
                if (run.Glyphs.Count == 0)
                {
                    continue;
                }

                float y = run.Glyphs[0].Y;
                var line = lineGroups.FirstOrDefault(group => Math.Abs(group[0].Glyphs[0].Y - y) < yTolerance);
                if (line == null)
                {
                    lineGroups.Add([run]);
                }
                else
                {
                    line.Add(run);
                }
            }

            var texts = new List<ExtractedText>();

            foreach (var line in lineGroups)
            {
                var orderedRuns = line.OrderBy(run => run.Glyphs[0].X).ToList();
                if (orderedRuns.Count == 0)
                {
                    continue;
                }

                var segments = new List<List<GlyphRun>>();
                var segment = new List<GlyphRun> { orderedRuns[0] };

                for (int i = 1; i < orderedRuns.Count; i++)
                {
                    var prevRun = orderedRuns[i - 1];
                    var currRun = orderedRuns[i];
                    var lastGlyph = prevRun.Glyphs[prevRun.Glyphs.Count - 1];
                    float prevEnd = lastGlyph.X + lastGlyph.Width;
                    float gap = currRun.Glyphs[0].X - prevEnd;
                    float threshold = lastGlyph.Height * gapFactor;

                    if (gap > threshold)
                    {
                        segments.Add(segment);
                        segment = [];
                    }

                    segment.Add(currRun);
                }

                segments.Add(segment);

                foreach (var runSegment in segments)
                {
                    if (runSegment.All(run => run.Glyphs.All(glyph => char.IsWhiteSpace(glyph.Character))))
                    {
                        continue;
                    }

                    var firstGlyph = runSegment[0].Glyphs[0];
                    texts.Add(new ExtractedText
                    {
                        PageNumber = pageNumber,
                        Text = string.Concat(runSegment.SelectMany(run => run.Glyphs).Select(glyph => glyph.Character)),
                        FontName = firstGlyph.FontName,
                        FontSize = firstGlyph.FontSize,
                        X = firstGlyph.X,
                        Y = firstGlyph.Y
                    });
                }
            }

            return texts;
        }
    }
}
