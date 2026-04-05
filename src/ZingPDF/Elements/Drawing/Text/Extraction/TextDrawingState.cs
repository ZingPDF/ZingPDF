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
using ZingPDF.Extensions;

namespace ZingPDF.Elements.Drawing.Text.Extraction
{
    /// <summary>
    /// Encapsulates PDF text‐drawing state and turns text operators into positioned glyphs.
    /// </summary>
    public class TextDrawingState
    {
        private readonly Dictionary<string, CMap> _cmapCache = [];
        private readonly Dictionary<string, ResolvedFontState> _resolvedFontStateCache = [];

        private Encoding? _fontEncoding;  // set when we handle Tf
        private FontMetrics? _currentFontMetrics;
        private string? _currentFontName;

        private readonly IReadOnlyList<IFontMetricsProvider> _fontMetricsProviders;
        private readonly Dictionary _fontResourceMap;

        public TextDrawingState(IEnumerable<IFontMetricsProvider> fontMetricsProviders, Dictionary fontResourceMap)
        {
            _fontMetricsProviders = fontMetricsProviders as IReadOnlyList<IFontMetricsProvider> ?? [.. fontMetricsProviders];
            _fontResourceMap = fontResourceMap;

            FontSize = 12f; // Default font size. This shouldn't be needed, but a malformed content stream may not set it.
        }

        public Matrix3x2 TextMatrix { get; private set; } = Matrix3x2.Identity;
        public Matrix3x2 TextLineMatrix { get; private set; } = Matrix3x2.Identity;
        public Name? FontResourceName { get; private set; }
        public float FontSize { get; private set; }
        public FontDictionary? FontDictionary { get; private set; }
        public float CharSpacing { get; private set; } = 0f;
        public float WordSpacing { get; private set; } = 0f;
        public float HorizontalScaling { get; private set; } = 100f;
        public float TextRise { get; private set; } = 0f;
        public float TextLeading { get; private set; } = 0f;
        public CMap? ToUnicodeCMap { get; private set; }

        /// <summary>
        /// Returns the effective font size on the page by combining
        /// the current PDF font size and the vertical scale factor
        /// from the text‐line matrix (the d component of Tm).
        /// </summary>
        public float EffectiveFontSizeVertical => FontSize * TextLineMatrix.M22;

        /// <summary>
        /// Returns the effective horizontal scaling of the font, in case
        /// you ever need to measure gaps in the X direction.
        /// </summary>
        public float EffectiveFontSizeHorizontal => FontSize * TextLineMatrix.M11;

        /// <summary>Process a content‐stream operator and emit any glyphs drawn.</summary>
        public async Task<GlyphRun?> ProcessOperatorAsync(ContentStreamOperation op, int pageNumber)
        {
            switch (op.Operator)
            {
                case ContentStream.Operators.TextShowing.Tj:
                    return CreateGlyphRunOrNull(pageNumber, HandleTj(op.GetOperand<PdfString>(0), pageNumber));

                case ContentStream.Operators.TextShowing.TJ:
                    return CreateGlyphRunOrNull(pageNumber, HandleTJ(op, pageNumber));

                case ContentStream.Operators.TextShowing.Apostrophe:
                    MoveTextPosition(0, -TextLeading);
                    return CreateGlyphRunOrNull(pageNumber, HandleTj(op.GetOperand<PdfString>(0), pageNumber));

                case ContentStream.Operators.TextShowing.Quote:
                    WordSpacing = op.GetOperand<Number>(0);
                    CharSpacing = op.GetOperand<Number>(1);
                    MoveTextPosition(0, -TextLeading);
                    return CreateGlyphRunOrNull(pageNumber, HandleTj(op.GetOperand<PdfString>(2), pageNumber));

                default:
                    await ApplyOperatorStateAsync(op);
                    break;
            }

            return null;
        }

