using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ZingPDF.Editor;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();
    private bool _allowClose;
    private bool _confirmingClose;
    private int _shownPage = 1;
    private readonly DispatcherTimer _zoomTimer = new() { Interval = TimeSpan.FromMilliseconds(120) };
    private bool _closed;
    private readonly EditorPreferences _preferences;
    private readonly DispatcherTimer _thumbnailTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private bool _thumbnailsRunning;
    private object? _failedThumbnailRevision;
    private static readonly FilePickerFileType PdfType = new("PDF documents") { Patterns = ["*.pdf"], MimeTypes = ["application/pdf"] };

    public MainWindow() : this(null) { }
    public MainWindow(string? initialPath, string? preferencesPath = null, IEditorFontSource? fontSource = null)
    {
        InitializeComponent();
        _preferences = new EditorPreferences(preferencesPath ?? EditorPreferences.DefaultPath);
        ViewModel.ZoomChoice = _preferences.LoadZoomChoice();
        ViewModel.SetRecentFiles(_preferences.LoadRecentFiles());
        ViewModel.DocumentOpened += path => { _shownPage = 1; _failedThumbnailRevision = null; DocumentViewer.Offset = default; RecordRecentFile(path); };
        DataContext = ViewModel;
        ((Control)((Flyout)RecentFilesButton.Flyout!).Content!).DataContext = ViewModel;
        DocumentViewer.SizeChanged += (_, _) => UpdateViewport();
        ViewModel.PreviewRefreshRequested += ScheduleZoomPreview;
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.Mode) || e.PropertyName == nameof(MainViewModel.IsBusy) && ViewModel.IsBusy) PageOverview.CancelDrag();
            if (e.PropertyName == nameof(MainViewModel.ZoomChoice)) _preferences.SaveZoomChoice(ViewModel.ZoomChoice);
            if (e.PropertyName == nameof(MainViewModel.IsBusy))
            {
                Cursor = new Cursor(ViewModel.IsBusy ? StandardCursorType.Wait : StandardCursorType.Arrow);
                if (!ViewModel.IsBusy) ScheduleZoomPreview();
            }
            if (e.PropertyName == nameof(MainViewModel.Mode))
                Dispatcher.UIThread.Post(UpdateViewport, DispatcherPriority.Loaded);
        };
        _zoomTimer.Tick += RefreshZoomPreview;
        _thumbnailTimer.Tick += RefreshVisibleThumbnails;
        LayoutUpdated += (_, _) => ScheduleThumbnails();
        DocumentSurface.CommitDrafts = CommitDraftsAsync;
        DocumentSurface.TextPlaced += () => { TextEditor.Focus(); TextEditor.SelectAll(); };
        InitializeTextEditing(fontSource);
        PageOverview.PageMoveRequested += async (from, to) => await MovePageAsync(from, to);
        Closing += ClosingAsync;
        Closed += (_, _) => { _closed = true; _zoomTimer.Stop(); _thumbnailTimer.Stop(); ViewModel.Dispose(); };
        AddHandler(KeyDownEvent, ShortcutPressed, RoutingStrategies.Tunnel);
        Opened += async (_, _) => { if (initialPath == "--demo") OpenSampleClicked(this, new()); else if (initialPath is not null) await ViewModel.OpenAsync(initialPath); };
    }

    private void RecordRecentFile(string path)
    {
        _preferences.RecordFile(path);
        ViewModel.SetRecentFiles(_preferences.LoadRecentFiles());
    }

    private async void RecentFileClicked(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not string path || !await CanLeaveAsync()) return;
        RecentFilesButton.Flyout?.Hide();
        if (!await ViewModel.OpenAsync(path) && !File.Exists(path))
        {
            _preferences.RemoveFile(path);
            ViewModel.SetRecentFiles(_preferences.LoadRecentFiles());
            ViewModel.Error = "That recent PDF is no longer available. Choose Open PDF to find it in its new location.";
        }
    }

    private void ClearRecentClicked(object? sender, RoutedEventArgs e)
    {
        _preferences.ClearRecentFiles();
        ViewModel.SetRecentFiles([]);
        RecentFilesButton.Flyout?.Hide();
    }

    private void PreviousOverviewClicked(object? sender, RoutedEventArgs e) => ChangeOverview(-1);
    private void NextOverviewClicked(object? sender, RoutedEventArgs e) => ChangeOverview(1);
    private void ChangeOverview(int delta)
    {
        PageOverview.CancelDrag();
        ViewModel.ChangeOverview(delta);
        var viewer = PageOverview.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (viewer is not null) viewer.Offset = default;
    }

    private async void JumpPageClicked(object? sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsReady) return;
        if (await AskPageNumberAsync("Go to page") is { } page) await SelectPageAsync(page);
    }

    private async void MoveToClicked(object? sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsReady) return;
        var from = ViewModel.CurrentPage;
        if (await AskPageNumberAsync($"Move page {from} to", "Move") is { } to) await MovePageAsync(from, to);
    }

    private Task<int?> AskPageNumberAsync(string title, string actionText = "Go")
    {
        var dialog = new Window { Title = title, Width = 320, Height = 185, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var number = new NumericUpDown { Minimum = 1, Maximum = ViewModel.Pages.Count, Value = ViewModel.CurrentPage, FormatString = "0", Increment = 1 };
        var go = new Button { Content = actionText, IsDefault = true, Classes = { "primary" } };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        go.Click += (_, _) => dialog.Close((int?)number.Value);
        cancel.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { number, new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Children = { cancel, go } } } };
        return dialog.ShowDialog<int?>(this);
    }

    private int[] VisibleThumbnailNumbers()
    {
        var list = ViewModel.IsPagesMode ? (ListBox)PageOverview : PageList;
        if (!list.IsEffectivelyVisible) return [];
        var viewport = new Rect(list.Bounds.Size);
        return list.GetVisualDescendants().OfType<ListBoxItem>()
            .Where(item => item.DataContext is PageItem && item.TranslatePoint(default, list) is { } point && new Rect(point, item.Bounds.Size).Intersects(viewport))
            .Select(item => ((PageItem)item.DataContext!).Number).ToArray();
    }

    private void ScheduleThumbnails()
    {
        if (_closed || _thumbnailsRunning || !ViewModel.IsReady || ReferenceEquals(_failedThumbnailRevision, ViewModel.Document?.RevisionToken)) return;
        if (VisibleThumbnailNumbers().Any(ViewModel.NeedsThumbnail) && !_thumbnailTimer.IsEnabled) _thumbnailTimer.Start();
    }

    private async void RefreshVisibleThumbnails(object? sender, EventArgs e)
    {
        _thumbnailTimer.Stop();
        if (_closed || _thumbnailsRunning || !ViewModel.IsReady) return;
        _thumbnailsRunning = true;
        var revision = ViewModel.Document?.RevisionToken;
        try { await ViewModel.LoadVisibleThumbnailsAsync(VisibleThumbnailNumbers()); }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _failedThumbnailRevision = revision;
            if (!_closed) ViewModel.Status = "Some page previews are unavailable. Select a page to try opening it.";
        }
        finally { _thumbnailsRunning = false; ScheduleThumbnails(); }
    }

    private async void OpenSampleClicked(object? sender, RoutedEventArgs e)
    {
        if (!await CanLeaveAsync()) return;
        var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZingPDF Editor", "Samples", "Visitor pack.pdf");
        var ready = await ViewModel.RunAsync(async () =>
        {
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                await using var output = File.Create(path);
                await Core.SampleDocument.WriteAsync(output);
            }
        });
        if (ready && await ViewModel.OpenAsync(path)) _shownPage = 1;
    }

    private async Task<string?> PickPdfAsync(string title)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new() { Title = title, AllowMultiple = false, FileTypeFilter = [PdfType] });
        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    private async void OpenClicked(object? sender, RoutedEventArgs e)
    {
        if (!await CanLeaveAsync()) return;
        var path = await PickPdfAsync("Open a PDF");
        if (path is not null && await ViewModel.OpenAsync(path)) _shownPage = 1;
    }

    private async Task<bool> SaveAsync(bool singlePage = false)
    {
        var vm = ViewModel;
        if (vm.Document is null || vm.IsBusy) return false;
        var file = await StorageProvider.SaveFilePickerAsync(new()
        {
            Title = singlePage ? "Save selected page" : "Save a copy",
            SuggestedFileName = System.IO.Path.GetFileNameWithoutExtension(vm.FileName) + (singlePage ? $" - page {vm.CurrentPage}" : " - edited") + ".pdf",
            DefaultExtension = "pdf", FileTypeChoices = [PdfType], ShowOverwritePrompt = true
        });
        var path = file?.TryGetLocalPath();
        if (path is null) return false;
        return await vm.RunAsync(async () =>
        {
            await vm.ApplyAllDraftsAsync();
            await Task.Run(() => vm.Document.SaveCopyAsync(path, singlePage ? vm.CurrentPage : null));
            RecordRecentFile(path);
            await vm.RefreshAsync();
            vm.Status = $"Saved {System.IO.Path.GetFileName(path)} · original unchanged";
        });
    }

    private async void SaveClicked(object? sender, RoutedEventArgs e) => await SaveAsync();
    private async void ExtractClicked(object? sender, RoutedEventArgs e) => await SaveAsync(singlePage: true);

    private async Task CommitDraftsAsync()
    {
        if (ViewModel.HasTextDraft || ViewModel.HasFormDraft || ViewModel.HasExistingDraft)
            await ViewModel.RunAsync(async () => { await ViewModel.ApplyAllDraftsAsync(); await ViewModel.RefreshAsync(); });
    }

    private async void ModeClicked(object? sender, RoutedEventArgs e)
    {
        await CommitDraftsAsync();
        if (ViewModel.HasTextDraft || ViewModel.HasFormDraft || ViewModel.HasExistingDraft) return;
        if ((sender as Button)?.Tag is string mode) ViewModel.Mode = mode;
        if (ViewModel.PreviewNeedsRefresh) await ViewModel.RunAsync(ViewModel.RefreshPreviewAsync);
    }

    private async void SelectClicked(object? sender, RoutedEventArgs e) { await CommitDraftsAsync(); ViewModel.AddingText = false; }
    private async void AddTextClicked(object? sender, RoutedEventArgs e)
    {
        await CommitDraftsAsync();
        if (ViewModel.HasTextDraft || ViewModel.HasFormDraft || ViewModel.HasExistingDraft) return;
        ViewModel.SelectedExistingLine = null;
        ViewModel.Mode = "Text"; ViewModel.AddingText = true; DocumentSurface.Focus();
    }
    private async void ApplyClicked(object? sender, RoutedEventArgs e) => await CommitDraftsAsync();
    private void DismissClicked(object? sender, RoutedEventArgs e) => ViewModel.Error = "";

    private async Task EditAsync(Func<Core.EditorDocument, Task> action, bool structural = false)
    {
        if (ViewModel.Document is not { } doc) return;
        await ViewModel.RunAsync(async () => { await ViewModel.ApplyAllDraftsAsync(); await Task.Run(() => action(doc)); await ViewModel.RefreshAsync(structural); _shownPage = ViewModel.CurrentPage; });
    }

    private async void UndoClicked(object? sender, RoutedEventArgs e) => await EditAsync(d => d.UndoAsync(), true);
    private async void RedoClicked(object? sender, RoutedEventArgs e) => await EditAsync(d => d.RedoAsync(), true);
    private async void RotateClicked(object? sender, RoutedEventArgs e) => await EditAsync(d => d.RotateAsync(ViewModel.CurrentPage), true);
    private async void MoveEarlierClicked(object? sender, RoutedEventArgs e)
    {
        await MovePageAsync(ViewModel.CurrentPage, ViewModel.CurrentPage - 1);
    }
    private async void MoveLaterClicked(object? sender, RoutedEventArgs e)
    {
        await MovePageAsync(ViewModel.CurrentPage, ViewModel.CurrentPage + 1);
    }

    private async Task MovePageAsync(int from, int to)
    {
        var vm = ViewModel;
        if (!vm.IsReady || vm.Document is not { } doc || from == to || to < 1 || to > vm.Pages.Count) return;
        await vm.RunAsync(async () =>
        {
            await vm.ApplyAllDraftsAsync();
            await Task.Run(() => doc.MovePageAsync(from, to));
            vm.SelectedPage = vm.Pages[to - 1];
            vm.SelectedTextId = null;
            vm.SelectedExistingLine = null;
            await vm.RefreshAsync(resetThumbnails: true);
            _shownPage = vm.CurrentPage;
        });
    }
    private async void DeletePageClicked(object? sender, RoutedEventArgs e) => await EditAsync(d => d.DeletePageAsync(ViewModel.CurrentPage), true);
    private async void RemoveTextClicked(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTextId is not { } id) return;
        await EditAsync(d => { d.RemoveText(id); return Task.CompletedTask; });
    }
    private async void AppendClicked(object? sender, RoutedEventArgs e)
    {
        var path = await PickPdfAsync("Add pages from another PDF");
        if (path is not null) await EditAsync(d => d.AppendAsync(path), true);
    }

    private async Task SelectPageAsync(int number)
    {
        var vm = ViewModel;
        await vm.RunAsync(async () =>
        {
            await vm.ApplyAllDraftsAsync();
            vm.SelectedPage = vm.Pages[Math.Clamp(number - 1, 0, vm.Pages.Count - 1)];
            vm.SelectedTextId = null;
            vm.SelectedExistingLine = null;
            if (!vm.IsPagesMode) await vm.RefreshAsync();
            _shownPage = vm.CurrentPage;
        });
    }

    private async void PageSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(sender, PageOverview) && !ViewModel.IsBusy && PageOverview.SelectedItem is PageItem item) ViewModel.SelectedPage = item;
        if (ViewModel.IsBusy || !ViewModel.HasDocument || ViewModel.SelectedPage is null || ViewModel.CurrentPage == _shownPage) return;
        await SelectPageAsync(ViewModel.CurrentPage);
        if (ViewModel.HasError && _shownPage <= ViewModel.Pages.Count) ViewModel.SelectedPage = ViewModel.Pages[_shownPage - 1];
    }

    private void FormSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // MainViewModel keeps a draft for each field until Apply or Save.
    }

    private void UpdateViewport()
    {
        if (_closed || ViewModel.IsPagesMode) return;
        ViewModel.UpdateViewport(DocumentViewer.Bounds.Size);
        ScheduleZoomPreview();
    }

    private void ScheduleZoomPreview()
    {
        if (_closed || !ViewModel.PreviewNeedsRefresh) return;
        _zoomTimer.Stop();
        _zoomTimer.Start();
    }

    private async void RefreshZoomPreview(object? sender, EventArgs e)
    {
        _zoomTimer.Stop();
        if (_closed || !ViewModel.PreviewNeedsRefresh) return;
        if (ViewModel.IsBusy) { _zoomTimer.Start(); return; }
        // Zoom is a view operation: leave unapplied text/form drafts and undo history alone.
        if (!await ViewModel.RunAsync(ViewModel.RefreshPreviewAsync)) _zoomTimer.Stop();
    }

    private async void ShortcutPressed(object? sender, KeyEventArgs e)
    {
        // Keep the workspace visually enabled during work, while preventing keyboard edits.
        if (ViewModel.IsBusy) { e.Handled = true; return; }
        if (ViewModel.IsTextMode && ViewModel.IsReady && e.KeyModifiers == KeyModifiers.None && FocusManager?.GetFocusedElement() is not (TextBox or NumericUpDown or ComboBox))
        {
            if (e.Key == Key.V) { e.Handled = true; SelectClicked(this, new()); return; }
            if (e.Key == Key.T) { e.Handled = true; AddTextClicked(this, new()); return; }
        }
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) && !e.KeyModifiers.HasFlag(KeyModifiers.Meta)) return;
        if (e.Key is Key.Z or Key.Y && FocusManager?.GetFocusedElement() is TextBox) return;
        if (e.Key == Key.O) { e.Handled = true; OpenClicked(this, new()); }
        if (e.Key == Key.S) { e.Handled = true; await SaveAsync(); }
        if (e.Key == Key.Z && ViewModel.CanUndo && !e.KeyModifiers.HasFlag(KeyModifiers.Shift)) { e.Handled = true; await EditAsync(d => d.UndoAsync(), true); }
        if ((e.Key == Key.Y || e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Shift)) && ViewModel.CanRedo) { e.Handled = true; await EditAsync(d => d.RedoAsync(), true); }
    }

    private async Task<bool> CanLeaveAsync()
    {
        if (ViewModel.IsBusy) return false;
        if (!ViewModel.IsDirty) return true;
        var result = await AskUnsavedAsync();
        return result == "discard" || result == "save" && await SaveAsync();
    }

    private Task<string> AskUnsavedAsync()
    {
        var dialog = new Window { Title = "Save your changes?", Width = 450, Height = 205, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var save = new Button { Content = "Save a copy", Classes = { "primary" } };
        var discard = new Button { Content = "Discard changes" };
        var cancel = new Button { Content = "Cancel" };
        save.Click += (_, _) => dialog.Close("save"); discard.Click += (_, _) => dialog.Close("discard"); cancel.Click += (_, _) => dialog.Close("cancel");
        dialog.Content = new StackPanel { Margin = new Thickness(24), Spacing = 18, Children =
        {
            new TextBlock { Text = "Save your changes before continuing?", FontSize = 18 },
            new TextBlock { Text = "Your original PDF has not been changed." },
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { cancel, discard, save } }
        }};
        return dialog.ShowDialog<string>(this);
    }

    private async void ClosingAsync(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose) return;
        e.Cancel = true;
        if (_confirmingClose) return;
        _confirmingClose = true;
        try { if (await CanLeaveAsync()) { _allowClose = true; Close(); } }
        finally { _confirmingClose = false; }
    }
}
