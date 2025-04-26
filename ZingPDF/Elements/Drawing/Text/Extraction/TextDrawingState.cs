using System.Numerics;
using System.Text;
using ZingPDF.Elements.Drawing.Text.Extraction.CmapParsing;
using ZingPDF.Fonts;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text;
using ZingPDF.Text.CompositeFonts;
using ZingPDF.Text.Encoding;

namespace ZingPDF.Elements.Drawing.Text.Extraction
{
    /// <summary>
    /// Encapsulates PDF text‐drawing state and turns text operators into positioned glyphs.
    /// </summary>
    public class TextDrawingState
    {
        private readonly Dictionary<string, CMap> _cmapCache = [];

        private Encoding? _fontEncoding;  // set when we handle Tf

        private readonly IEnumerable<IFontMetricsProvider> _fontMetricsProviders;
        private readonly Dictionary _fontResourceMap;

        public Matrix3x2 TextMatrix { get; private set; } = Matrix3x2.Identity;
        public Matrix3x2 TextLineMatrix { get; private set; } = Matrix3x2.Identity;
        public string? FontResourceName { get; private set; }
        public float FontSize { get; private set; }
        public FontDictionary? FontDictionary { get; private set; }
        public float CharSpacing { get; private set; } = 0f;
        public float WordSpacing { get; private set; } = 0f;
        public float HorizontalScaling { get; private set; } = 100f;
        public float TextRise { get; private set; } = 0f;
        public float TextLeading { get; private set; } = 0f;
        public CMap? ToUnicodeCMap { get; private set; }

        public TextDrawingState(IEnumerable<IFontMetricsProvider> fontMetricsProviders, Dictionary fontResourceMap)
        {
            _fontMetricsProviders = fontMetricsProviders;
            _fontResourceMap = fontResourceMap;
        }

        /// <summary>Process a content‐stream operator and emit any glyphs drawn.</summary>
        public async Task<IEnumerable<PositionedGlyph>> ProcessOperatorAsync(ContentStreamOperation op, int pageNumber)
        {
            switch (op.Operator)
            {
                case ContentStream.Operators.TextShowing.Tj:
                    return await HandleTjAsync((LiteralString)op.Operands[0], pageNumber);

                case ContentStream.Operators.TextShowing.TJ:
                    return await HandleTJAsync(op, pageNumber);

                case ContentStream.Operators.TextShowing.Apostrophe:
                    MoveTextPosition(0, -TextLeading);
                    return await HandleTjAsync((LiteralString)op.Operands[0], pageNumber);

                case ContentStream.Operators.TextShowing.Quote:
                    WordSpacing = (Number)op.Operands[0];
                    CharSpacing = (Number)op.Operands[1];
                    MoveTextPosition(0, -TextLeading);
                    return await HandleTjAsync((LiteralString)op.Operands[0], pageNumber);

                case ContentStream.Operators.TextState.Tf:
                    FontResourceName = ((Name)op.Operands[0]).Value;
                    FontSize = (Number)op.Operands[1];
                    FontDictionary = await _fontResourceMap.Get<FontDictionary>(FontResourceName).GetAsync();
                    ToUnicodeCMap = await ResolveCMapAsync(FontResourceName);
                    TextMatrix = TextLineMatrix;
                    _fontEncoding = await ResolveFontEncodingAsync(FontResourceName);
                    break;

                case ContentStream.Operators.TextState.Tc:
                    CharSpacing = (Number)op.Operands[0];
                    break;

                case ContentStream.Operators.TextState.Tw:
                    WordSpacing = (Number)op.Operands[0];
                    break;

                case ContentStream.Operators.TextState.Tz:
                    HorizontalScaling = (Number)op.Operands[0];
                    break;

                case ContentStream.Operators.TextState.Ts:
                    TextRise = (Number)op.Operands[0];
                    break;

                case ContentStream.Operators.TextPositioning.Td:
                    float tx = (Number)op.Operands[0];
                    float ty = (Number)op.Operands[1];
                    MoveTextPosition(tx, ty);
                    break;

                case ContentStream.Operators.TextPositioning.Tm:
                    var nums = op.Operands.Cast<Number>().ToArray();
                    var m = new Matrix3x2(nums[0], nums[1], nums[2], nums[3], nums[4], nums[5]);
                    TextMatrix = m; TextLineMatrix = m;
                    break;

                case ContentStream.Operators.TextPositioning.TStar:
                    MoveTextPosition(0, -TextLeading);
                    break;

                case ContentStream.Operators.TextState.TL:
                    TextLeading = (Number)op.Operands[0];
                    break;

                case ContentStream.Operators.TextPositioning.TD:
                    TextLeading = -(Number)op.Operands[1];
                    MoveTextPosition((Number)op.Operands[0], (Number)op.Operands[1]);
                    break;

                case ContentStream.Operators.TextObjects.BT:
                    TextMatrix = Matrix3x2.Identity;
                    TextLineMatrix = Matrix3x2.Identity;
                    break;

                case "ET":
                    // No state change needed for extraction
                    break;
            }

            return [];
        }

