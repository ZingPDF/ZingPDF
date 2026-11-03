using System.Globalization;
using System.Collections.Immutable;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;
using PdfDictionary = ZingPDF.Syntax.Objects.Dictionaries.Dictionary;
using Encoding = System.Text.Encoding;

namespace ZingPDF.Text.Editing;

/// <summary>
/// Reads and replaces existing, horizontal-writing text lines in page and Form XObject
/// content streams. Lines retain their baseline, painting state and surrounding content.
/// Paragraph reflow, vertical writing and text clipping modes are not supported.
/// </summary>
public sealed class PdfTextEditor
{
    private readonly Pdf _pdf;
    private readonly IParser<ContentStream> _parser;
    internal PdfTextEditor(Pdf pdf, IParser<ContentStream> parser) { _pdf = pdf; _parser = parser; }

    /// <summary>
    /// Reads lines from one 1-based page, including Form XObject instances. Results are
    /// revision-specific; obtain fresh lines after changing content or font resources.
    /// The document's input stream must remain readable and seekable throughout the call.
    /// </summary>
    public async Task<IReadOnlyList<PdfEditableTextLine>> GetLinesAsync(int pageNumber, CancellationToken cancellationToken = default)
        => (await AnalyzeAsync(pageNumber, cancellationToken)).Lines.Select(l => l.Info).ToArray();

    /// <summary>Checks font coverage and available line width without changing document objects.</summary>
    public async Task<PdfTextEditCheck> CheckReplacementAsync(PdfEditableTextLine line, string text,
        PdfTextReplacementOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(line);
        var analysis = await AnalyzeAsync(line.PageNumber, cancellationToken);
        var target = FindLine(analysis, line);
        using var prepared = await PrepareAsync(target, text, options ?? new());
        return prepared.Check;
    }

    /// <summary>
    /// Replaces a previously read line by rewriting its text-showing operators. Full supplied
    /// TrueType font programs are embedded automatically with Identity-H and ToUnicode mappings.
    /// Shared content streams and Form XObjects are copied for the edited occurrence.
    /// Original following text positions and text-line matrices are preserved. A rejected
    /// coverage/overflow check leaves page contents unchanged. Saving still follows the
    /// document's normal incremental-save policy; this operation is not secure redaction.
    /// </summary>
    public async Task ReplaceLineAsync(PdfEditableTextLine line, string text,
        PdfTextReplacementOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(line);
        options ??= new();
        var analysis = await AnalyzeAsync(line.PageNumber, cancellationToken);
        var target = FindLine(analysis, line);
        using var prepared = await PrepareAsync(target, text, options);
        if (!prepared.Check.CanApply) throw new InvalidOperationException(prepared.Check.Message);
        cancellationToken.ThrowIfCancellationRequested();
        var first = target.Runs[0];
        var fontResource = first.Before.Font;
        var replacementBytes = prepared.RegisteredFont?.Encode(text).Bytes ?? prepared.Bytes;
        if (prepared.NewFont is not null || prepared.RegisteredFont is not null)
        {
            fontResource = "ZTE" + Guid.NewGuid().ToString("N");
            var fontReference = prepared.RegisteredFont?.FontReference ?? await prepared.NewFont!.EmbedAsync(_pdf, prepared.ExistingDescriptor);
            first.Container.Resources = CopyResources(first.Container.Resources);
            first.Container.ResourcesChanged = true;
            var fonts = await first.Container.Resources.Font.GetAsync() ?? new PdfDictionary(_pdf, ObjectContext.UserCreated);
            var cloned = new PdfDictionary(fonts, _pdf, ObjectContext.UserCreated);
            cloned.Set(fontResource, fontReference);
            first.Container.Resources.Set("Font", cloned);
        }
        var rewrittenContainers = target.Runs.Select(r => r.Container).Distinct().ToDictionary(c => c, c => (ContentStream)c.Content.Clone());
        foreach (var run in target.Runs.OrderByDescending(r => r.Index))
        {
            var rewritten = rewrittenContainers[run.Container];
            var operations = new List<ContentStreamOperation>();
            var original = run.Container.Content.Operations[run.Index];
            if (original.Operator == "\"")
            {
                operations.Add(Op("Tw", original.GetOperand<Number>(0)));
                operations.Add(Op("Tc", original.GetOperand<Number>(1)));
            }
            if (original.Operator is "'" or "\"") operations.Add(Op("T*"));
            if (ReferenceEquals(run, first))
            {
                operations.Add(Op("Tf", (Name)fontResource, (Number)prepared.Size));
                if ((prepared.NewFont is not null || prepared.RegisteredFont?.IsUnicode == true) && run.Before.WordSpacing != 0)
                    operations.Add(Op("TJ", BuildSpacedUnicodeArray(text, replacementBytes, run.Before.WordSpacing, prepared.Size)));
                else operations.Add(Op("Tj", PdfString.FromBytes(replacementBytes, PdfStringSyntax.Hex, ObjectContext.UserCreated)));
                operations.Add(Op("Tf", (Name)run.Before.Font, (Number)run.Before.Size));
            }
            // Tm restores the original line matrix. A numeric-only TJ then restores the
            // text matrix without changing the line matrix used by later Td/T* operators.
            operations.Add(MatrixOp(run.AfterLine));
            if (!Matrix3x2.Invert(run.AfterLine, out var inverse)) throw new NotSupportedException("The text line uses a non-invertible transformation.");
            var displacement = Vector2.Transform(run.AfterText.Translation, inverse);
            var denominator = run.Before.Size * run.Before.HorizontalScale / 100;
            if (Math.Abs(displacement.X) > .00001)
                operations.Add(Op("TJ", new ArrayObject([(Number)(-displacement.X / denominator * 1000)], ObjectContext.UserCreated)));
            rewritten.Operations.RemoveAt(run.Index);
            rewritten.Operations.InsertRange(run.Index, operations);
        }
        foreach (var entry in rewrittenContainers)
        {
            entry.Key.Content = entry.Value;
            await AttachEditedContainerAsync(analysis, entry.Key);
        }
    }

