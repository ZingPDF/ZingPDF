using System.Text;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Graphics;
using ZingPDF.Rendering;
using ZingPDF.Syntax.Objects;
using ZingPDF.Text;
using ZingPDF.Text.Editing;
using Path = System.IO.Path;

namespace ZingPDF.Editor.Core;

// This application layer deliberately uses public ZingPDF APIs only.
// The UI serializes mutations. Background previews capture immutable states;
// Pdf instances are short-lived and never shared between workers.
public sealed class EditorDocument
{
    private sealed record State(byte[] Bytes, TextAddition[] Text)
    {
        // Cache tags must not keep complete PDF snapshots alive after history is pruned.
        public object Token { get; } = new();
    }
    private State _state;
    private readonly List<State> _undo = [];
    private readonly List<State> _redo = [];
    private State _saved;
    private readonly SemaphoreSlim _renderGate = new(1, 1);
    private readonly Dictionary<(int Page, double Scale), PdfPageRenderResult> _renderCache = [];
    private object? _renderRevision;
    public const long MaximumFileBytes = 256L * 1024 * 1024;
    public object RevisionToken => _state.Token;

    private EditorDocument(string path, byte[] bytes, int count)
    {
        SourcePath = Path.GetFullPath(path);
        _state = new(bytes, []);
        _saved = _state;
        PageCount = count;
    }

    public string SourcePath { get; }
    public int PageCount { get; private set; }
    public bool IsDirty => !ReferenceEquals(_saved, _state);
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public IReadOnlyList<TextAddition> AddedText => Array.AsReadOnly(_state.Text);

    /// <summary>Reads existing lines on one page without including this session's added-text overlays.</summary>
    public async Task<IReadOnlyList<PdfEditableTextLine>> GetExistingTextLinesAsync(int pageNumber)
    {
        var state = _state;
        using var input = new MemoryStream(state.Bytes, writable: false);
        using var pdf = Pdf.Load(input);
        return await pdf.CreateTextEditor().GetLinesAsync(pageNumber);
    }

    /// <summary>Checks a draft against the current source line before changing document state.</summary>
    public async Task<PdfTextEditCheck> CheckExistingTextAsync(PdfEditableTextLine line, string text, PdfTextReplacementOptions options)
    {
        var state = _state;
        using var input = new MemoryStream(state.Bytes, writable: false);
        using var pdf = Pdf.Load(input);
        return await pdf.CreateTextEditor().CheckReplacementAsync(line, text, options);
    }

    /// <summary>Replaces one source-backed line as one undoable edit, embedding a chosen font automatically.</summary>
    public async Task ReplaceExistingTextAsync(PdfEditableTextLine line, string text, PdfTextReplacementOptions options)
    {
        await MutatePdfAsync(pdf => pdf.CreateTextEditor().ReplaceLineAsync(line, text, options), _state.Text);
    }

    public static async Task<EditorDocument> OpenAsync(string path)
    {
        var bytes = await ReadSourceAsync(path);
        using var input = new MemoryStream(bytes, writable: false);
        using var pdf = Pdf.Load(input);
        await ValidateEditableAsync(pdf);
        var count = await pdf.GetPageCountAsync();
        if (count < 1) throw new InvalidDataException("This document has no pages.");
        return new(path, bytes, count);
    }

    private static async Task ValidateEditableAsync(Pdf pdf)
    {
        if (await pdf.IsEncryptedAsync())
            throw new NotSupportedException("Password-protected PDFs are not supported in this version. Open an unprotected copy instead.");
        if ((await pdf.GetSignaturesAsync()).Count > 0)
            throw new NotSupportedException("This PDF has a digital signature. Editing could invalidate it. Open an unsigned copy instead.");
    }

    public async Task<PdfPageGeometry> GetGeometryAsync(int pageNumber)
    {
        using var input = new MemoryStream(_state.Bytes, false);
        using var pdf = Pdf.Load(input);
        return await (await pdf.GetPageAsync(pageNumber)).GetGeometryAsync();
    }

    public async Task<PdfPageRenderResult> RenderAsync(int pageNumber, double scale)
    {
        var state = _state;
        await _renderGate.WaitAsync();
        try
        {
            if (!ReferenceEquals(_renderRevision, state.Token)) { _renderCache.Clear(); _renderRevision = state.Token; }
            var key = (pageNumber, scale);
            if (_renderCache.TryGetValue(key, out var cached)) return cached;
            using var input = new MemoryStream(state.Bytes, 0, state.Bytes.Length, writable: false, publiclyVisible: true);
            using var pdf = Pdf.Load(input);
            await ApplyTextAsync(pdf, state.Text.Where(t => t.PageNumber == pageNumber));
            var geometry = await (await pdf.GetPageAsync(pageNumber)).GetGeometryAsync();
            // Bound native raster allocations for unusually large pages and high zoom settings.
            scale = Math.Min(scale, Math.Min(5000 / geometry.DisplayWidth, 5000 / geometry.DisplayHeight));
            var result = await pdf.RenderPageAsync(pageNumber, new PdfPageRenderOptions { Scale = scale });
            if (result.PngBytes.Length <= 16 * 1024 * 1024)
            {
                while (_renderCache.Count >= 4 || _renderCache.Count > 0 && _renderCache.Values.Sum(r => (long)r.PngBytes.Length) + result.PngBytes.Length > 16 * 1024 * 1024)
                    _renderCache.Remove(_renderCache.Keys.First());
                _renderCache[key] = result;
            }
            return result;
        }
        finally { _renderGate.Release(); }
    }

