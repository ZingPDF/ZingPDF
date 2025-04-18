using System.Numerics;
using System.Text;
using ZingPDF.Elements.Drawing.Text.Extraction.CmapParsing;
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
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text.SimpleFonts;

namespace ZingPDF.Elements.Drawing.Text.Extraction
{
    /// <summary>
    /// Encapsulates PDF text‐drawing state and turns text operators into positioned glyphs.
    /// </summary>
    public class TextDrawingState
    {
        private readonly IEnumerable<IFontMetricsProvider> _fontMetricsProviders;
        private readonly Func<string, Task<CMap?>> _cmapResolver;

        public Matrix3x2 TextMatrix { get; private set; } = Matrix3x2.Identity;
        public Matrix3x2 TextLineMatrix { get; private set; } = Matrix3x2.Identity;
        public string? FontResourceName { get; private set; }
        public float FontSize { get; private set; }
        public float CharSpacing { get; private set; } = 0f;
        public float WordSpacing { get; private set; } = 0f;
        public float HorizontalScaling { get; private set; } = 100f;
        public float TextRise { get; private set; } = 0f;
        public CMap? ToUnicodeCMap { get; private set; }

        public TextDrawingState(IEnumerable<IFontMetricsProvider> fontMetricsProviders, Func<string, Task<CMap?>> cmapResolver)
        {
            _fontMetricsProviders = fontMetricsProviders;
            _cmapResolver = cmapResolver;
        }

        /// <summary>Process a content‐stream operator and emit any glyphs drawn.</summary>
        public async Task<IEnumerable<PositionedGlyph>> ProcessOperatorAsync(ContentStreamOperation op, int pageNumber)
        {
            await ApplyOperatorAsync(op);

            if (ContentStream.Operators.TextShowing.Tj.Equals(op.Operator))
            {
                return HandleTj(op, pageNumber);
            }

            if (ContentStream.Operators.TextShowing.TJ.Equals(op.Operator))
            {
                return HandleTJ(op, pageNumber);
            }

            return [];
        }

        private async Task ApplyOperatorAsync(ContentStreamOperation op)
        {
            switch (op.Operator)
            {
                case "Tf":
                    FontResourceName = ((Name)op.Operands[0]).Value;
                    FontSize = (Number)op.Operands[1];
                    ToUnicodeCMap = await _cmapResolver(FontResourceName);
                    TextMatrix = TextLineMatrix;
                    break;
                case "Tc": CharSpacing = (Number)op.Operands[0]; break;
                case "Tw": WordSpacing = (Number)op.Operands[0]; break;
                case "Tz": HorizontalScaling = (Number)op.Operands[0]; break;
                case "Ts": TextRise = (Number)op.Operands[0]; break;
                case "Td":
                    {
                        float tx = (Number)op.Operands[0];
                        float ty = (Number)op.Operands[1];
                        MoveTextPosition(tx, ty);
                    }
                    break;
                case "Tm":
                    {
                        var nums = op.Operands.Cast<Number>().ToArray();
                        var m = new Matrix3x2(nums[0], nums[1], nums[2], nums[3], nums[4], nums[5]);
                        TextMatrix = m; TextLineMatrix = m;
                    }
                    break;
                case "T*": MoveTextPosition(0, -FontSize); break;
            }
        }

        private IEnumerable<PositionedGlyph> HandleTj(ContentStreamOperation op, int pageNumber)
        {
            string unicode = (LiteralString)op.Operands[0];
            var test = MapCharacterCode(Encoding.Unicode.GetBytes(unicode));

            char? prev = null;
            foreach (var ch in unicode)
            {
                var (x, y, adv) = CalculateNextCharPosition(ch, prev);
                yield return new PositionedGlyph
                {
                    PageNumber = pageNumber,
                    Character = ch.ToString(),
                    X = x,
                    Y = y,
                    Width = adv,
                    Height = FontSize,
                    FontName = FontResourceName ?? string.Empty,
                    FontSize = FontSize
                };
                prev = ch;
            }
        }

        private IEnumerable<PositionedGlyph> HandleTJ(ContentStreamOperation op, int pageNumber)
        {
            var array = (ArrayObject)op.Operands[0];
            char? prev = null;
            foreach (var elem in array)
            {
                if (elem is LiteralString so)
                {
                    string unicode = so;
                    foreach (var ch in unicode)
                    {
                        var (x, y, adv) = CalculateNextCharPosition(ch, prev);
                        yield return new PositionedGlyph
                        {
                            PageNumber = pageNumber,
                            Character = ch.ToString(),
                            X = x,
                            Y = y,
                            Width = adv,
                            Height = FontSize,
                            FontName = FontResourceName ?? string.Empty,
                            FontSize = FontSize
                        };
                        prev = ch;
                    }
                }
                else if (elem is Number no)
                {
                    ApplyTJAdjustment(no);
                }
            }
        }

        private void ApplyTJAdjustment(double adjust)
        {
            float h = HorizontalScaling / 100f;
            float tx = (float)(-adjust / 1000.0 * FontSize * h);
            TextMatrix = Matrix3x2.CreateTranslation(tx, 0) * TextMatrix;
        }

