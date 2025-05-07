using System.Text;
using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Parsing;
using ZingPDF.Parsing.Parsers;
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
    public class TextExtractor
    {
        private readonly IPdfContext _pdfContext;
        private readonly IEnumerable<IFontMetricsProvider> _baseProviders;

        public TextExtractor(IPdfContext pdfContext, IEnumerable<IFontMetricsProvider>? baseProviders = null)
        {
            _pdfContext = pdfContext;
            _baseProviders = baseProviders ?? [new PDFStandardFontMetricsProvider()];
        }

        /// <summary>
        /// Extracts raw glyph runs (one run per Tj/TJ/'/" operator) in document order.
        /// </summary>
        public async Task<IEnumerable<GlyphRun>> ExtractGlyphRunsAsync()
        {
            var pages = await _pdfContext.Objects.PageTree.GetPagesAsync();

            var glyphRuns = new List<GlyphRun>();

            for (int i = 0; i < pages.Count; i++)
            {
                int pageNum = i + 1;
                var pageDict = (PageDictionary)pages[i].Object;

                ArrayObject? contents = await pageDict.Contents.GetAsync();
                if (contents == null)
                {
                    continue;
                }

                Dictionary? rawRes = await pageDict.Resources.GetAsync();
                var resDict = rawRes != null ? ResourceDictionary.FromDictionary(rawRes) : null;
                var providers = new List<IFontMetricsProvider>(_baseProviders);
                if (resDict != null)
                {
                    var dyn = await resDict.GetFontMetricsProvidersAsync(_pdfContext);
                    providers.AddRange(dyn);
                }

                var fontResources = (await resDict?.Font.GetAsync()) ?? new Dictionary(_pdfContext, ObjectOrigin.None);

                var state = new TextDrawingState(providers, fontResources);
                var context = ParseContext.WithOrigin(ObjectOrigin.ParsedContentStream);

                foreach (var so in contents.Cast<StreamObject<StreamDictionary>>())
                {
                    var data = await so.GetDecompressedDataAsync();
                    var ops = (await _pdfContext.Parser.ContentStreamParser.ParseAsync(data, context)).Operations;

                    foreach (ContentStreamOperation op in ops)
                    {
                        var emittedGlyphs = await state.ProcessOperatorAsync(op, pageNum);
                        if (emittedGlyphs != null)
                        {
                            glyphRuns.Add(emittedGlyphs);
                        }
                    }
                }
            }

            return glyphRuns;
        }

        /// <summary>
        /// Groups glyph runs by page & line, then concatenates into ExtractedText.
        /// </summary>
        public async Task<IEnumerable<ExtractedText>> ExtractTextAsync()
        {
            var runs = (await ExtractGlyphRunsAsync()).ToList();
            const float yTolerance = 2f;
            const float gapFactor = 0.2f;  // threshold relative to glyph height

            var texts = new List<ExtractedText>();

            foreach (var pageGroup in runs.GroupBy(r => r.PageNumber))
            {
                // 1) Cluster runs into lines by Y
                var lineGroups = new List<List<GlyphRun>>();

                foreach (var run in pageGroup)
                {
                    if (!run.Glyphs.Any())
                    {
                        continue;
                    }

                    float y = run.Glyphs[0].Y;

                    // find the first line that is close to this Y
                    var line = lineGroups.FirstOrDefault(l => Math.Abs(l[0].Glyphs[0].Y - y) < yTolerance);
                    if (line == null)
                    {
                        lineGroups.Add([run]);
                    }
                    else
                    {
                        line.Add(run);
                    }
                }

                // 2) For each line, sort by X and split by large gaps
                foreach (var line in lineGroups)
                {
                    var orderedRuns = line.OrderBy(lr => lr.Glyphs[0].X).ToList();
                    var segments = new List<List<GlyphRun>>();
                    var segment = new List<GlyphRun> { orderedRuns.First() };

                    for (int i = 1; i < orderedRuns.Count; i++)
                    {
                        var prevRun = orderedRuns[i - 1];
                        var currRun = orderedRuns[i];
                        var prevGlyphs = prevRun.Glyphs;
                        var lastGlyph = prevGlyphs[prevGlyphs.Count - 1];
                        float prevEnd = lastGlyph.X + lastGlyph.Width;
                        float gap = currRun.Glyphs[0].X - prevEnd;
                        float threshold = lastGlyph.Height * gapFactor;

                        if (gap > threshold)
                        {
                            segments.Add(segment);
                            segment = new List<GlyphRun>();
                        }

                        segment.Add(currRun);
                    }
                    segments.Add(segment);

                    // 3) Build ExtractedText for each segment
                    foreach (var seg in segments)
                    {
                        // skip segments of whitespace
                        if (seg.All(r => r.Glyphs.All(g => char.IsWhiteSpace(g.Character))))
                            continue;

                        var sb = new StringBuilder();
                        foreach (var runSeg in seg)
                        {
                            sb.Append(string.Concat(runSeg.Glyphs.Select(g => g.Character)));
                        }

                        var firstGlyph = seg[0].Glyphs[0];
                        texts.Add(new ExtractedText
                        {
                            PageNumber = pageGroup.Key,
                            Text = sb.ToString(),
                            FontName = firstGlyph.FontName,
                            FontSize = firstGlyph.FontSize,
                            X = firstGlyph.X,
                            Y = firstGlyph.Y
                        });
                    }
                }
            }

            return texts;
        }
    }
}