    private static Line FindLine(Analysis analysis, PdfEditableTextLine line)
        => analysis.Lines.FirstOrDefault(l => l.Info.Id == line.Id && l.Info.Text == line.Text)
            ?? throw new InvalidOperationException("This text line has changed. Select it again before editing.");

    private async Task<Prepared> PrepareAsync(Line line, string text, PdfTextReplacementOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        var state = line.Runs[0].Before;
        var size = options.FontSize ?? state.Size;
        string? invalid = !line.Info.CanEdit ? line.Info.UnsupportedReason : ValidateText(text);
        if (!double.IsFinite(size) || size is <= 0 or > 1000) invalid = "Choose a font size between 0 and 1,000 points.";
        if (invalid is not null) return new([], size, null, new() { CanApply = false, Message = invalid });
        UnicodeTrueTypeFont? newFont = null;
        byte[] bytes;
        string missing;
        double width;
        IPdfObject? existingDescriptor = null;
        if (options.Font is not null && !options.TrueTypeFontData.IsEmpty)
            throw new ArgumentException("Choose a registered Font or TrueTypeFontData, not both.", nameof(options));
        if (options.Font is { } selectedFont)
        {
            if (!ReferenceEquals(selectedFont.Owner, _pdf)) throw new ArgumentException("Register the replacement font with this PDF document.", nameof(options));
            missing = selectedFont.GetMissingCharacters(text); bytes = [];
            width = missing.Length == 0 ? (selectedFont.MeasureText(text, size) + state.CharSpacing * text.EnumerateRunes().Count() + state.WordSpacing * text.Count(c => c == ' ')) * state.HorizontalScale / 100 : 0;
        }
        else if (!options.TrueTypeFontData.IsEmpty)
        {
            newFont = await UnicodeTrueTypeFont.CreateAsync(options.TrueTypeFontData, text, options.FontName);
            bytes = newFont.EncodedText; missing = newFont.MissingCharacters;
            width = (newFont.Width / 1000 * size + state.CharSpacing * text.EnumerateRunes().Count() + state.WordSpacing * text.Count(c => c == ' ')) * state.HorizontalScale / 100;
        }
        else
        {
            var originalFont = line.Runs[0].Font;
            originalFont.TryEncode(text, out bytes, out missing);
            width = Advance(originalFont, bytes, state with { Size = size });
            if (missing.Length != 0 && originalFont.IsEmbedded && !originalFont.IsSubset)
            {
                try
                {
                    var expanded = await UnicodeTrueTypeFont.CreateAsync(originalFont.Program, text, originalFont.Name);
                    if (expanded.MissingCharacters.Length == 0)
                    {
                        newFont = expanded; bytes = expanded.EncodedText; missing = "";
                        width = (expanded.Width / 1000 * size + state.CharSpacing * text.EnumerateRunes().Count() + state.WordSpacing * text.Count(c => c == ' ')) * state.HorizontalScale / 100;
                        existingDescriptor = originalFont.DescriptorReference;
                    }
                    else expanded.Dispose();
                }
                catch (NotSupportedException) { /* A subset/CFF or restricted program still needs an explicit replacement choice. */ }
            }
        }
        var overflow = width > line.AvailableWidth + .1;
        var canApply = missing.Length == 0 && (!overflow || options.AllowOverflow);
        var message = missing.Length != 0
            ? $"{(newFont is null && options.Font is null ? "The original" : "The selected")} font cannot encode: {missing}. Choose a font that includes these characters."
            : overflow && !options.AllowOverflow ? "The replacement is wider than the available line space. Shorten the text, reduce its size, or explicitly allow overflow."
            : existingDescriptor is not null ? "The existing font program covers the new characters. Its Unicode mapping will be extended automatically." : null;
        return new(bytes, size, newFont, new()
        {
            CanApply = canApply, RequiresFontChange = missing.Length != 0, MissingCharacters = missing, WillEmbedFont = newFont is not null,
            WouldOverflow = overflow, ReplacementWidth = width, AvailableWidth = line.AvailableWidth, Message = message
        }, existingDescriptor, options.Font);
    }