        /// <summary>
        /// Map a byte sequence to Unicode using ToUnicode, PdfDocEncoding, WinAnsi, or ASCII.
        /// Assumes PdfDocEncodingProvider is registered on startup.
        /// </summary>
        public string MapCharacterCode(byte[] code)
        {
            // 1) ToUnicode CMap
            if (ToUnicodeCMap != null && ToUnicodeCMap.Map(code) is string mapped)
                return mapped;

            // 2) PDFDocEncoding
            try
            {
                return Encoding.GetEncoding("PdfDocEncoding").GetString(code);
            }
            catch
            {
                // 3) UTF8 fallback
                return Encoding.UTF8.GetString(code);
            }
        }

        private void MoveTextPosition(float tx, float ty)
        {
            var t = Matrix3x2.CreateTranslation(tx, ty);
            TextMatrix = t * TextLineMatrix;
            TextLineMatrix = TextMatrix;
        }

        public (float x, float y, float advance) CalculateNextCharPosition(char c, char? prev)
        {
            float h = HorizontalScaling / 100f;
            float adv = 0f;
            if (FontResourceName != null)
            {
                var metrics = _fontMetricsProviders
                    .FirstOrDefault(p => p.IsSupported(FontResourceName))
                    ?.GetFontMetrics(FontResourceName);
                if (metrics != null)
                {
                    metrics.Widths.TryGetValue(c, out var w);
                    w = w == 0 ? metrics.StandardHorizontalWidth ?? 500 : w;
                    adv = (w / 1000f) * FontSize;
                    if (prev.HasValue && metrics.KerningPairs.TryGetValue((prev.Value, c), out var k))
                        adv += (k / 1000f) * FontSize;
                }
            }
            adv += CharSpacing * h;
            if (char.IsWhiteSpace(c)) adv += WordSpacing * h;
            var x = TextMatrix.Translation.X;
            var y = TextMatrix.Translation.Y + TextRise;
            TextMatrix = Matrix3x2.CreateTranslation(adv * h, 0) * TextMatrix;
            return (x, y, adv);
        }
    }

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

                var cmapCache = new Dictionary<string, CMap>();
                var fontResources = (await resDict?.Font.GetAsync()) ?? new Dictionary(EmptyPdfEditor.Instance);

                var state = new TextDrawingState(providers, async fn =>
                {
                    if (!cmapCache.TryGetValue(fn, out var cm))
                    {
                        cm = null;
                        FontDictionary fontDict = await fontResources.Get<FontDictionary>(fn).GetAsync();
                        if (fontDict != null)
                        {
                            StreamObject<StreamDictionary>? toUnicode = await fontDict.ToUnicode.GetAsync();
                            if (toUnicode != null)
                            {
                                cm = CMapParser.Parse(await toUnicode.GetDecompressedDataAsync());
                            }
                        }
                        cmapCache[fn] = cm!;
                    }
                    return cm;
                });

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
            var glyphs = await ExtractGlyphsAsync();

            return [.. glyphs
                .GroupBy(g => g.PageNumber)
                .SelectMany(pageGroup =>
                {
                    // Group glyphs by Y-position (allowing for small variations)
                    var lines = pageGroup
                        .GroupBy(g => Math.Round(g.Y, 1)) // tweak rounding as needed
                        .OrderByDescending(g => g.Key);   // higher Y first (PDF origin is bottom-left)

                    return lines.Select(lineGroup => new ExtractedText
                    {
                        PageNumber = pageGroup.Key,
                        Text = string.Concat(lineGroup.Select(g => g.Character))
                    });
                })];
        }


        ///// <summary>Extract logical text lines by grouping glyphs into strings.</summary>
        //public async Task<IEnumerable<ExtractedText>> ExtractTextAsync()
        //{
        //    var glyphs = (await ExtractGlyphsAsync()).ToList();
        //    const float gapFactor = 0.2f;

        //    var lines = glyphs
        //        .GroupBy(g => g.PageNumber)
        //        .SelectMany(pg => pg
        //            .OrderBy(g => g.Y)           // from top to bottom
        //            .GroupBy(g => MathF.Round(g.Y, 2))
        //            .Select(lg => lg.OrderBy(g => g.X).ToList())
        //        )
        //        .Select(sorted =>
        //        {
        //            var sb = new StringBuilder();
        //            for (int i = 0; i < sorted.Count; i++)
        //            {
        //                if (i > 0)
        //                {
        //                    var prev = sorted[i - 1];
        //                    var curr = sorted[i];
        //                    var gap = curr.X - (prev.X + prev.Width);

        //                    if (gap > prev.FontSize * gapFactor)
        //                    {
        //                        sb.Append(' ');
        //                    }
        //                }
        //                sb.Append(sorted[i].Character);
        //            }
        //            return new ExtractedText { PageNumber = sorted.First().PageNumber, Text = sb.ToString() };
        //        })
        //        .ToList();

        //    return lines;
        //}
    }
}