        internal TextRun? ProcessTextOperator(ContentStreamOperation op, int pageNumber)
        {
            switch (op.Operator)
            {
                case ContentStream.Operators.TextShowing.Tj:
                    return HandleTjTextRun(op.GetOperand<PdfString>(0), pageNumber);

                case ContentStream.Operators.TextShowing.TJ:
                    return HandleTJTextRun(op, pageNumber);

                case ContentStream.Operators.TextShowing.Apostrophe:
                    MoveTextPosition(0, -TextLeading);
                    return HandleTjTextRun(op.GetOperand<PdfString>(0), pageNumber);

                case ContentStream.Operators.TextShowing.Quote:
                    WordSpacing = op.GetOperand<Number>(0);
                    CharSpacing = op.GetOperand<Number>(1);
                    MoveTextPosition(0, -TextLeading);
                    return HandleTjTextRun(op.GetOperand<PdfString>(2), pageNumber);

                default:
                    ApplyOperatorState(op);
                    return null;
            }
        }

        internal async Task WarmFontCacheAsync()
        {
            foreach (var entry in _fontResourceMap)
            {
                await ResolveFontStateAsync(entry.Key);
            }
        }

        internal void BeginTextObject()
        {
            TextMatrix = Matrix3x2.Identity;
            TextLineMatrix = Matrix3x2.Identity;
        }

        internal void EndTextObject()
        {
        }

        internal void SetCharSpacing(float charSpacing) => CharSpacing = charSpacing;
        internal void SetWordSpacing(float wordSpacing) => WordSpacing = wordSpacing;
        internal void SetHorizontalScaling(float horizontalScaling) => HorizontalScaling = horizontalScaling;
        internal void SetTextLeading(float textLeading) => TextLeading = textLeading;
        internal void SetTextRise(float textRise) => TextRise = textRise;
        internal void SetFont(string fontResourceName, float fontSize) => ApplyFont(fontResourceName, fontSize);
        internal void MoveTextPositionAndSetLeading(float tx, float ty)
        {
            TextLeading = -ty;
            MoveTextPosition(tx, ty);
        }
        internal void MoveToStartOfNextLine() => MoveTextPosition(0, -TextLeading);

        internal void SetTextMatrix(float a, float b, float c, float d, float e, float f)
        {
            var matrix = new Matrix3x2(a, b, c, d, e, f);
            TextMatrix = matrix;
            TextLineMatrix = matrix;
        }

        internal string CurrentFontName => GetCurrentFontName();

        internal TextRun? ShowText(byte[] textBytes, int pageNumber)
            => ShowText(textBytes.AsSpan(), pageNumber);

        internal TextRun? ShowText(ReadOnlySpan<byte> textBytes, int pageNumber)
            => CreateTextRunOrNull(MapCharacterCode(textBytes), pageNumber, GetCurrentFontName());

        internal TextRun? ShowTextArray(IReadOnlyList<TextArrayElement> array, int pageNumber)
        {
            var fontName = GetCurrentFontName();
            var builder = new StringBuilder();
            char? prev = null;
            var hasGlyph = false;
            var allWhitespace = true;
            var startX = 0f;
            var startY = 0f;
            var endX = 0f;
            var height = EffectiveFontSizeVertical;

            foreach (var element in array)
            {
                if (element.IsText)
                {
                    var unicode = MapCharacterCode(element.TextBytes!);
                    if (unicode.Length == 0)
                    {
                        continue;
                    }

                    builder.Append(unicode);

                    foreach (var ch in unicode)
                    {
                        var (x, y, adv) = CalculateNextCharPosition(ch, prev);
                        if (!hasGlyph)
                        {
                            startX = x;
                            startY = y;
                            height = EffectiveFontSizeVertical;
                            hasGlyph = true;
                        }

                        endX = x + adv;
                        allWhitespace &= char.IsWhiteSpace(ch);
                        prev = ch;
                    }
                }
                else
                {
                    ApplyTJAdjustment(element.Adjustment);
                }
            }

            return hasGlyph
                ? new TextRun(pageNumber, builder.ToString(), startX, startY, endX, height, fontName, FontSize, allWhitespace)
                : null;
        }