    public async Task<Guid> AddTextAsync(int pageNumber, double displayX, double displayY)
    {
        var geometry = await GetGeometryAsync(pageNumber);
        var origin = geometry.DisplayToPage(new Coordinate(Math.Clamp(displayX, 0, Math.Max(0, geometry.DisplayWidth - 20)), Math.Clamp(displayY, 0, Math.Max(0, geometry.DisplayHeight - 20))));
        var item = new TextAddition(Guid.NewGuid(), pageNumber, "Your text", origin.X, origin.Y, 16, geometry.RotationDegrees);
        Commit(new(_state.Bytes, [.. _state.Text, item]));
        return item.Id;
    }

    public void UpdateText(Guid id, string text, double size)
    {
        ValidateText(text);
        if (!double.IsFinite(size) || size is < 6 or > 96) throw new ArgumentOutOfRangeException(nameof(size));
        var existing = _state.Text.Single(t => t.Id == id);
        var updated = existing with { Text = text.Replace("\r\n", "\n").Replace('\r', '\n'), Size = size };
        if (updated != existing) Commit(new(_state.Bytes, _state.Text.Select(t => t.Id == id ? updated : t).ToArray()));
    }

    public async Task MoveTextAsync(Guid id, double displayX, double displayY)
    {
        var item = _state.Text.Single(t => t.Id == id);
        var geometry = await GetGeometryAsync(item.PageNumber);
        var point = geometry.DisplayToPage(new Coordinate(Math.Clamp(displayX, 0, Math.Max(0, geometry.DisplayWidth - 10)), Math.Clamp(displayY, 0, Math.Max(0, geometry.DisplayHeight - 10))));
        var updated = item with { X = point.X, Y = point.Y };
        if (updated != item) Commit(new(_state.Bytes, _state.Text.Select(t => t.Id == id ? updated : t).ToArray()));
    }

    public void RemoveText(Guid id)
    {
        if (_state.Text.Any(t => t.Id == id)) Commit(new(_state.Bytes, _state.Text.Where(t => t.Id != id).ToArray()));
    }

    public async Task RotateAsync(int pageNumber)
    {
        await MutatePdfAsync(async pdf => await (await pdf.GetPageAsync(pageNumber)).RotateAsync(Rotation.Degrees90), _state.Text);
    }

    public async Task DeletePageAsync(int pageNumber)
    {
        if (PageCount == 1) throw new InvalidOperationException("Keep at least one page in the document.");
        await MutatePdfAsync(pdf => pdf.DeletePageAsync(pageNumber), _state.Text.Where(t => t.PageNumber != pageNumber)
            .Select(t => t.PageNumber > pageNumber ? t with { PageNumber = t.PageNumber - 1 } : t).ToArray());
        PageCount--;
    }

    public async Task MovePageAsync(int from, int to)
    {
        if (from < 1 || from > PageCount || to < 1 || to > PageCount) throw new ArgumentOutOfRangeException(nameof(to));
        if (from == to) return;
        int Remap(int p) => p == from ? to : from < to && p > from && p <= to ? p - 1 : from > to && p >= to && p < from ? p + 1 : p;
        await MutatePdfAsync(pdf => pdf.MovePageAsync(from, to), _state.Text.Select(t => t with { PageNumber = Remap(t.PageNumber) }).ToArray());
    }

    public async Task AppendAsync(string path)
    {
        var bytes = await ReadSourceAsync(path);
        if (_state.Bytes.LongLength + bytes.LongLength > MaximumFileBytes)
            throw new NotSupportedException("The combined PDF would exceed this editor's 256 MB limit. Split it into smaller documents first.");
        using var checkStream = new MemoryStream(bytes, false);
        using var check = Pdf.Load(checkStream);
        await ValidateEditableAsync(check);
        // Page merging does not currently provide a complete AcroForm field merge.
        if (await check.GetFormAsync() is not null)
            throw new NotSupportedException("Adding a PDF with form fields is not supported yet. Save a flattened copy of that PDF first.");
        var count = await check.GetPageCountAsync();
        using var input = new MemoryStream(bytes, false);
        await MutatePdfAsync(pdf => pdf.AppendPdfAsync(input), _state.Text);
        PageCount += count;
    }

    public async Task<IReadOnlyList<EditorFormField>> GetFormFieldsAsync()
    {
        using var input = new MemoryStream(_state.Bytes, false);
        using var pdf = Pdf.Load(input);
        var form = await pdf.GetFormAsync();
        if (form is null) return [];
        var fields = new List<EditorFormField>();
        foreach (var field in await form.GetFieldsAsync())
        {
            if (field is TextFormField text && !field.Properties.IsPassword)
                fields.Add(new(field.Name, field.Description ?? field.Name, await text.GetValueAsync() ?? "", field.Properties.IsReadOnly));
        }
        return fields;
    }

