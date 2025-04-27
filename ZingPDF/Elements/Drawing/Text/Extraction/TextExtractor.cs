using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.IncrementalUpdates;
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
        private readonly IPdf _pdf;
        private readonly IEnumerable<IFontMetricsProvider> _baseProviders;

        public TextExtractor(IPdf pdf, IEnumerable<IFontMetricsProvider>? baseProviders = null)
        {
            _pdf = pdf;
            _baseProviders = baseProviders ?? [new PDFStandardFontMetricsProvider()];
        }

        /// <summary>Extract raw positioned glyphs from all pages.</summary>
        public async Task<IEnumerable<PositionedGlyph>> ExtractGlyphsAsync()
        {
            var pages = await _pdf.PageTree.GetPagesAsync();
            var parser = new ContentStreamParser(_pdf.IndirectObjects);
            var glyphs = new List<PositionedGlyph>();

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
                    var dyn = await resDict.GetFontMetricsProvidersAsync(_pdf.IndirectObjects);
                    providers.AddRange(dyn);
                }

                var fontResources = (await resDict?.Font.GetAsync()) ?? new Dictionary(EmptyPdfEditor.Instance);

                var state = new TextDrawingState(providers, fontResources);

                foreach (var so in contents.Cast<StreamObject<StreamDictionary>>())
                {
                    var data = await so.GetDecompressedDataAsync();
                    var ops = (await parser.ParseAsync(data)).Operations;

                    foreach (ContentStreamOperation op in ops)
                    {
                        glyphs.AddRange(await state.ProcessOperatorAsync(op, pageNum));
                    }
                }
            }
            return glyphs;
        }

        /// <summary>Extract logical text lines by grouping glyphs into strings.</summary>
        public async Task<IEnumerable<ExtractedText>> ExtractTextAsync()
        {
            var glyphs = (await ExtractGlyphsAsync()).ToList();
            const float yTolerance = 2.0f;     // how close in Y to be same line
            const float gapFactor = 0.05f;      // fraction of fontSize to consider a “word gap”

            var output = new List<ExtractedText>();

            foreach (var pageGroup in glyphs.GroupBy(g => g.PageNumber))
            {
                // 1) cluster into lines
                var lineGroups = new List<List<PositionedGlyph>>();
                foreach (var g in pageGroup)
                {
                    var lg = lineGroups
                        .FirstOrDefault(l => Math.Abs(l[0].Y - g.Y) < yTolerance);
                    if (lg == null)
                        lineGroups.Add([g]);
                    else
                        lg.Add(g);
                }

                // 2) for each line, sort and then split on X gaps
                foreach (var line in lineGroups)
                {
                    var sorted = line.OrderBy(g => g.X).ToList();

                    var segment = new List<PositionedGlyph>();

                    for (int i = 0; i < sorted.Count; i++)
                    {
                        if (i > 0)
                        {
                            var prev = sorted[i - 1];
                            var curr = sorted[i];
                            float gap = curr.X - (prev.X + prev.Width);

                            // if the gap is large, flush the current segment
                            if (gap > prev.Width * gapFactor)
                            {
                                if (segment.Count > 0)
                                {
                                    output.Add(new ExtractedText
                                    {
                                        PageNumber = pageGroup.Key,
                                        Text = string.Concat(segment.Select(g2 => g2.Character)),
                                        FontName = segment[0].FontName,
                                        FontSize = segment[0].FontSize,
                                        X = segment[0].X,
                                        Y = segment[0].Y,
                                    });
                                    segment.Clear();
                                }
                            }
                        }

                        segment.Add(sorted[i]);
                    }

                    // flush the last segment
                    if (segment.Count > 0)
                    {
                        output.Add(new ExtractedText
                        {
                            PageNumber = pageGroup.Key,
                            Text = string.Concat(segment.Select(g2 => g2.Character)),
                            FontName = segment[0].FontName,
                            FontSize = segment[0].FontSize,
                            X = segment[0].X,
                            Y = segment[0].Y,
                        });
                    }
                }
            }

            return output;
        }

    }
}