        public string MapCharacterCode(byte[] code)
            => MapCharacterCode(code.AsSpan());

        public string MapCharacterCode(ReadOnlySpan<byte> code)
        {
            if (code.Length == 0)
            {
                return string.Empty;
            }

            // 1) ToUnicode CMap
            if (ToUnicodeCMap != null)
            {
                return DecodeWithCMap(code);
            }

            // 2) Font’s single-byte encoding
            //    (Standard, WinAnsi, MacRoman or PdfDocEncoding)
            return (_fontEncoding ?? Encoding.GetEncoding(PDFEncoding.PDFDoc)).GetString(code);
        }

        public (float x, float y, float deviceAdvance) CalculateNextCharPosition(char c, char? prev)
        {
            // 1) Look up raw glyph width (in glyph units)
            float rawWidth = 0f;
            if (_currentFontMetrics != null)
            {
                _currentFontMetrics.Widths.TryGetValue(c, out var w);
                w = w == 0 ? _currentFontMetrics.StandardHorizontalWidth ?? 500 : w;
                rawWidth = w / 1000f;          // convert to text-space units
                if (prev.HasValue && _currentFontMetrics.KerningPairs.TryGetValue((prev.Value, c), out var k))
                {
                    rawWidth += k / 1000f;     // add kerning
                }
            }

            // 2) Scale by font size
            float textAdvance = rawWidth * FontSize;

            // 3) Add character & word spacing (still text-space)
            float hScale = HorizontalScaling / 100f;
            textAdvance += CharSpacing * hScale;
            if (char.IsWhiteSpace(c))
                textAdvance += WordSpacing * hScale;

            // 4) Compute device-space advance:
            //    matrixScale = TextLineMatrix.M11 (horizontal scale from Tm)
            float matrixScale = TextLineMatrix.M11;
            float deviceAdvance = textAdvance * matrixScale;

            // 5) Grab current position
            float x = TextMatrix.Translation.X;
            float y = TextMatrix.Translation.Y + TextRise;

            // 6) Move text matrix in text-space by (textAdvance * hScale)
            //    so that future text uses the right origin
            float textTranslation = textAdvance * hScale;
            TextMatrix = Matrix3x2
                .CreateTranslation(textTranslation, 0)
                * TextMatrix;

            // 7) Return the on-page advance for grouping, etc.
            return (x, y, deviceAdvance);
        }

        private IEnumerable<PositionedGlyph> HandleTj(PdfString text, int pageNumber)
        {
            var unicode = MapCharacterCode(text.Bytes);
            var fontName = GetCurrentFontName();

            char? prev = null;
            List<PositionedGlyph> glyphs = [];
            foreach (var ch in unicode)
            {
                var (x, y, adv) = CalculateNextCharPosition(ch, prev);
                glyphs.Add(new PositionedGlyph
                {
                    PageNumber = pageNumber,
                    Character = ch,
                    X = x,
                    Y = y,
                    Width = adv,
                    Height = EffectiveFontSizeVertical,
                    FontName = fontName,
                    FontSize = FontSize
                });
                prev = ch;
            }

            return glyphs;
        }