        private async Task<IEnumerable<PositionedGlyph>> HandleTjAsync(LiteralString text, int pageNumber)
        {
            var unicode = MapCharacterCode(text.RawBytes);

            char? prev = null;
            List<PositionedGlyph> glyphs = [];
            foreach (var ch in unicode)
            {
                var (x, y, adv) = await CalculateNextCharPositionAsync(ch, prev);
                glyphs.Add(new PositionedGlyph
                {
                    PageNumber = pageNumber,
                    Character = ch.ToString(),
                    X = x,
                    Y = y,
                    Width = adv,
                    Height = FontSize,
                    FontName = FontResourceName ?? string.Empty,
                    FontSize = FontSize
                });
                prev = ch;
            }

            return glyphs;
        }

        private async Task<IEnumerable<PositionedGlyph>> HandleTJAsync(ContentStreamOperation op, int pageNumber)
        {
            var array = (ArrayObject)op.Operands[0];
            char? prev = null;

            List<PositionedGlyph> glyphs = [];

            foreach (var elem in array)
            {
                if (elem is LiteralString so)
                {
                    string unicode = MapCharacterCode(so.RawBytes);

                    foreach (var ch in unicode)
                    {
                        var (x, y, adv) = await CalculateNextCharPositionAsync(ch, prev);
                        glyphs.Add(new PositionedGlyph
                        {
                            PageNumber = pageNumber,
                            Character = ch.ToString(),
                            X = x,
                            Y = y,
                            Width = adv,
                            Height = FontSize,
                            FontName = FontResourceName ?? string.Empty,
                            FontSize = FontSize
                        });
                        prev = ch;
                    }
                }
                else if (elem is Number no)
                {
                    ApplyTJAdjustment(no);
                }
            }

            return glyphs;
        }

        private async Task<CMap?> ResolveCMapAsync(Name fontResourceName)
        {
            if (_cmapCache.TryGetValue(fontResourceName, out CMap? cm))
            {
                return cm;
            }

            // TODO: for Type 0 fonts, the encoding entry gives the name of the cmap.

            StreamObject<StreamDictionary>? toUnicode = await FontDictionary!.ToUnicode.GetAsync();
            if (toUnicode != null)
            {
                cm = CMapParser.Parse(await toUnicode.GetDecompressedDataAsync());

                _cmapCache[fontResourceName] = cm;
            }

            return cm;
        }

        private async Task<Encoding?> ResolveFontEncodingAsync(Name fontResourceName)
        {
            if (FontDictionary is Type0FontDictionary)
            {
                // Type 0 fonts use a CMap
                return null;
            }

            Syntax.Either<Name?, Dictionary?> encoding = await FontDictionary!.Encoding.GetAsync();

            if (encoding.Value == null)
            {
                return null;
            }

            if (encoding.Type1 != null)
            {
                return encoding.Type1.Value switch
                {
                    PDFEncoding.Standard => Encoding.Latin1,
                    PDFEncoding.WinAnsi => Encoding.GetEncoding(1252),
                    PDFEncoding.MacRoman => Encoding.GetEncoding(10000),
                    PDFEncoding.PDFDoc => Encoding.GetEncoding(PDFEncoding.PDFDoc),
                    //PDFEncoding.MacExpert => Encoding.GetEncoding(PDFEncoding.MacExpert), // TODO
                    _ => throw new NotSupportedException($"Font encoding '{encoding.Type1}' is not supported."),
                };
            }

            if (encoding.Type2 != null)
            {
                // TODO: implement encoding dictionary and difference encoding class
                throw new NotImplementedException();
            }

            return null;
        }

        private void ApplyTJAdjustment(double adjust)
        {
            float h = HorizontalScaling / 100f;
            float tx = (float)(-adjust / 1000.0 * FontSize * h);
            TextMatrix = Matrix3x2.CreateTranslation(tx, 0) * TextMatrix;
        }

        public string MapCharacterCode(byte[] code)
        {
            // 1) ToUnicode CMap
            if (ToUnicodeCMap != null && ToUnicodeCMap.Map(code) is string mapped)
                return mapped;

            // 2) Font’s single-byte encoding
            //    (Standard, WinAnsi, MacRoman or PdfDocEncoding)
            return _fontEncoding.GetString(code);
        }

        private void MoveTextPosition(float tx, float ty)
        {
            var t = Matrix3x2.CreateTranslation(tx, ty);
            TextMatrix = t * TextLineMatrix;
            TextLineMatrix = TextMatrix;
        }

        public async Task<(float x, float y, float advance)> CalculateNextCharPositionAsync(char c, char? prev)
        {
            float h = HorizontalScaling / 100f;
            float adv = 0f;
            if (FontDictionary != null)
            {
                var fontName = await FontDictionary.BaseFont.GetAsync();

                var metrics = _fontMetricsProviders
                    .FirstOrDefault(p => p.IsSupported(fontName))
                    ?.GetFontMetrics(fontName);

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
}