    public async Task SetFormFieldAsync(string name, string value)
    {
        ValidateText(value);
        await MutatePdfAsync(async pdf =>
        {
            var form = await pdf.GetFormAsync() ?? throw new InvalidOperationException("There are no form fields in this document.");
            var field = await form.GetFieldAsync<TextFormField>(name) ?? throw new InvalidOperationException("This field cannot be edited in this version.");
            if (field.Properties.IsReadOnly || field.Properties.IsPassword) throw new InvalidOperationException("This field cannot be edited.");
            await field.SetValueAsync(value);
        }, _state.Text);
    }

    public async Task SaveCopyAsync(string path, int? onlyPage = null)
    {
        var fullPath = Path.GetFullPath(path);
        if (string.Equals(fullPath, SourcePath, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidOperationException("Choose a different filename. Save a copy keeps your original PDF unchanged.");
        using var input = new MemoryStream(_state.Bytes, false);
        using var pdf = Pdf.Load(input);
        await ApplyTextAsync(pdf, _state.Text);
        byte[] bytes;
        if (onlyPage is { } page)
        {
            using var exported = await pdf.ExportPagesAsync([page]);
            bytes = await SerializeAsync(exported);
        }
        else bytes = await SerializeAsync(pdf);
        // Stage alongside the destination, then atomically replace it. Failed saves preserve existing files.
        var directory = Path.GetDirectoryName(fullPath)!;
        var temporary = Path.Combine(directory, $".zingpdf-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes);
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        if (onlyPage is null) _saved = _state;
    }

    public async Task UndoAsync() => await RestoreAsync(_undo, _redo);
    public async Task RedoAsync() => await RestoreAsync(_redo, _undo);

    private async Task RestoreAsync(List<State> from, List<State> to)
    {
        if (from.Count == 0) return;
        var previous = from[^1];
        using var input = new MemoryStream(previous.Bytes, false);
        using var pdf = Pdf.Load(input);
        var count = await pdf.GetPageCountAsync();
        to.Add(_state);
        from.RemoveAt(from.Count - 1);
        _state = previous;
        PageCount = count;
    }

    private void Commit(State state)
    {
        _undo.Add(_state);
        // History holds immutable PDF snapshots; bound retained revisions and byte storage.
        while (_undo.Count > 30 || _undo.Count > 1 && _undo.Select(s => s.Bytes).Distinct().Sum(b => (long)b.Length) > 128 * 1024 * 1024)
            _undo.RemoveAt(0);
        _redo.Clear();
        _state = state;
    }

    private async Task MutatePdfAsync(Func<Pdf, Task> action, TextAddition[] additions)
    {
        using var input = new MemoryStream(_state.Bytes, false);
        using var pdf = Pdf.Load(input);
        await action(pdf);
        var bytes = await SerializeAsync(pdf);
        Commit(new(bytes, additions));
    }

    private static async Task<byte[]> ReadSourceAsync(string path)
    {
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (input.Length > MaximumFileBytes)
            throw new NotSupportedException("This PDF exceeds this editor's 256 MB limit. Split it into smaller documents first.");
        var bytes = new byte[checked((int)input.Length)];
        await input.ReadExactlyAsync(bytes);
        return bytes;
    }

    private static async Task<byte[]> SerializeAsync(Pdf pdf)
    {
        using var output = new MemoryStream();
        await pdf.SaveAsync(output);
        return output.ToArray();
    }

    private static async Task ApplyTextAsync(Pdf pdf, IEnumerable<TextAddition> additions)
    {
        var items = additions.ToArray();
        if (items.Length == 0) return;
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);
        foreach (var item in items)
        {
            var angle = item.OrientationDegrees * Math.PI / 180;
            var baseline = new Coordinate(item.X + Math.Sin(angle) * item.Size * .8, item.Y - Math.Cos(angle) * item.Size * .8);
            await (await pdf.GetPageAsync(item.PageNumber)).AddTextAsync(item.Text, baseline, font, (Number)item.Size,
                RGBColour.Black, new TextPlacementOptions { RotationDegrees = item.OrientationDegrees, LineHeight = item.Size * 1.25 });
        }
    }

    private static void ValidateText(string text)
    {
        if (text.Length > 10000) throw new ArgumentException("Use up to 10,000 characters in one text box.");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try { Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetBytes(text); }
        catch (EncoderFallbackException) { throw new ArgumentException("This version supports Western European text. Some of these characters are not available in the editor font."); }
        if (text.Any(c => char.IsControl(c) && c is not '\r' and not '\n'))
            throw new ArgumentException("Remove tabs and other control characters from the text.");
    }
}

public sealed record TextAddition(Guid Id, int PageNumber, string Text, double X, double Y, double Size, int OrientationDegrees);
public sealed record EditorFormField(string Name, string Label, string Value, bool IsReadOnly);