        private IEnumerable<PositionedGlyph> HandleTJ(ContentStreamOperation op, int pageNumber)
        {
            var fontName = GetCurrentFontName();

            var array = op.GetOperand<ArrayObject>(0);
            char? prev = null;

            List<PositionedGlyph> glyphs = [];

            foreach (var elem in array)
            {
                if (elem is PdfString so)
                {
                    string unicode = MapCharacterCode(so.Bytes);

                    foreach (var ch in unicode)
                    {
                        var (x, y, adv) = CalculateNextCharPosition(ch, prev);
                        glyphs.Add(new PositionedGlyph
                        {
                            PageNumber = pageNumber,
                            Character = ch,
                            X = x,
                            Y = y,
                            Width = adv,
                            Height = EffectiveFontSizeVertical,
                            FontName = fontName,
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

        private TextRun? HandleTjTextRun(PdfString text, int pageNumber)
        {
            var unicode = MapCharacterCode(text.Bytes);
            return CreateTextRunOrNull(unicode, pageNumber, GetCurrentFontName());
        }

        private TextRun? HandleTJTextRun(ContentStreamOperation op, int pageNumber)
        {
            var fontName = GetCurrentFontName();
            var array = op.GetOperand<ArrayObject>(0);
            var builder = new StringBuilder();
            char? prev = null;
            var hasGlyph = false;
            var allWhitespace = true;
            var startX = 0f;
            var startY = 0f;
            var endX = 0f;
            var height = EffectiveFontSizeVertical;

            foreach (var elem in array)
            {
                if (elem is PdfString so)
                {
                    var unicode = MapCharacterCode(so.Bytes);
                    if (unicode.Length == 0)
                    {
                        continue;
                    }

                    builder.Append(unicode);

                    foreach (var ch in unicode)
                    {
                        var (x, y, adv) = CalculateNextCharPosition(ch, prev);
                        if (!hasGlyph)
                        {
                            startX = x;
                            startY = y;
                            height = EffectiveFontSizeVertical;
                            hasGlyph = true;
                        }

                        endX = x + adv;
                        allWhitespace &= char.IsWhiteSpace(ch);
                        prev = ch;
                    }
                }
                else if (elem is Number no)
                {
                    ApplyTJAdjustment(no);
                }
            }

            return hasGlyph
                ? new TextRun(pageNumber, builder.ToString(), startX, startY, endX, height, fontName, FontSize, allWhitespace)
                : null;
        }

        private TextRun? CreateTextRunOrNull(string text, int pageNumber, string fontName)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            char? prev = null;
            var hasGlyph = false;
            var allWhitespace = true;
            var startX = 0f;
            var startY = 0f;
            var endX = 0f;
            var height = EffectiveFontSizeVertical;

            foreach (var ch in text)
            {
                var (x, y, adv) = CalculateNextCharPosition(ch, prev);
                if (!hasGlyph)
                {
                    startX = x;
                    startY = y;
                    height = EffectiveFontSizeVertical;
                    hasGlyph = true;
                }

                endX = x + adv;
                allWhitespace &= char.IsWhiteSpace(ch);
                prev = ch;
            }

            return hasGlyph
                ? new TextRun(pageNumber, text, startX, startY, endX, height, fontName, FontSize, allWhitespace)
                : null;
        }

        private async Task<FontMetrics?> ResolveCurrentFontMetricsAsync(FontDictionary fontDictionary)
        {
            var fontName = await fontDictionary.BaseFont.GetAsync()
                ?? throw new InvalidPdfException("The current font is missing a BaseFont name.");

            return _fontMetricsProviders
                .FirstOrDefault(p => p.IsSupported(fontName))
                ?.GetFontMetrics(fontName);
        }

        private static GlyphRun? CreateGlyphRunOrNull(int pageNumber, IEnumerable<PositionedGlyph> glyphs)
        {
            var materializedGlyphs = glyphs as IReadOnlyList<PositionedGlyph> ?? [.. glyphs];
            return materializedGlyphs.Count == 0 ? null : new GlyphRun(pageNumber, materializedGlyphs);
        }

        private string DecodeWithCMap(ReadOnlySpan<byte> code)
        {
            var cmap = ToUnicodeCMap
                ?? throw new InvalidOperationException("Cannot decode with a missing ToUnicode CMap.");

            var builder = new StringBuilder();
            var offset = 0;

            while (offset < code.Length)
            {
                if (cmap.TryReadMatch(code.Slice(offset), out var mapped, out var bytesConsumed))
                {
                    builder.Append(mapped);
                    offset += bytesConsumed;
                    continue;
                }

                if (_fontEncoding != null)
                {
                    builder.Append(_fontEncoding.GetString(code.Slice(offset, 1)));
                    offset += 1;
                    continue;
                }

                builder.Append('\uFFFD');
                offset += cmap.GetFallbackCodeLength(code.Length - offset);
            }

            return builder.ToString();
        }

        private async Task<CMap?> ResolveCMapAsync(string fontResourceName, FontDictionary fontDictionary)
        {
            if (_cmapCache.TryGetValue(fontResourceName, out CMap? cm))
            {
                return cm;
            }

            StreamObject<StreamDictionary>? toUnicode = await fontDictionary.ToUnicode.GetAsync();
            if (toUnicode != null)
            {
                cm = CMapParser.Parse(await toUnicode.GetDecompressedDataAsync());

                // Only accept if the CMap is non-null and has mappings
                if (cm != null && cm.MappingCount != 0)
                {
                    _cmapCache[fontResourceName] = cm;
                    return cm;
                }
                else
                {
                    // CMap was invalid, treat as no ToUnicode available
                    return null;
                }
            }

            return null;
        }


        private async Task<Encoding?> ResolveFontEncodingAsync(FontDictionary fontDictionary)
        {
            // PDF spec §9.6.5: Type0 fonts use CMap only
            if (fontDictionary is Type0FontDictionary)
                return null;

            // Fetch the Encoding entry: either a name or a dictionary
            Either<Name, EncodingDictionary> encoding = await fontDictionary.Encoding.GetAsync();

            // 1) No Encoding entry -> fall back to PDFDocEncoding as a safe default
            if (encoding.Value == null)
                return Encoding.GetEncoding(PDFEncoding.PDFDoc);

            // 2) Named Encoding (Type1): Standard, WinAnsi, MacRoman, PDFDoc
            if (encoding.Type1 != null)
            {
                return GetEncoding(encoding.Type1.Value);
            }

            // 3) Dictionary Encoding (Type2) with Differences
            if (encoding.Type2 != null)
            {
                Encoding baseEnc;

                var dict = encoding.Type2;

                // Determine base encoding
                var baseEncoding = await dict.BaseEncoding.GetAsync();
                if (baseEncoding != null)
                {
                    baseEnc = GetEncoding(baseEncoding.Value);
                }
                else
                {
                    // If BaseEncoding is not present, we take it from the font if embedded.
                    // If not, fallback to StandardEncoding.
                    FontDescriptorDictionary? fontDescriptor = await fontDictionary.FontDescriptor.GetAsync();
                    StreamObject<IStreamDictionary>? fontFile = fontDescriptor != null
                        ? await fontDescriptor.FontFile.GetAsync()
                        : null;

                    bool hasEmbeddedFont = fontFile != null;

                    if (hasEmbeddedFont)
                    {
                        // For embedded fonts, the base encoding is the font’s built-in encoding
                        // Returning null here and handling the font's char mapping in MapCharacterCode
                        return null;
                    }
                    else
                    {
                        baseEnc = Encoding.GetEncoding(PDFEncoding.Standard);
                    }
                }

                // Build the Differences map
                var differences = await dict.Differences.GetAsync();
                if (differences == null)
                {
                    return baseEnc;
                }

                var diffs = new Dictionary<byte, char>();
                byte currentCode = 0;
                foreach (var item in differences)
                {
                    if (item is Number intVal)
                    {
                        currentCode = (byte)intVal.Value;
                    }
                    else if (item is Name nameVal)
                    {
                        char mappedChar = GlyphToUnicodeTranslator.Translate(nameVal.Value);
                        diffs[currentCode] = mappedChar;
                        currentCode++;
                    }
                }

                return new DerivedEncoding(baseEnc, diffs);
            }

            // 4) Fallback
            return Encoding.GetEncoding(PDFEncoding.PDFDoc);
        }

        private string GetCurrentFontName()
        {
            return _currentFontName
                ?? throw new InvalidOperationException("Cannot resolve font information before a font has been selected.");
        }

        private async Task<string> ResolveCurrentFontNameAsync(FontDictionary fontDictionary)
        {
            var fontDescriptor = await fontDictionary.FontDescriptor.GetAsync();
            if (fontDescriptor != null)
            {
                var fontFamily = await fontDescriptor.FontFamily.GetAsync();
                if (fontFamily != null)
                {
                    return fontFamily.Decode(this);
                }
            }

            return (string)(await fontDictionary.BaseFont.GetAsync()
                ?? throw new InvalidPdfException("The current font is missing a BaseFont name."));
        }

        private void ApplyTJAdjustment(double adjust)
        {
            float h = HorizontalScaling / 100f;
            float tx = (float)(-adjust / 1000.0 * FontSize * h);
            TextMatrix = Matrix3x2.CreateTranslation(tx, 0) * TextMatrix;
        }

        internal void ApplyTextArrayAdjustment(double adjust) => ApplyTJAdjustment(adjust);

        internal void MoveTextPosition(float tx, float ty)
        {
            var t = Matrix3x2.CreateTranslation(tx, ty);
            TextMatrix = t * TextLineMatrix;
            TextLineMatrix = TextMatrix;
        }

        private static Encoding GetEncoding(string name)
        {
            return name switch
            {
                PDFEncoding.Standard => Encoding.GetEncoding(PDFEncoding.Standard),
                PDFEncoding.PDFDoc => Encoding.GetEncoding(PDFEncoding.PDFDoc),
                PDFEncoding.WinAnsi => Encoding.GetEncoding(1252),
                PDFEncoding.MacRoman => Encoding.GetEncoding(10000),
                _ => throw new NotSupportedException($"Unsupported encoding name: {name}"),
            };
        }

        private void ApplyOperatorState(ContentStreamOperation op)
        {
            switch (op.Operator)
            {
                case ContentStream.Operators.TextState.Tf:
                    ApplyFont(op.GetOperand<Name>(0).Value, op.GetOperand<Number>(1));
                    break;

                case ContentStream.Operators.TextState.Tc:
                    CharSpacing = op.GetOperand<Number>(0);
                    break;

                case ContentStream.Operators.TextState.Tw:
                    WordSpacing = op.GetOperand<Number>(0);
                    break;

                case ContentStream.Operators.TextState.Tz:
                    HorizontalScaling = op.GetOperand<Number>(0);
                    break;

                case ContentStream.Operators.TextState.Ts:
                    TextRise = op.GetOperand<Number>(0);
                    break;

                case ContentStream.Operators.TextPositioning.Td:
                    MoveTextPosition(op.GetOperand<Number>(0), op.GetOperand<Number>(1));
                    break;

                case ContentStream.Operators.TextPositioning.Tm:
                    var nums = op.Operands?.Cast<Number>().ToArray()
                        ?? throw new InvalidOperationException("Tm operator is missing operands.");
                    var m = new Matrix3x2(nums[0], nums[1], nums[2], nums[3], nums[4], nums[5]);
                    TextMatrix = m;
                    TextLineMatrix = m;
                    break;

                case ContentStream.Operators.TextPositioning.TStar:
                    MoveTextPosition(0, -TextLeading);
                    break;

                case ContentStream.Operators.TextState.TL:
                    TextLeading = op.GetOperand<Number>(0);
                    break;

                case ContentStream.Operators.TextPositioning.TD:
                    TextLeading = -op.GetOperand<Number>(1);
                    MoveTextPosition(op.GetOperand<Number>(0), op.GetOperand<Number>(1));
                    break;

                case ContentStream.Operators.TextObjects.BT:
                    BeginTextObject();
                    break;

                case "ET":
                    break;
            }
        }

        private async Task ApplyOperatorStateAsync(ContentStreamOperation op)
        {
            switch (op.Operator)
            {
                case ContentStream.Operators.TextState.Tf:
                    await ApplyFontAsync(op.GetOperand<Name>(0).Value, op.GetOperand<Number>(1));
                    break;

                case ContentStream.Operators.TextState.Tc:
                    CharSpacing = op.GetOperand<Number>(0);
                    break;

                case ContentStream.Operators.TextState.Tw:
                    WordSpacing = op.GetOperand<Number>(0);
                    break;

                case ContentStream.Operators.TextState.Tz:
                    HorizontalScaling = op.GetOperand<Number>(0);
                    break;

                case ContentStream.Operators.TextState.Ts:
                    TextRise = op.GetOperand<Number>(0);
                    break;

                case ContentStream.Operators.TextPositioning.Td:
                    MoveTextPosition(op.GetOperand<Number>(0), op.GetOperand<Number>(1));
                    break;

                case ContentStream.Operators.TextPositioning.Tm:
                    var nums = op.Operands?.Cast<Number>().ToArray()
                        ?? throw new InvalidOperationException("Tm operator is missing operands.");
                    var m = new Matrix3x2(nums[0], nums[1], nums[2], nums[3], nums[4], nums[5]);
                    TextMatrix = m;
                    TextLineMatrix = m;
                    break;

                case ContentStream.Operators.TextPositioning.TStar:
                    MoveTextPosition(0, -TextLeading);
                    break;

                case ContentStream.Operators.TextState.TL:
                    TextLeading = op.GetOperand<Number>(0);
                    break;

                case ContentStream.Operators.TextPositioning.TD:
                    TextLeading = -op.GetOperand<Number>(1);
                    MoveTextPosition(op.GetOperand<Number>(0), op.GetOperand<Number>(1));
                    break;

                case ContentStream.Operators.TextObjects.BT:
                    BeginTextObject();
                    break;

                case "ET":
                    break;
            }
        }

        private async Task ApplyFontAsync(string fontResourceName, float fontSize)
        {
            FontResourceName = fontResourceName;
            FontSize = fontSize;

            var resolvedState = await ResolveFontStateAsync(fontResourceName);
            FontDictionary = resolvedState.FontDictionary;
            ToUnicodeCMap = resolvedState.ToUnicodeCMap;
            _fontEncoding = resolvedState.FontEncoding;
            _currentFontMetrics = resolvedState.FontMetrics;
            _currentFontName = resolvedState.FontName;

            TextMatrix = TextLineMatrix;
            TextLeading = FontSize * 1.2f;
        }

        private void ApplyFont(string fontResourceName, float fontSize)
        {
            if (!_resolvedFontStateCache.TryGetValue(fontResourceName, out var resolvedState))
            {
                throw new InvalidOperationException($"The font resource '{fontResourceName}' was used before its state was preloaded.");
            }

            FontResourceName = fontResourceName;
            FontSize = fontSize;
            FontDictionary = resolvedState.FontDictionary;
            ToUnicodeCMap = resolvedState.ToUnicodeCMap;
            _fontEncoding = resolvedState.FontEncoding;
            _currentFontMetrics = resolvedState.FontMetrics;
            _currentFontName = resolvedState.FontName;

            TextMatrix = TextLineMatrix;
            TextLeading = FontSize * 1.2f;
        }

        private async Task<ResolvedFontState> ResolveFontStateAsync(string fontResourceName)
        {
            if (_resolvedFontStateCache.TryGetValue(fontResourceName, out var cached))
            {
                return cached;
            }

            var fontDictionary = await _fontResourceMap.GetRequiredProperty<FontDictionary>(fontResourceName).GetAsync();
            var resolved = new ResolvedFontState(
                fontDictionary,
                await ResolveCMapAsync(fontResourceName, fontDictionary),
                await ResolveFontEncodingAsync(fontDictionary),
                await ResolveCurrentFontMetricsAsync(fontDictionary),
                await ResolveCurrentFontNameAsync(fontDictionary));

            _resolvedFontStateCache[fontResourceName] = resolved;
            return resolved;
        }

        private sealed record ResolvedFontState(
            FontDictionary FontDictionary,
            CMap? ToUnicodeCMap,
            Encoding? FontEncoding,
            FontMetrics? FontMetrics,
            string FontName);
    }
}