    private static string? ValidateText(string text)
    {
        if (text.Length > 10000) return "Use up to 10,000 characters in one line.";
        if (text.Any(char.IsControl)) return "This edits one line. Remove line breaks, tabs and other control characters.";
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsSurrogate(text[i]) && (!char.IsHighSurrogate(text[i]) || i + 1 >= text.Length || !char.IsLowSurrogate(text[++i])))
                return "The text contains an incomplete Unicode character.";
        }
        if (text.EnumerateRunes().Any(r => r.Value is >= 0x0590 and <= 0x1CFF or >= 0xA800 and <= 0xABFF or >= 0xFB1D and <= 0xFEFF || Rune.GetUnicodeCategory(r) is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark))
            return "This version edits left-to-right lines without complex-script shaping. Use precomposed accented characters where possible.";
        return null;
    }

    private static double Advance(EditingFont font, byte[] bytes, State state)
        => font.Decode(bytes).Sum(g => (g.Width / 1000 * state.Size + state.CharSpacing + (g.Code.Length == 1 && g.Code[0] == 32 ? state.WordSpacing : 0)) * state.HorizontalScale / 100);

    private static ArrayObject BuildSpacedUnicodeArray(string text, byte[] bytes, double spacing, double size)
    {
        var array = new ArrayObject(); var index = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            array.Add(PdfString.FromBytes(bytes.AsSpan(index, 2).ToArray(), PdfStringSyntax.Hex, ObjectContext.UserCreated));
            if (rune.Value == 32) array.Add((Number)(-spacing / size * 1000));
            index += 2;
        }
        return array;
    }

    private sealed record Prepared(byte[] Bytes, double Size, UnicodeTrueTypeFont? NewFont, PdfTextEditCheck Check, IPdfObject? ExistingDescriptor = null, PdfFont? RegisteredFont = null) : IDisposable
    { public void Dispose() => NewFont?.Dispose(); }

    private sealed record State
    {
        public Matrix3x2 Transform { get; set; } = Matrix3x2.Identity;
        public Matrix3x2 Text { get; set; } = Matrix3x2.Identity;
        public Matrix3x2 Line { get; set; } = Matrix3x2.Identity;
        public string Font { get; set; } = "";
        public double Size { get; set; } = 12;
        public double CharSpacing { get; set; }
        public double WordSpacing { get; set; }
        public double HorizontalScale { get; set; } = 100;
        public double Rise { get; set; }
        public double Leading { get; set; }
        public int RenderingMode { get; set; }
        public ImmutableDictionary<string, string> Painting { get; set; } = ImmutableDictionary<string, string>.Empty;
        public int ClipVersion { get; set; }
        public int LastBarrier { get; set; }
        public string? MarkedUnsupported { get; set; }
        public Stack<string?> MarkedScopes { get; set; } = new();
        public string? Unsupported { get; set; }
        public bool InText { get; set; }
    }

    private sealed class Container
    {
        public required string Path { get; init; }
        public required ContentStream Content { get; set; }
        public required ResourceDictionary Resources { get; set; }
        public bool ResourcesChanged { get; set; }
        public required string Hash { get; init; }
        public Container? Parent { get; init; }
        public int Invocation { get; init; }
        public int RootIndex { get; init; }
        public IStreamDictionary? FormDictionary { get; init; }
    }
    private sealed record Run(Container Container, int Index, string Text, EditingFont Font, State Before,
        Matrix3x2 AfterText, Matrix3x2 AfterLine, double Advance, int Barrier);
    private sealed record Line(PdfEditableTextLine Info, List<Run> Runs, double AvailableWidth);
    private sealed record Analysis(Page Page, IPdfObject[] OriginalContents, List<Line> Lines);

    private async Task<Analysis> AnalyzeAsync(int pageNumber, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        var page = await _pdf.GetPageAsync(pageNumber);
        var geometry = await page.GetGeometryAsync(cancellationToken);
        var resources = new ResourceDictionary(await page.Dictionary.Resources.GetAsync() ?? new PdfDictionary(_pdf, ObjectContext.UserCreated));
        var raw = await page.Dictionary.Contents.GetRawValueAsync();
        if (raw is IndirectObjectReference arrayReference && (await _pdf.Objects.GetAsync(arrayReference)).Object is ArrayObject indirectArray) raw = indirectArray;
        var originals = raw is ArrayObject array ? array.ToArray() : raw is null ? [] : new[] { raw };
        var runs = new List<Run>(); var state = new State(); var stack = new Stack<State>(); var barrier = 0;
        for (var index = 0; index < originals.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await ResolveAsync(originals[index]) is not IStreamObject stream) continue;
            var container = await ParseContainerAsync(stream, resources, $"p{pageNumber}/s{index}", index);
            state = await WalkAsync(container, state, stack, runs, barrier, 0, cancellationToken);
            barrier = state.LastBarrier;
        }
        var groups = new List<List<Run>>();
        foreach (var run in runs)
        {
            if (string.IsNullOrWhiteSpace(run.Text)) continue;
            if (groups.Count > 0 && CanJoin(groups[^1][^1], run)) groups[^1].Add(run);
            else groups.Add([run]);
        }
        var lines = new List<Line>();
        foreach (var group in groups)
        {
            var first = group[0]; var last = group[^1];
            var matrix = first.Before.Text * first.Before.Transform;
            if (!Matrix3x2.Invert(matrix, out var inverse)) continue;
            var text = new StringBuilder(first.Text);
            for (var i = 1; i < group.Count; i++)
            {
                var previousEnd = PagePoint(group[i - 1].AfterText, group[i - 1].Before.Transform);
                var nextStart = PagePoint(group[i].Before.Text, group[i].Before.Transform);
                var gap = Vector2.Transform(nextStart, inverse).X - Vector2.Transform(previousEnd, inverse).X;
                if (gap > first.Before.Size * .15 && !text.ToString().EndsWith(' ') && !group[i].Text.StartsWith(' ')) text.Append(' ');
                text.Append(group[i].Text);
            }
            var end = Vector2.Transform(PagePoint(last.AfterText, last.Before.Transform), inverse).X;
            var ascent = first.Font.Ascent / 1000 * first.Before.Size + first.Before.Rise;
            var descent = first.Font.Descent / 1000 * first.Before.Size + first.Before.Rise;
            var corners = new[] { new Vector2(0, (float)descent), new Vector2((float)end, (float)descent), new Vector2(0, (float)ascent), new Vector2((float)end, (float)ascent) }.Select(p => Vector2.Transform(p, matrix)).ToArray();
            var bounds = Rectangle.FromCoordinates(new(corners.Min(p => p.X), corners.Min(p => p.Y)), new(corners.Max(p => p.X), corners.Max(p => p.Y)));
            var available = DistanceToPageEdge(matrix, geometry.VisibleBox);
            foreach (var other in runs.Except(group))
            {
                var position = Vector2.Transform(PagePoint(other.Before.Text, other.Before.Transform), inverse);
                if (position.X > end + .1 && Math.Abs(position.Y) < first.Before.Size * .3) available = Math.Min(available, position.X - 2);
            }
            available = Math.Max(end, available);
            var unsupported = first.Before.MarkedUnsupported ?? first.Before.Unsupported ?? (!first.Font.SupportedEncoding ? "This font uses a vertical or unsupported encoding." : null)
                ?? (first.Before.RenderingMode is >= 3 ? "Invisible text and text clipping modes cannot be edited here." : null)
                ?? (text.ToString().Contains('\uFFFD') ? "The text does not have a reliable Unicode mapping." : null)
                ?? (first.Before.Size <= 0 || first.Before.HorizontalScale <= 0 ? "This text uses an unsupported size or horizontal scale." : null);
            var ancestry = new StringBuilder();
            foreach (var source in group.Select(r => r.Container).Distinct())
                for (var ancestor = source; ancestor is not null; ancestor = ancestor.Parent) ancestry.Append(ancestor.Path).Append(':').Append(ancestor.Hash);
            var id = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(FormattableString.Invariant($"{first.Container.Path}:{ancestry}:{first.Index}:{last.Index}:{text}:{first.Font.Name}:{first.Font.Signature}:{matrix}"))));
            lines.Add(new(new()
            {
                Id = id, PageNumber = pageNumber, Text = text.ToString(), FontName = first.Font.Name, FontSize = first.Before.Size,
                BaselineOrigin = new(matrix.M31, matrix.M32), Bounds = bounds, CanEdit = unsupported is null, UnsupportedReason = unsupported,
                IsFontEmbedded = first.Font.IsEmbedded, IsFontSubset = first.Font.IsSubset
            }, group, available));
        }
        return new(page, originals, lines);
    }

    private async Task<Container> ParseContainerAsync(IStreamObject stream, ResourceDictionary resources, string path, int rootIndex, Container? parent = null, int invocation = 0)
    {
        using var data = await stream.GetDecompressedDataAsync();
        using var bytes = new MemoryStream(); await data.CopyToAsync(bytes);
        var hash = Convert.ToHexString(SHA256.HashData(bytes.GetBuffer().AsSpan(0, (int)bytes.Length)));
        bytes.Position = 0;
        return new() { Path = path, Content = await _parser.ParseAsync(bytes, ObjectContext.WithOrigin(ObjectOrigin.ParsedContentStream)),
            Hash = hash, Resources = resources, RootIndex = rootIndex, Parent = parent, Invocation = invocation, FormDictionary = parent is null ? null : stream.Dictionary };
    }

    private async Task<State> WalkAsync(Container container, State state, Stack<State> stack, List<Run> runs, int barrier, int depth, CancellationToken token)
    {
        if (depth > 24) throw new NotSupportedException("This PDF nests Form XObjects too deeply to edit text safely.");
        var fonts = await container.Resources.Font.GetAsync();
        var loadedFonts = new Dictionary<string, EditingFont>();
        for (var index = 0; index < container.Content.Operations.Count; index++)
        {
            token.ThrowIfCancellationRequested();
            var op = container.Content.Operations[index];
            double N(int n) => op.GetOperand<Number>(n).Value;
            switch (op.Operator)
            {
                case "q": stack.Push(state with { }); break;
                case "Q": if (stack.TryPop(out var restored)) { var text = state.Text; var line = state.Line; var inText = state.InText; var marked = state.MarkedUnsupported; var scopes = state.MarkedScopes;
                    state = restored; state.Text = text; state.Line = line; state.InText = inText; state.MarkedUnsupported = marked; state.MarkedScopes = scopes; } break;
                case "cm": state.Transform = ReadMatrix(op) * state.Transform; break;
                case "BT": state.Text = state.Line = Matrix3x2.Identity; state.InText = true; break;
                case "ET": state.InText = false; break;
                case "Tf": state.Font = op.GetOperand<Name>(0).Value; state.Size = N(1); state.Unsupported = null; break;
                case "Tc": state.CharSpacing = N(0); break;
                case "Tw": state.WordSpacing = N(0); break;
                case "Tz": state.HorizontalScale = N(0); break;
                case "Ts": state.Rise = N(0); break;
                case "TL": state.Leading = N(0); break;
                case "Tr": state.RenderingMode = (int)N(0); break;
                case "Tm": state.Text = state.Line = ReadMatrix(op); break;
                case "Td": MoveLine(state, N(0), N(1)); break;
                case "TD": state.Leading = -N(1); MoveLine(state, N(0), N(1)); break;
                case "T*": MoveLine(state, 0, -state.Leading); break;
                case "g": case "rg": case "k": case "G": case "RG": case "K": case "cs": case "CS": case "sc": case "SC": case "scn": case "SCN":
                    var paintKey = op.Operator switch { "g" or "rg" or "k" => "fill", "G" or "RG" or "K" => "stroke", _ => op.Operator };
                    state.Painting = state.Painting.SetItem(paintKey, op.Operator + ":" + string.Join(",", op.Operands?.Select(p => p.ToString()) ?? [])); break;
                case "gs":
                    var states = await container.Resources.ExtGState.GetAsync();
                    if (states is not null && await states.GetOptionalProperty<PdfDictionary>(op.GetOperand<Name>(0).Value).GetAsync() is { } graphicsState &&
                        await graphicsState.GetOptionalProperty<ArrayObject>("Font").GetAsync() is { } fontState && fontState.Count() >= 2)
                    {
                        // Resolve the actual font for accurate positions, but do not rewrite a font selected through gs yet.
                        fonts = fonts is null ? new PdfDictionary(_pdf, ObjectContext.UserCreated) : new PdfDictionary(fonts, _pdf, ObjectContext.UserCreated);
                        state.Font = "ZGS" + index;
                        fonts.Set(state.Font, fontState[0]); state.Size = ((Number)fontState[1]).Value;
                        state.Unsupported = "This text selects its font through ExtGState /Font. Other text on this page can still be edited.";
                    }
                    state.Painting = state.Painting.SetItem("gs", op.GetOperand<Name>(0).Value);
                    break;
                case "BDC": case "BMC":
                    state.MarkedScopes.Push(state.MarkedUnsupported);
                    if (op.Operator == "BDC")
                    {
                        var properties = op.Operands![1];
                        if (properties is Name propertyName && await container.Resources.Properties.GetAsync() is { } namedProperties)
                            properties = await namedProperties.GetOptionalProperty<PdfDictionary>(propertyName.Value).GetAsync() ?? properties;
                        if (properties is PdfDictionary propertyDictionary && propertyDictionary.InnerDictionary.ContainsKey("ActualText"))
                            state.MarkedUnsupported = "This text has an ActualText logical mapping which cannot be replaced here. Other text on this page can still be edited.";
                    }
                    barrier++;
                    break;
                case "EMC":
                    if (state.MarkedScopes.TryPop(out var markedReason)) state.MarkedUnsupported = markedReason;
                    barrier++; break;
                case "W": case "W*": state.ClipVersion = ++barrier; break;
                case "Tj": case "TJ": case "'": case "\"":
                    if (fonts is null || !state.InText) break;
                    if (!loadedFonts.TryGetValue(state.Font, out var font)) loadedFonts[state.Font] = font = await EditingFont.LoadAsync(_pdf, fonts, state.Font);
                    if (op.Operator == "\"") { state.WordSpacing = N(0); state.CharSpacing = N(1); }
                    if (op.Operator is "'" or "\"") MoveLine(state, 0, -state.Leading);
                    var before = state with { };
                    var builder = new StringBuilder(); var advance = 0d;
                    var elements = op.Operator == "TJ" ? op.GetOperand<ArrayObject>(0).ToArray() : new IPdfObject[] { op.GetOperand<PdfString>(op.Operator == "\"" ? 2 : 0) };
                    foreach (var element in elements)
                    {
                        if (element is Number adjust) advance += -adjust.Value / 1000 * state.Size * state.HorizontalScale / 100;
                        else if (element is PdfString value) { foreach (var glyph in font.Decode(value.Bytes)) builder.Append(glyph.Text); advance += Advance(font, value.Bytes, state); }
                    }
                    state.Text = Matrix3x2.CreateTranslation((float)advance, 0) * state.Text;
                    runs.Add(new(container, index, builder.ToString(), font, before, state.Text, state.Line, advance, barrier));
                    break;
                case "Do":
                    barrier++;
                    var xObjects = await container.Resources.XObject.GetAsync();
                    var name = op.GetOperand<Name>(0).Value;
                    if (xObjects is null || !xObjects.InnerDictionary.TryGetValue(name, out var xValue)) break;
                    if (await ResolveAsync(xValue) is not IStreamObject form || form.Dictionary.InnerDictionary.GetValueOrDefault("Subtype") is not Name { Value: "Form" }) break;
                    var formDictionary = new PdfDictionary(form.Dictionary);
                    var formResources = await formDictionary.GetOptionalProperty<PdfDictionary>("Resources").GetAsync();
                    var child = await ParseContainerAsync(form, formResources is null ? container.Resources : new(formResources), container.Path + "/d" + index, container.RootIndex, container, index);
                    var childState = state with { MarkedScopes = new Stack<string?>(state.MarkedScopes.Reverse()) };
                    if (await formDictionary.GetOptionalProperty<ArrayObject>("Matrix").GetAsync() is { } transform) childState.Transform = ReadMatrix(transform.Cast<Number>().Select(n => n.Value).ToArray()) * state.Transform;
                    await WalkAsync(child, childState, new Stack<State>(), runs, barrier, depth + 1, token);
                    barrier = runs.Count + barrier + 1;
                    break;
                case "S": case "s": case "f": case "F": case "f*": case "B": case "B*": case "b": case "b*": case "sh": barrier++; break;
            }
        }
        state.LastBarrier = barrier;
        return state;
    }

    private async Task AttachEditedContainerAsync(Analysis analysis, Container container)
    {
        while (container.Parent is { } parent)
        {
            var dictionary = new StreamDictionary(_pdf, ObjectContext.UserCreated);
            foreach (var entry in container.FormDictionary!.InnerDictionary)
                if (entry.Key is not ("Length" or "Filter" or "DecodeParms")) dictionary.Set(entry.Key, entry.Value);
            dictionary.Set("Resources", container.Resources);
            var stream = await new ContentStreamFactory([container.Content]).CreateAsync(dictionary, ObjectContext.UserCreated);
            var reference = (await _pdf.Objects.AddAsync(stream)).Reference;
            parent.Resources = CopyResources(parent.Resources); parent.ResourcesChanged = true;
            var old = await parent.Resources.XObject.GetAsync();
            var xObjects = old is null ? new PdfDictionary(_pdf, ObjectContext.UserCreated) : new PdfDictionary(old, _pdf, ObjectContext.UserCreated);
            var name = "ZTE" + Guid.NewGuid().ToString("N"); xObjects.Set(name, reference); parent.Resources.Set("XObject", xObjects);
            parent.Content = (ContentStream)parent.Content.Clone(); parent.Content.Operations[container.Invocation] = Op("Do", (Name)name);
            container = parent;
        }
        var rootStream = await new ContentStreamFactory([container.Content]).CreateAsync(new StreamDictionary(_pdf, ObjectContext.UserCreated), ObjectContext.UserCreated);
        var rootReference = (await _pdf.Objects.AddAsync(rootStream)).Reference;
        var contents = analysis.OriginalContents; contents[container.RootIndex] = rootReference;
        analysis.Page.Dictionary.Set("Contents", new ArrayObject(contents, ObjectContext.UserCreated));
        if (container.ResourcesChanged) analysis.Page.Dictionary.Set("Resources", container.Resources);
        _pdf.Objects.Update(analysis.Page.IndirectObject);
    }

    private async Task<IPdfObject> ResolveAsync(IPdfObject value) => value is IndirectObjectReference reference ? (await _pdf.Objects.GetAsync(reference)).Object : value;
    private ResourceDictionary CopyResources(ResourceDictionary resources) => new(new PdfDictionary(resources, _pdf, ObjectContext.UserCreated));
    private static ContentStreamOperation Op(string name, params IPdfObject[] operands) => new() { Operator = name, Operands = operands.ToList() };
    private static ContentStreamOperation MatrixOp(Matrix3x2 m) => Op("Tm", (Number)m.M11, (Number)m.M12, (Number)m.M21, (Number)m.M22, (Number)m.M31, (Number)m.M32);
    private static Matrix3x2 ReadMatrix(ContentStreamOperation op) => ReadMatrix(op.Operands!.Cast<Number>().Select(n => n.Value).ToArray());
    private static Matrix3x2 ReadMatrix(double[] n) => new((float)n[0], (float)n[1], (float)n[2], (float)n[3], (float)n[4], (float)n[5]);
    private static void MoveLine(State state, double x, double y) { state.Text = state.Line = Matrix3x2.CreateTranslation((float)x, (float)y) * state.Line; }
    private static Vector2 PagePoint(Matrix3x2 text, Matrix3x2 transform) => Vector2.Transform(Vector2.Zero, text * transform);
    private static bool CanJoin(Run previous, Run next)
    {
        if ((!ReferenceEquals(previous.Container, next.Container) && (previous.Container.Parent is not null || next.Container.Parent is not null)) || previous.Barrier != next.Barrier ||
            previous.Font.Name != next.Font.Name || previous.Font.Signature != next.Font.Signature || previous.Before.Size != next.Before.Size ||
            previous.Before.CharSpacing != next.Before.CharSpacing || previous.Before.WordSpacing != next.Before.WordSpacing || previous.Before.HorizontalScale != next.Before.HorizontalScale ||
            previous.Before.Rise != next.Before.Rise || previous.Before.RenderingMode != next.Before.RenderingMode || previous.Before.ClipVersion != next.Before.ClipVersion || previous.Before.MarkedUnsupported != next.Before.MarkedUnsupported || previous.Before.Unsupported != next.Before.Unsupported ||
            previous.Before.Painting.Count != next.Before.Painting.Count || previous.Before.Painting.Any(p => !next.Before.Painting.TryGetValue(p.Key, out var value) || value != p.Value)) return false;
        var matrix = previous.Before.Text * previous.Before.Transform;
        var nextMatrix = next.Before.Text * next.Before.Transform;
        if (Math.Abs(matrix.M11 - nextMatrix.M11) + Math.Abs(matrix.M12 - nextMatrix.M12) + Math.Abs(matrix.M21 - nextMatrix.M21) + Math.Abs(matrix.M22 - nextMatrix.M22) > .001 || !Matrix3x2.Invert(matrix, out var inverse)) return false;
        var start = Vector2.Transform(PagePoint(next.Before.Text, next.Before.Transform), inverse);
        return Math.Abs(start.Y) < .1 && start.X >= previous.Advance - .1 && start.X - previous.Advance <= previous.Before.Size * .75;
    }
    private static double DistanceToPageEdge(Matrix3x2 matrix, Rectangle box)
    {
        var origin = matrix.Translation; var direction = Vector2.TransformNormal(Vector2.UnitX, matrix);
        var x = direction.X > .000001 ? ((double)box.UpperRight.X - origin.X) / direction.X : direction.X < -.000001 ? ((double)box.LowerLeft.X - origin.X) / direction.X : double.PositiveInfinity;
        var y = direction.Y > .000001 ? ((double)box.UpperRight.Y - origin.Y) / direction.Y : direction.Y < -.000001 ? ((double)box.LowerLeft.Y - origin.Y) / direction.Y : double.PositiveInfinity;
        return Math.Max(0, Math.Min(x, y));
    }
}
