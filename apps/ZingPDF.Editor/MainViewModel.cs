using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ZingPDF.Editor.Core;
using ZingPDF.Elements.Drawing;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.Rendering;
using ZingPDF.Text.Editing;

namespace ZingPDF.Editor;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private static readonly PDFStandardFontMetricsProvider Metrics = new();
    private readonly Dictionary<string, string> _formDrafts = [];
    public EditorDocument? Document { get; private set; }
    public ObservableCollection<PageItem> Pages { get; } = [];
    public ObservableCollection<FormFieldItem> FormFields { get; } = [];
    public ObservableCollection<RecentFileItem> RecentFiles { get; } = [];
    public bool HasRecentFiles => RecentFiles.Count > 0;
    public event Action<string>? DocumentOpened;
    public void SetRecentFiles(IEnumerable<string> paths)
    {
        RecentFiles.Clear();
        foreach (var path in paths) RecentFiles.Add(new(path));
        OnPropertyChanged(nameof(HasRecentFiles));
    }
    public const int OverviewBatchSize = 48;
    private int _overviewStart;
    private IReadOnlyList<PageItem> _overviewPages = [];
    public IReadOnlyList<PageItem> OverviewPages => _overviewPages;
    public string OverviewLabel => $"Pages {_overviewStart + 1}–{Math.Min(Pages.Count, _overviewStart + OverviewBatchSize)} of {Pages.Count}";
    public bool HasPreviousOverview => _overviewStart > 0;
    public bool HasNextOverview => _overviewStart + OverviewBatchSize < Pages.Count;
    public bool HasOverviewBatches => Pages.Count > OverviewBatchSize;
    public void ChangeOverview(int delta)
    {
        _overviewStart = Math.Clamp(_overviewStart + delta * OverviewBatchSize, 0, Math.Max(0, (Pages.Count - 1) / OverviewBatchSize * OverviewBatchSize));
        NotifyOverview();
    }
    private void NotifyOverview()
    {
        var pages = Pages.Skip(_overviewStart).Take(OverviewBatchSize).ToArray();
        if (!_overviewPages.SequenceEqual(pages)) { _overviewPages = pages; OnPropertyChanged(nameof(OverviewPages)); }
        OnPropertyChanged(nameof(SelectedPage));
        foreach (var name in new[] { nameof(OverviewLabel), nameof(HasPreviousOverview), nameof(HasNextOverview), nameof(HasOverviewBatches) }) OnPropertyChanged(name);
    }
    public static string[] ZoomOptions { get; } = ["Fit page", "Fit width", "50%", "75%", "100%", "125%", "150%", "200%"];
    public string[] ZoomChoices => ZoomOptions;
    public static bool IsZoomChoice(string? choice) => choice is not null && ZoomOptions.Contains(choice);
    private Avalonia.Size _viewport;
    private bool _applyingFit;
    private int _renderedZoom;
    private int _renderedPage;
    public event Action? PreviewRefreshRequested;
    public double[] FontSizes { get; } = [10, 12, 16, 20, 24, 36, 48];
    public event Action? SurfaceChanged;
    public event Action? ExistingEditCheckRequested;
    public IReadOnlyList<PdfEditableTextLine> ExistingTextLines { get; private set; } = [];
    [ObservableProperty] private PdfEditableTextLine? _selectedExistingLine;
    [ObservableProperty] private string _existingDraftText = "";
    [ObservableProperty] private double _existingDraftSize = 12;
    [ObservableProperty] private bool _allowExistingOverflow;
    [ObservableProperty] private bool _isCheckingExistingEdit;
    [ObservableProperty] private string _existingEditMessage = "";
    [ObservableProperty] private string _existingLinesStatus = "";
    private byte[]? _existingFontBytes;
    private string? _chosenExistingFontName;
    private PdfTextEditCheck? _existingCheck;
    private int _existingCheckVersion;
    private bool _loadingExistingDraft;
    public bool HasExistingSelection => IsTextMode && SelectedExistingLine is not null;
    public bool HasInspectorSelection => HasSelection || HasExistingSelection;
    public bool CanEditExisting => SelectedExistingLine?.CanEdit == true;
    public bool HasChosenExistingFont => _existingFontBytes is { Length: > 0 };
    public bool HasExistingDraft => SelectedExistingLine is { } line && (ExistingDraftText != line.Text || ExistingDraftSize != line.FontSize || HasChosenExistingFont);
    public bool HasExistingEditMessage => ExistingEditMessage.Length > 0;
    public bool ExistingWouldOverflow => _existingCheck?.WouldOverflow == true;
    public bool CanApplyExisting => CanEditExisting && HasExistingDraft && !IsCheckingExistingEdit && _existingCheck?.CanApply == true;
    public string ExistingFontDescription => HasChosenExistingFont ? $"{_chosenExistingFontName} · will be embedded automatically" : SelectedExistingLine is { } line
        ? line.FontName + (line.IsFontSubset ? " · embedded subset, with limited character coverage" : line.IsFontEmbedded ? " · embedded font" : " · original PDF font") : "";

    partial void OnSelectedExistingLineChanged(PdfEditableTextLine? value)
    {
        _loadingExistingDraft = true;
        if (value is not null) SelectedTextId = null;
        _existingFontBytes = null; _chosenExistingFontName = null;
        ExistingDraftText = value?.Text ?? ""; ExistingDraftSize = value?.FontSize ?? 12; AllowExistingOverflow = false;
        _loadingExistingDraft = false;
        RequestExistingCheck(); NotifyExistingState(); SurfaceChanged?.Invoke();
    }
    partial void OnExistingDraftTextChanged(string value) => RequestExistingCheck();
    partial void OnExistingDraftSizeChanged(double value) => RequestExistingCheck();
    partial void OnAllowExistingOverflowChanged(bool value) => RequestExistingCheck();
    partial void OnIsCheckingExistingEditChanged(bool value) => OnPropertyChanged(nameof(CanApplyExisting));
    partial void OnExistingEditMessageChanged(string value) => OnPropertyChanged(nameof(HasExistingEditMessage));
    partial void OnExistingLinesStatusChanged(string value) => OnPropertyChanged(nameof(Hint));
    private void RequestExistingCheck()
    {
        if (_loadingExistingDraft) return;
        _existingCheckVersion++; _existingCheck = null;
        ExistingEditMessage = SelectedExistingLine?.UnsupportedReason ?? "";
        IsCheckingExistingEdit = CanEditExisting;
        NotifyExistingState();
        if (CanEditExisting) ExistingEditCheckRequested?.Invoke();
    }
    public void ChooseExistingFont(byte[] bytes, string name)
    {
        _existingFontBytes = bytes; _chosenExistingFontName = name;
        RequestExistingCheck();
    }
    public void DiscardExistingDraft() => OnSelectedExistingLineChanged(SelectedExistingLine);
    public void UseOriginalExistingFont()
    {
        _existingFontBytes = null; _chosenExistingFontName = null;
        RequestExistingCheck();
    }
    private PdfTextReplacementOptions ExistingOptions() => new()
    {
        TrueTypeFontData = _existingFontBytes ?? ReadOnlyMemory<byte>.Empty,
        FontSize = ExistingDraftSize, AllowOverflow = AllowExistingOverflow
    };
    public async Task CheckExistingDraftAsync()
    {
        if (SelectedExistingLine is not { } line || Document is not { } document || !CanEditExisting) return;
        var version = _existingCheckVersion; var revision = document.RevisionToken;
        var text = ExistingDraftText; var options = ExistingOptions();
        try
        {
            var check = await Task.Run(() => document.CheckExistingTextAsync(line, text, options));
            if (version != _existingCheckVersion || !ReferenceEquals(Document, document) || !ReferenceEquals(revision, document.RevisionToken) || _disposed) return;
            _existingCheck = check; ExistingEditMessage = check.Message ?? "";
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (version == _existingCheckVersion && !_disposed) ExistingEditMessage = ex.Message;
        }
        finally
        {
            if (version == _existingCheckVersion && !_disposed) { IsCheckingExistingEdit = false; NotifyExistingState(); }
        }
    }
    private void NotifyExistingState()
    {
        foreach (var property in new[] { nameof(HasExistingSelection), nameof(HasInspectorSelection), nameof(CanEditExisting), nameof(HasChosenExistingFont), nameof(HasExistingDraft), nameof(CanApplyExisting), nameof(ExistingFontDescription), nameof(ExistingWouldOverflow), nameof(Title) }) OnPropertyChanged(property);
    }
    private static async Task<(IReadOnlyList<PdfEditableTextLine> Lines, string Message)> ReadExistingLinesAsync(EditorDocument document, int page)
    {
        try
        {
            var lines = await document.GetExistingTextLinesAsync(page);
            return (lines, lines.Count == 0 ? "No editable page text was found. You can still add text." : "Select an existing line or text you have added.");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return ([], "Existing-text editing is unavailable on this page. You can still add text.");
        }
    }

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "Open a PDF to get started.";
    [ObservableProperty] private string _error = "";
    [ObservableProperty] private string _fileName = "No document open";
    [ObservableProperty] private string _mode = "Text";
    [ObservableProperty] private bool _addingText;
    [ObservableProperty] private PageItem? _selectedPage;
    [ObservableProperty] private int _zoom = 100;
    [ObservableProperty] private string _zoomChoice = "Fit page";
    [ObservableProperty] private string _draftText = "";
    [ObservableProperty] private double _draftSize = 16;
    [ObservableProperty] private Guid? _selectedTextId;
    [ObservableProperty] private string _formDraft = "";
    [ObservableProperty] private FormFieldItem? _selectedField;
    public Bitmap? PageBitmap { get; private set; }
    public PdfPageGeometry? Geometry { get; private set; }
    public double ViewScale => Zoom / 100d * 96 / 72;
    public double PageWidth => (Geometry?.DisplayWidth ?? 0) * ViewScale;
    public double PageHeight => (Geometry?.DisplayHeight ?? 0) * ViewScale;
    public string ZoomLabel => $"{Zoom}%";
    public bool PreviewNeedsRefresh => HasDocument && !IsPagesMode && (_renderedZoom != Zoom || _renderedPage != CurrentPage);
    public bool HasDocument => Document is not null;
    public bool IsReady => HasDocument && !IsBusy;
    public bool CanOpen => !IsBusy;
    public bool IsTextMode => Mode == "Text";
    public bool IsSelectTool => !AddingText;
    public bool IsPagesMode => Mode == "Pages";
    public bool IsFormsMode => Mode == "Forms";
    public bool HasSelection => SelectedTextId is not null && IsTextMode;
    public bool HasField => SelectedField is not null;
    public bool NoFields => FormFields.Count == 0;
    public bool CanEditField => SelectedField is { IsReadOnly: false };
    public bool HasError => Error.Length > 0;
    public bool CanUndo => Document?.CanUndo == true;
    public bool CanRedo => Document?.CanRedo == true;
    public bool CanDeletePage => HasDocument && Pages.Count > 1;
    public bool CanMoveEarlier => HasDocument && CurrentPage > 1;
    public bool CanMoveLater => HasDocument && CurrentPage < Pages.Count;
    public int CurrentPage => SelectedPage?.Number ?? 1;
    public string PageLabel => HasDocument ? $"Page {CurrentPage} of {Pages.Count}" : "Ready";
    public string Title => $"{(HasDocument ? FileName + (IsDirty ? " •" : "") + " — " : "")}ZingPDF Editor";
    public bool IsDirty => Document?.IsDirty == true || HasTextDraft || HasFormDraft || HasExistingDraft;
    public bool HasTextDraft => SelectedTextId is { } id && Document?.AddedText.FirstOrDefault(t => t.Id == id) is { } item && (DraftText != item.Text || DraftSize != item.Size);
    public bool HasFormDraft => _formDrafts.Count > 0;
    public string Hint => IsPagesMode ? "Drag pages to reorder them. Select a page for more actions." : IsFormsMode ? "Choose a text field on the right. Changes appear on the page when applied." : AddingText ? "Click on the page to add text, or press Enter to place it in the centre." : ExistingLinesStatus;

    partial void OnIsBusyChanged(bool value) => NotifyState();
    partial void OnErrorChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnModeChanged(string value) { AddingText = false; NotifyState(); NotifyExistingState(); if (IsTextMode && SelectedExistingLine is not null) RequestExistingCheck(); SurfaceChanged?.Invoke(); }
    partial void OnAddingTextChanged(bool value) { OnPropertyChanged(nameof(IsSelectTool)); OnPropertyChanged(nameof(Hint)); SurfaceChanged?.Invoke(); }
    partial void OnSelectedPageChanged(PageItem? value)
    {
        if (value is not null && (value.Number <= _overviewStart || value.Number > _overviewStart + OverviewBatchSize))
        {
            _overviewStart = (value.Number - 1) / OverviewBatchSize * OverviewBatchSize;
            NotifyOverview();
        }
        NotifyState();
    }
    partial void OnZoomChanged(int value)
    {
        if (!_applyingFit) ZoomChoice = $"{value}%";
        OnPropertyChanged(nameof(PageWidth)); OnPropertyChanged(nameof(PageHeight)); OnPropertyChanged(nameof(ZoomLabel));
        SurfaceChanged?.Invoke();
        PreviewRefreshRequested?.Invoke();
    }
    partial void OnZoomChoiceChanged(string value)
    {
        if (int.TryParse(value.TrimEnd('%'), out var percent)) Zoom = Math.Clamp(percent, 1, 400);
        else UpdateFitZoom();
    }

    public void UpdateViewport(Avalonia.Size size)
    {
        if (_viewport == size) return;
        _viewport = size;
        UpdateFitZoom();
    }

    private int CalculateZoom(PdfPageGeometry geometry)
    {
        if (ZoomChoice is not ("Fit page" or "Fit width") || _viewport.Width <= 0 || _viewport.Height <= 0) return Zoom;
        // Page margins (48 x 38) plus a reserved 16px scrollbar gutter on each axis.
        // Use the viewer's outer bounds so scrollbars appearing cannot cause fit oscillation.
        var widthScale = Math.Max(1, _viewport.Width - 64) / geometry.DisplayWidth;
        var heightScale = Math.Max(1, _viewport.Height - 54) / geometry.DisplayHeight;
        var scale = ZoomChoice == "Fit width" ? widthScale : Math.Min(widthScale, heightScale);
        return Math.Clamp((int)Math.Floor(scale * 72 / 96 * 100), 1, 400);
    }

    private void UpdateFitZoom()
    {
        if (Geometry is not { } geometry) return;
        _applyingFit = true;
        try { Zoom = CalculateZoom(geometry); }
        finally { _applyingFit = false; }
    }

    private async Task<(PdfPageRenderResult Render, int Zoom)> RenderPageAsync(EditorDocument document, int number)
    {
        var geometry = await document.GetGeometryAsync(number);
        var zoom = CalculateZoom(geometry);
        return (await document.RenderAsync(number, zoom / 100d * 96 / 72 * 1.5), zoom);
    }

    public async Task RefreshPreviewAsync()
    {
        if (Document is not { } document) return;
        var result = await Task.Run(async () => (Preview: await RenderPageAsync(document, CurrentPage), Lines: await ReadExistingLinesAsync(document, CurrentPage)));
        ExistingTextLines = result.Lines.Lines; ExistingLinesStatus = result.Lines.Message;
        SetRender(result.Preview.Render, result.Preview.Zoom);
    }
    partial void OnSelectedTextIdChanged(Guid? value) { if (value is not null) SelectedExistingLine = null; OnPropertyChanged(nameof(HasSelection)); OnPropertyChanged(nameof(HasInspectorSelection)); SurfaceChanged?.Invoke(); }
    partial void OnDraftTextChanged(string value) => OnPropertyChanged(nameof(Title));
    partial void OnDraftSizeChanged(double value) => OnPropertyChanged(nameof(Title));
    partial void OnFormDraftChanged(string value)
    {
        if (SelectedField is { IsReadOnly: false } selected)
        {
            if (value == selected.Value) _formDrafts.Remove(selected.Name);
            else _formDrafts[selected.Name] = value;
        }
        OnPropertyChanged(nameof(Title));
    }
    partial void OnSelectedFieldChanged(FormFieldItem? value)
    {
        FormDraft = value is not null && _formDrafts.TryGetValue(value.Name, out var draft) ? draft : value?.Value ?? "";
        OnPropertyChanged(nameof(HasField)); OnPropertyChanged(nameof(CanEditField));
    }

    public async Task<bool> RunAsync(Func<Task> operation)
    {
        if (IsBusy) return false;
        IsBusy = true; Error = "";
        try { await operation(); return true; }
        catch (Exception ex) { Error = FriendlyError(ex); Status = "The operation could not be completed."; return false; }
        finally { IsBusy = false; NotifyState(); }
    }

    private static string FriendlyError(Exception ex) => ex switch
    {
        UnauthorizedAccessException => "The file cannot be accessed. Choose a folder you can write to, or close the file in another application.",
        IOException => "The file could not be read or written. Check that it is available and not locked by another application.",
        NotSupportedException or ArgumentException or InvalidOperationException => ex.Message,
        _ => "This PDF could not be processed. It may be damaged, password protected, or use an unsupported feature. Your original file is unchanged."
    };

    public async Task<bool> OpenAsync(string path) => await RunAsync(async () =>
    {
        Status = "Opening PDF…";
        var candidate = await Task.Run(async () =>
        {
            var document = await EditorDocument.OpenAsync(path);
            var render = await RenderPageAsync(document, 1);
            var fields = await document.GetFormFieldsAsync();
            var lines = await ReadExistingLinesAsync(document, 1);
            return (document, render, fields, lines);
        });
        DisposePages();
        Document = candidate.document;
        FileName = System.IO.Path.GetFileName(path);
        SelectedTextId = null;
        SelectedExistingLine = null;
        ExistingTextLines = candidate.lines.Lines; ExistingLinesStatus = candidate.lines.Message;
        _formDrafts.Clear();
        FormFields.Clear();
        foreach (var field in candidate.fields) FormFields.Add(new(field));
        SelectedField = null;
        for (var n = 1; n <= Document.PageCount; n++) Pages.Add(new(n));
        SelectedPage = Pages[0];
        _overviewStart = 0; NotifyOverview();
        SetRender(candidate.render.Render, candidate.render.Zoom);
        Status = "Original file unchanged · edits stay on this computer";
        NotifyState();
        DocumentOpened?.Invoke(Document.SourcePath);
    });

    public async Task RefreshAsync(bool resetThumbnails = false)
    {
        if (Document is null) return;
        var number = Math.Clamp(CurrentPage, 1, Document.PageCount);
        var result = await Task.Run(async () => (Preview: IsPagesMode ? ((PdfPageRenderResult Render, int Zoom)?)null : await RenderPageAsync(Document, number), Fields: await Document.GetFormFieldsAsync(), Lines: IsPagesMode ? ((IReadOnlyList<PdfEditableTextLine>, string))([], "") : await ReadExistingLinesAsync(Document, number)));
        if (resetThumbnails || Pages.Count != Document.PageCount)
        {
            // Keep existing containers and previews on screen while replacements render.
            // Clearing the list also resets selection and scroll position between edits.
            while (Pages.Count > Document.PageCount)
            {
                var last = Pages[^1];
                Pages.RemoveAt(Pages.Count - 1);
                _cachedThumbnails.Remove(last); _thumbnailRevisions.Remove(last.Number);
                last.Thumbnail?.Dispose();
            }
            while (Pages.Count < Document.PageCount) Pages.Add(new(Pages.Count + 1));
            SelectedPage = Pages[number - 1];
            InvalidateThumbnails();
            NotifyOverview();
        }
        if (result.Preview is { } preview) SetRender(preview.Render, preview.Zoom);
        else _renderedPage = 0;
        ExistingTextLines = result.Lines.Item1; ExistingLinesStatus = result.Lines.Item2;
        if (SelectedExistingLine is { } selectedExisting)
        {
            var replacement = ExistingTextLines.FirstOrDefault(l => l.Id == selectedExisting.Id)
                ?? ExistingTextLines.Where(l => Math.Abs(l.BaselineOrigin.X - selectedExisting.BaselineOrigin.X) < .01 && Math.Abs(l.BaselineOrigin.Y - selectedExisting.BaselineOrigin.Y) < .01)
                    .OrderBy(l => l.Text == ExistingDraftText ? 0 : 1).FirstOrDefault();
            SelectedExistingLine = replacement;
        }
        SurfaceChanged?.Invoke();
        var fieldName = SelectedField?.Name;
        FormFields.Clear();
        foreach (var field in result.Fields) FormFields.Add(new(field));
        SelectedField = FormFields.FirstOrDefault(f => f.Name == fieldName);
        if (SelectedTextId is { } id)
        {
            if (Document.AddedText.FirstOrDefault(t => t.Id == id && t.PageNumber == number) is { } t) SetTextSelection(t);
            else SelectedTextId = null;
        }
        Status = Document.IsDirty ? "Unsaved changes · original file unchanged" : "Original file unchanged";
        NotifyState();
    }

    private void SetRender(PdfPageRenderResult render, int renderedZoom)
    {
        using var stream = new MemoryStream(render.PngBytes.ToArray());
        var bitmap = new Bitmap(stream);
        var previous = PageBitmap;
        PageBitmap = bitmap;
        Geometry = render.Geometry;
        _renderedZoom = renderedZoom;
        _renderedPage = render.PageNumber;
        UpdateFitZoom();
        var pageItem = Pages.FirstOrDefault(p => p.Number == render.PageNumber);
        if (pageItem is not null)
        {
            using var thumbStream = new MemoryStream(render.PngBytes.ToArray());
            var thumb = Bitmap.DecodeToWidth(thumbStream, 130);
            SetThumbnail(pageItem, thumb, Document!.RevisionToken);
        }
        previous?.Dispose();
        OnPropertyChanged(nameof(PageWidth)); OnPropertyChanged(nameof(PageHeight));
        SurfaceChanged?.Invoke();
    }

    private readonly Dictionary<int, object> _thumbnailRevisions = [];
    private readonly HashSet<PageItem> _cachedThumbnails = [];
    private bool _disposed;
    public bool NeedsThumbnail(int number) => Document is { } document && number >= 1 && number <= Pages.Count &&
        (!_thumbnailRevisions.TryGetValue(number, out var revision) || !ReferenceEquals(revision, document.RevisionToken));

    private void InvalidateThumbnails() => _thumbnailRevisions.Clear();

    // Only the visible cards are queued. Foreground edits stay responsive; each rendering
    // call captures an immutable revision, and results from old revisions are discarded.
    public async Task LoadVisibleThumbnailsAsync(IEnumerable<int> visibleNumbers)
    {
        if (Document is not { } document) return;
        var revision = document.RevisionToken;
        var numbers = visibleNumbers.Distinct().Where(NeedsThumbnail).Take(8).ToArray();
        foreach (var number in numbers)
        {
            if (_disposed || !ReferenceEquals(Document, document) || !ReferenceEquals(revision, document.RevisionToken)) return;
            var render = await Task.Run(async () =>
            {
                var geometry = await document.GetGeometryAsync(number);
                return await document.RenderAsync(number, 180 / Math.Max(geometry.DisplayWidth, geometry.DisplayHeight));
            });
            if (_disposed || !ReferenceEquals(Document, document) || !ReferenceEquals(revision, document.RevisionToken)) return;
            using var stream = new MemoryStream(render.PngBytes.ToArray());
            SetThumbnail(Pages[number - 1], new Bitmap(stream), revision, numbers);
        }
    }

    private void SetThumbnail(PageItem page, Bitmap thumbnail, object revision, IReadOnlyCollection<int>? protectedPages = null)
    {
        var previous = page.Thumbnail;
        page.Thumbnail = thumbnail;
        page.LastThumbnailUse = Environment.TickCount64;
        _cachedThumbnails.Add(page);
        _thumbnailRevisions[page.Number] = revision;
        previous?.Dispose();
        while (_cachedThumbnails.Count > 64)
        {
            var evicted = _cachedThumbnails.Where(p => p.Number != CurrentPage && protectedPages?.Contains(p.Number) != true)
                .MinBy(p => p.LastThumbnailUse);
            if (evicted is null) break;
            _cachedThumbnails.Remove(evicted); _thumbnailRevisions.Remove(evicted.Number);
            var old = evicted.Thumbnail; evicted.Thumbnail = null; old?.Dispose();
        }
    }

    public void ApplyDrafts()
    {
        if (HasTextDraft && Document is { } doc && SelectedTextId is { } id) doc.UpdateText(id, DraftText, DraftSize);
    }

    public async Task ApplyAllDraftsAsync()
    {
        if (HasExistingDraft && Document is { } existingDocument && SelectedExistingLine is { } existingLine)
        {
            var text = ExistingDraftText; var options = ExistingOptions();
            await Task.Run(() => existingDocument.ReplaceExistingTextAsync(existingLine, text, options));
            // A later operation (for example saving to an unwritable destination) can fail.
            // Commit the inspector and preview now so the applied line never retains a stale handle.
            await RefreshAsync();
        }
        ApplyDrafts();
        if (HasFormDraft && Document is { } doc)
        {
            foreach (var draft in _formDrafts.ToArray())
            {
                await Task.Run(() => doc.SetFormFieldAsync(draft.Key, draft.Value));
                _formDrafts.Remove(draft.Key);
            }
        }
    }

    public async Task AddTextAtAsync(double x, double y)
    {
        if (Document is null) return;
        await RunAsync(async () =>
        {
            await ApplyAllDraftsAsync();
            var id = await Task.Run(() => Document.AddTextAsync(CurrentPage, x, y));
            AddingText = false;
            SetTextSelection(Document.AddedText.Single(t => t.Id == id));
            await RefreshAsync();
        });
    }

    public void SetTextSelection(TextAddition text)
    {
        SelectedTextId = text.Id;
        DraftText = text.Text;
        DraftSize = text.Size;
        NotifyState();
    }

    public IReadOnlyList<TextBoxBounds> GetTextBounds()
    {
        if (Geometry is not { } geometry || Document is null) return [];
        var result = new List<TextBoxBounds>();
        foreach (var text in Document.AddedText.Where(t => t.PageNumber == CurrentPage))
        {
            var lines = text.Text.Split('\n');
            // AFM metrics describe the same Helvetica glyphs used in the saved content stream.
            var width = Math.Max(24, lines.Max(line => Metrics.MeasureText(line, "Helvetica", text.Size)) + 4);
            var height = text.Size * (1.25 * (lines.Length - 1) + 1.1);
            var angle = text.OrientationDegrees * Math.PI / 180;
            var cos = Math.Round(Math.Cos(angle)); var sin = Math.Round(Math.Sin(angle));
            var corners = new[] { (0d, 0d), (width, 0d), (0d, height), (width, height) }
                .Select(p => geometry.PageToDisplay(new Coordinate(text.X + cos * p.Item1 + sin * p.Item2, text.Y + sin * p.Item1 - cos * p.Item2))).ToArray();
            var left = corners.Min(p => p.X); var top = corners.Min(p => p.Y);
            result.Add(new(text.Id, new Rect(left * ViewScale, top * ViewScale, (corners.Max(p => p.X) - left) * ViewScale, (corners.Max(p => p.Y) - top) * ViewScale), geometry.PageToDisplay(new Coordinate(text.X, text.Y))));
        }
        return result;
    }

    public IReadOnlyList<ExistingLineBounds> GetExistingTextBounds()
    {
        if (Geometry is not { } geometry) return [];
        return ExistingTextLines.Select(line =>
        {
            var b = line.Bounds;
            var corners = new[] { b.LowerLeft, b.UpperRight, new Coordinate(b.LowerLeft.X, b.UpperRight.Y), new Coordinate(b.UpperRight.X, b.LowerLeft.Y) }
                .Select(geometry.PageToDisplay).ToArray();
            var left = corners.Min(p => p.X) * ViewScale; var top = corners.Min(p => p.Y) * ViewScale;
            return new ExistingLineBounds(line, new Rect(left, top, Math.Max(2, corners.Max(p => p.X) * ViewScale - left), Math.Max(2, corners.Max(p => p.Y) * ViewScale - top)));
        }).ToArray();
    }

    public void NotifyState()
    {
        foreach (var name in new[] { nameof(HasDocument), nameof(IsReady), nameof(CanOpen), nameof(CanUndo), nameof(CanRedo), nameof(CanDeletePage), nameof(CanMoveEarlier), nameof(CanMoveLater), nameof(PageLabel), nameof(Title), nameof(HasSelection), nameof(IsTextMode), nameof(IsPagesMode), nameof(IsFormsMode), nameof(NoFields), nameof(Hint) }) OnPropertyChanged(name);
    }

    private void DisposePages() { foreach (var p in Pages) p.Thumbnail?.Dispose(); Pages.Clear(); _cachedThumbnails.Clear(); _thumbnailRevisions.Clear(); }
    public void Dispose() { _disposed = true; PageBitmap?.Dispose(); PageBitmap = null; DisposePages(); }
}

public sealed partial class PageItem(int number) : ObservableObject
{
    public int Number { get; } = number;
    public string Label => $"Page {Number}";
    internal long LastThumbnailUse { get; set; }
    [ObservableProperty] private Bitmap? _thumbnail;
    public bool NeedsPreview => Thumbnail is null;
    partial void OnThumbnailChanged(Bitmap? value) => OnPropertyChanged(nameof(NeedsPreview));
}

public sealed class FormFieldItem(EditorFormField item)
{
    public string Name => item.Name;
    public string Label => item.Label;
    public string Value => item.Value;
    public bool IsReadOnly => item.IsReadOnly;
}

public sealed record TextBoxBounds(Guid Id, Rect Bounds, Coordinate DisplayAnchor);
public sealed record ExistingLineBounds(PdfEditableTextLine Line, Rect Bounds);
