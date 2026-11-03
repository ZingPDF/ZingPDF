using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ZingPDF.Editor;
using ZingPDF.Extensions;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(ZingPDF.Editor.Tests.TestAppBuilder))]

namespace ZingPDF.Editor.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

public sealed class EditorUiTests
{
    [AvaloniaFact]
    public async Task ExistingLine_FontWarningChooseAndEmbed_UndoSaveAndReopen_UsesRealPdf()
    {
        using var fixture = new EditorDocumentTests();
        var path = await fixture.CreatePdfAsync();
        var original = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        var fontBytes = await File.ReadAllBytesAsync(System.IO.Path.Combine(AppContext.BaseDirectory, "TestFonts", "NotoSans-Regular.ttf"), TestContext.Current.CancellationToken);
        var source = new TestEditorFontSource(fontBytes);
        var window = new MainWindow(null, System.IO.Path.ChangeExtension(path, ".preferences.json"), source); window.Show();
        var vm = window.ViewModel;
        Assert.True(await vm.OpenAsync(path), vm.Error); await WaitIdleAsync(vm);
        var surface = window.FindControl<PageSurface>("DocumentSurface")!;
        var line = Assert.Single(vm.GetExistingTextBounds());
        var click = surface.TranslatePoint(line.Bounds.Center, window)!.Value;
        window.MouseDown(click, MouseButton.Left); window.MouseUp(click, MouseButton.Left);
        Assert.NotNull(vm.SelectedExistingLine); Assert.True(vm.HasExistingSelection);
        var editor = window.FindControl<TextBox>("ExistingTextEditor")!;
        Assert.True(editor.IsEffectivelyVisible);
        editor.Focus(); editor.SelectAll(); window.KeyTextInput("Edited \u03A9");
        await WaitExistingCheckAsync(vm);
        Assert.True(vm.HasExistingDraft); Assert.Contains("\u03A9", vm.ExistingEditMessage); Assert.False(vm.CanApplyExisting);
        Click(window, "DiscardExistingTextButton"); await WaitExistingCheckAsync(vm);
        Assert.False(vm.HasExistingDraft); Assert.Equal("Original page 1", vm.ExistingDraftText);
        editor.Focus(); editor.SelectAll(); window.KeyTextInput("Edited \u03A9"); await WaitExistingCheckAsync(vm);
        await CaptureAsync(window, "font-coverage-warning");
        Click(window, "ChooseExistingFontButton");
        await Task.Delay(30, TestContext.Current.CancellationToken);
        var dialog = Assert.Single(window.OwnedWindows);
        dialog.GetVisualDescendants().OfType<AutoCompleteBox>().Single(c => c.Name == "GoogleFontFamily").Text = "Noto Sans";
        await CaptureAsync(dialog, "font-picker");
        dialog.GetVisualDescendants().OfType<Button>().Single(c => c.Name == "UseGoogleFontButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (window.OwnedWindows.Count > 0 && DateTime.UtcNow < deadline) { await Task.Delay(20, TestContext.Current.CancellationToken); Dispatcher.UIThread.RunJobs(); }
        Assert.Empty(window.OwnedWindows);
        await WaitExistingCheckAsync(vm);
        Assert.Equal("Noto Sans", source.Family); Assert.True(vm.HasChosenExistingFont); Assert.True(vm.CanApplyExisting, vm.ExistingEditMessage);
        Assert.Contains("embedded automatically", vm.ExistingFontDescription);
        Click(window, "ApplyExistingTextButton"); await WaitIdleAsync(vm);
        Assert.False(vm.HasExistingDraft); Assert.Equal("Edited \u03A9", vm.SelectedExistingLine!.Text);
        Assert.True(vm.SelectedExistingLine.IsFontEmbedded); Assert.Empty(vm.Document!.AddedText);
        await WaitExistingCheckAsync(vm);
        await CaptureAsync(window, "existing-line-edited");
        vm.ExistingDraftSize = 30; await WaitExistingCheckAsync(vm);
        Assert.True(vm.CanApplyExisting, vm.ExistingEditMessage);
        // Applying a size change refreshes the source handle even before a later save/action.
        Assert.True(await vm.RunAsync(vm.ApplyAllDraftsAsync), vm.Error);
        Assert.False(vm.HasExistingDraft); Assert.Equal(30, vm.SelectedExistingLine!.FontSize);
        vm.ExistingDraftText = "Final \u03A9"; await WaitExistingCheckAsync(vm);
        Assert.True(await vm.RunAsync(vm.ApplyAllDraftsAsync), vm.Error);
        Assert.Equal("Final \u03A9", vm.SelectedExistingLine!.Text);
        // Restore the first edit before the single-step undo/redo assertions below.
        await vm.Document.UndoAsync(); await vm.Document.UndoAsync(); await vm.RefreshAsync();
        var copy = System.IO.Path.ChangeExtension(path, ".edited.pdf");
        Assert.True(await vm.RunAsync(() => vm.Document.SaveCopyAsync(copy)), vm.Error);
        window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Undo").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitIdleAsync(vm);
        Assert.Equal("Original page 1", Assert.Single(vm.ExistingTextLines).Text);
        window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Redo").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitIdleAsync(vm);
        Assert.Equal("Edited \u03A9", Assert.Single(vm.ExistingTextLines).Text);
        Assert.True(await vm.OpenAsync(copy)); await WaitIdleAsync(vm);
        Assert.Equal("Edited \u03A9", Assert.Single(vm.ExistingTextLines).Text);
        Assert.Equal(original, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        window.Close();
    }

    private sealed class TestEditorFontSource(byte[] bytes) : IEditorFontSource
    {
        public string? Family { get; private set; }
        public Task<byte[]> GetFontAsync(string family, string variant, CancellationToken cancellationToken)
        { Family = family; return Task.FromResult(bytes); }
    }

    private static async Task WaitExistingCheckAsync(MainViewModel vm)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        do { await Task.Delay(20, TestContext.Current.CancellationToken); Dispatcher.UIThread.RunJobs(); }
        while (vm.IsCheckingExistingEdit && DateTime.UtcNow < deadline);
        Assert.False(vm.IsCheckingExistingEdit);
    }

    [AvaloniaFact]
    public async Task RecentFiles_AndToolSelector_WorkWithoutLeakingIntoTextInput()
    {
        using var fixture = new EditorDocumentTests();
        var path = await fixture.CreatePdfAsync();
        var settings = System.IO.Path.ChangeExtension(path, ".preferences.json");
        var preferences = new EditorPreferences(settings);
        preferences.SaveZoomChoice("Fit width");
        preferences.RecordFile(path); preferences.RecordFile(path);
        Assert.Single(preferences.LoadRecentFiles());
        Assert.Equal("Fit width", preferences.LoadZoomChoice());
        var window = new MainWindow(null, settings); window.Show();
        Assert.Single(window.ViewModel.RecentFiles);
        await CaptureAsync(window, "welcome-recent");
        var recent = window.GetVisualDescendants().OfType<Button>().Single(b => b.Tag as string == path);
        recent.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitIdleAsync(window.ViewModel);
        Assert.Equal(path, window.ViewModel.Document!.SourcePath);
        var select = window.FindControl<RadioButton>("SelectToolButton")!;
        var add = window.FindControl<RadioButton>("AddTextButton")!;
        Assert.True(select.IsChecked); Assert.False(add.IsChecked);
        window.FindControl<PageSurface>("DocumentSurface")!.Focus();
        window.KeyPressQwerty(PhysicalKey.T, RawInputModifiers.None);
        Assert.True(add.IsChecked); Assert.False(select.IsChecked);
        window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.None);
        Assert.True(select.IsChecked); Assert.False(add.IsChecked);
        await window.ViewModel.AddTextAtAsync(20, 40);
        var editor = window.FindControl<TextBox>("TextEditor")!; editor.Focus();
        window.KeyPressQwerty(PhysicalKey.T, RawInputModifiers.None); window.KeyTextInput("t");
        Assert.False(window.ViewModel.AddingText);
        await window.ViewModel.RunAsync(async () => { await window.ViewModel.ApplyAllDraftsAsync(); await window.ViewModel.Document.SaveCopyAsync(System.IO.Path.ChangeExtension(path, ".saved.pdf")); });
        window.Close();
        var reopened = new MainWindow(null, settings); reopened.Show();
        Dispatcher.UIThread.RunJobs();
        var recentButton = reopened.FindControl<Button>("RecentFilesButton")!;
        var recentPoint = recentButton.TranslatePoint(new Point(recentButton.Bounds.Width / 2, recentButton.Bounds.Height / 2), reopened)!.Value;
        reopened.MouseDown(recentPoint, MouseButton.Left); reopened.MouseUp(recentPoint, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(recentButton.Flyout!.IsOpen);
        var flyoutContent = (Control)((Flyout)recentButton.Flyout).Content!;
        Assert.Single(flyoutContent.GetVisualDescendants().OfType<Button>(), b => b.Tag as string == path);
        await CaptureAsync(reopened, "recent-menu");
        // Use the welcome-screen clear action; the same handler serves the Recent menu.
        reopened.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Clear list").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Empty(reopened.ViewModel.RecentFiles); Assert.Empty(preferences.LoadRecentFiles());
        Assert.Equal("Fit width", preferences.LoadZoomChoice()); Assert.True(File.Exists(path));
        for (var index = 0; index < 12; index++) preferences.RecordFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!, $"recent-{index}.pdf"));
        Assert.Equal(10, preferences.LoadRecentFiles().Length);
        Assert.EndsWith("recent-11.pdf", preferences.LoadRecentFiles()[0]);
        reopened.Close();
    }

    [AvaloniaFact]
    public async Task LargeDocument_PreviewsAreLazyBoundedAndBatchReorderingUsesActualPageNumbers()
    {
        using var fixture = new EditorDocumentTests();
        var path = await fixture.CreatePdfAsync(pages: 240);
        using (var model = new MainViewModel())
        {
            Assert.True(await model.OpenAsync(path), model.Error);
            Assert.Single(model.Pages, p => p.Thumbnail is not null); // No initial rasterization of every page.
            Assert.Equal(48, model.OverviewPages.Count);
            for (var page = 1; page <= 80; page += 8)
                await model.LoadVisibleThumbnailsAsync(Enumerable.Range(page, 8));
            Assert.InRange(model.Pages.Count(p => p.Thumbnail is not null), 1, 64);
            model.ChangeOverview(1);
            Assert.Equal(49, model.OverviewPages[0].Number);
            Assert.Equal(1, model.CurrentPage); // Paging the overview does not change the selected PDF page.
        }
        var window = new MainWindow(null, System.IO.Path.ChangeExtension(path, ".preferences.json")); window.Show();
        var vm = window.ViewModel;
        Assert.True(await vm.OpenAsync(path));
        Click(window, "PagesModeButton"); await WaitIdleAsync(vm);
        var overview = window.FindControl<PageReorderListBox>("PageOverview")!;
        Assert.Equal(48, overview.GetVisualDescendants().OfType<ListBoxItem>().Count());
        window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Next").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, vm.CurrentPage);
        var cards = overview.GetVisualDescendants().OfType<ListBoxItem>().OrderBy(c => ((PageItem)c.DataContext!).Number).ToArray();
        Assert.Equal(49, ((PageItem)cards[0].DataContext!).Number);
        var source = cards[0].TranslatePoint(new Point(cards[0].Bounds.Width / 2, cards[0].Bounds.Height / 2), window)!.Value;
        var target = cards[1].TranslatePoint(new Point(cards[1].Bounds.Width - 8, cards[1].Bounds.Height / 2), window)!.Value;
        window.MouseDown(source, MouseButton.Left); window.MouseMove(target); window.MouseUp(target, MouseButton.Left);
        await WaitIdleAsync(vm);
        Assert.Equal(50, vm.CurrentPage);
        Assert.Equal(48, overview.GetVisualDescendants().OfType<ListBoxItem>().Count());
        await CaptureAsync(window, "large-pages");
        Click(window, "JumpPageButton");
        await Task.Delay(30, TestContext.Current.CancellationToken);
        var jump = Assert.Single(window.OwnedWindows);
        jump.GetVisualDescendants().OfType<NumericUpDown>().Single().Value = 119;
        jump.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Go").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitIdleAsync(vm);
        Assert.Equal(119, vm.CurrentPage);
        Assert.Equal(97, vm.OverviewPages[0].Number);
        window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Move to…").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(30, TestContext.Current.CancellationToken);
        var moveDialog = Assert.Single(window.OwnedWindows);
        moveDialog.GetVisualDescendants().OfType<NumericUpDown>().Single().Value = 200;
        moveDialog.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Move").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitIdleAsync(vm);
        Assert.Equal(200, vm.CurrentPage);
        Click(window, "TextModeButton"); await WaitIdleAsync(vm);
        Assert.Equal(200, vm.Geometry!.PageNumber);
        var output = System.IO.Path.ChangeExtension(path, ".large-saved.pdf");
        await vm.Document!.SaveCopyAsync(output);
        using (var stream = File.OpenRead(output))
        using (var pdf = Pdf.Load(stream))
            Assert.Contains("Original page 49", string.Join(" ", (await pdf.ExtractTextAsync(50)).Select(t => t.Text)));
        window.Close();
    }

    [AvaloniaFact]
    public async Task PageDragging_InsertionLineIsCentredAndStableAcrossBothSidesOfGap()
    {
        using var fixture = new EditorDocumentTests();
        var path = await fixture.CreatePdfAsync(pages: 8);
        var window = new MainWindow(null, System.IO.Path.ChangeExtension(path, ".preferences.json")); window.Show();
        var vm = window.ViewModel;
        Assert.True(await vm.OpenAsync(path));
        Click(window, "PagesModeButton"); await WaitIdleAsync(vm);
        var overview = window.FindControl<PageReorderListBox>("PageOverview")!;
        var cards = overview.GetVisualDescendants().OfType<ListBoxItem>().OrderBy(c => ((PageItem)c.DataContext!).Number).ToArray();
        Rect BoundsInWindow(ListBoxItem item) => new(item.TranslatePoint(default, window)!.Value, item.Bounds.Size);
        var left = BoundsInWindow(cards[1]); var right = BoundsInWindow(cards[2]);
        var source = BoundsInWindow(cards[0]).Center;
        window.MouseDown(source, MouseButton.Left);

        async Task<(int Left, int Right)> ReadMarkerAsync(Point pointer, double rowY)
        {
            window.MouseMove(pointer);
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(30, TestContext.Current.CancellationToken);
            using var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            using var bytes = new MemoryStream();
            frame.Save(bytes, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            bytes.Position = 0;
            using var pixels = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes);
            var blueXs = Enumerable.Range(0, pixels.Width).Where(x =>
            {
                var pixel = pixels[x, (int)rowY];
                return pixel.R == 0x28 && pixel.G == 0x59 && pixel.B == 0xB8;
            }).ToArray();
            Assert.NotEmpty(blueXs);
            return (blueXs.Min(), blueXs.Max());
        }

        var fromLeft = await ReadMarkerAsync(new Point(left.Right - 1, left.Center.Y), right.Top + 20);
        var atMidpoint = await ReadMarkerAsync(new Point((left.Right + right.Left) / 2, left.Center.Y), right.Top + 20);
        var fromRight = await ReadMarkerAsync(new Point(right.Left + 1, right.Center.Y), right.Top + 20);
        Assert.Equal(fromLeft, atMidpoint); Assert.Equal(fromLeft, fromRight);
        Assert.InRange(Math.Abs((fromLeft.Left + fromLeft.Right + 1) / 2d - (left.Right + right.Left) / 2), 0, 1);
        await CaptureAsync(window, "indicator-centred");

        // A wrapped boundary has one canonical position at the next row's start.
        var rowEnd = BoundsInWindow(cards[5]); var rowStart = BoundsInWindow(cards[6]);
        var afterRowEnd = await ReadMarkerAsync(new Point(rowEnd.Right - 1, rowEnd.Center.Y), rowStart.Top + 20);
        var beforeRowStart = await ReadMarkerAsync(new Point(rowStart.Left + 1, rowStart.Center.Y), rowStart.Top + 20);
        Assert.Equal(afterRowEnd, beforeRowStart);
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        window.MouseUp(rowStart.Center, MouseButton.Left);
        Assert.False(vm.IsDirty);
        window.Close();
    }

    [AvaloniaFact]
    public async Task PageDragging_ReordersAcrossRows_PreservesSelectionAndSavedOrder_WithUndoRedo()
    {
        using var fixture = new EditorDocumentTests();
        var path = await fixture.CreatePdfAsync(pages: 8);
        var original = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        var window = new MainWindow(null, System.IO.Path.ChangeExtension(path, ".preferences.json")); window.Show();
        var vm = window.ViewModel;
        Assert.True(await vm.OpenAsync(path), vm.Error);
        Click(window, "PagesModeButton"); await WaitIdleAsync(vm);
        var overview = window.FindControl<PageReorderListBox>("PageOverview")!;
        var cards = overview.GetVisualDescendants().OfType<ListBoxItem>().OrderBy(c => ((PageItem)c.DataContext!).Number).ToArray();
        var source = cards[0].TranslatePoint(cards[0].Bounds.Center - cards[0].Bounds.Position, window)!.Value;
        var target = cards[6].TranslatePoint(new Point(cards[6].Bounds.Width - 10, cards[6].Bounds.Height / 2), window)!.Value;
        Assert.True(target.Y > source.Y); // The drag crosses a wrapped row.
        window.MouseDown(source, MouseButton.Left);
        window.MouseMove(target);
        await CaptureAsync(window, "dragging-page");
        Assert.False(vm.IsBusy); // Moving the pointer never mutates or renders the PDF.
        window.MouseUp(target, MouseButton.Left);
        await WaitIdleAsync(vm);
        Assert.Equal(7, vm.CurrentPage);
        Assert.True(vm.CanUndo);
        var saved = System.IO.Path.ChangeExtension(path, ".reordered.pdf");
        await vm.Document!.SaveCopyAsync(saved);
        using (var stream = File.OpenRead(saved))
        using (var pdf = Pdf.Load(stream))
        {
            Assert.Contains("Original page 2", string.Join(" ", (await pdf.ExtractTextAsync(1)).Select(t => t.Text)));
            Assert.Contains("Original page 1", string.Join(" ", (await pdf.ExtractTextAsync(7)).Select(t => t.Text)));
            Assert.Contains("Original page 8", string.Join(" ", (await pdf.ExtractTextAsync(8)).Select(t => t.Text)));
        }
        window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Undo").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitIdleAsync(vm);
        Assert.False(vm.CanUndo); Assert.True(vm.CanRedo); // One drag creates exactly one undo step.
        window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Redo").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitIdleAsync(vm);
        // Move it back, exercising insertion before a card as well as after one.
        var first = cards[0].TranslatePoint(new Point(10, cards[0].Bounds.Height / 2), window)!.Value;
        window.MouseDown(target, MouseButton.Left); window.MouseMove(first); window.MouseUp(first, MouseButton.Left);
        await WaitIdleAsync(vm);
        Assert.Equal(1, vm.CurrentPage);
        await vm.Document.SaveCopyAsync(saved);
        using (var stream = File.OpenRead(saved))
        using (var pdf = Pdf.Load(stream))
            Assert.Contains("Original page 1", string.Join(" ", (await pdf.ExtractTextAsync(1)).Select(t => t.Text)));
        Assert.Equal(original, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        await CaptureAsync(window, "pages-reordered");
        window.Close();
    }

    [AvaloniaFact]
    public async Task PageDragging_NoOpOutsideAndCancelledDrops_DoNotChangeDocument()
    {
        using var fixture = new EditorDocumentTests();
        var path = await fixture.CreatePdfAsync(pages: 15);
        var window = new MainWindow(null, System.IO.Path.ChangeExtension(path, ".preferences.json")); window.Show();
        var vm = window.ViewModel;
        Assert.True(await vm.OpenAsync(path));
        Click(window, "PagesModeButton"); await WaitIdleAsync(vm);
        var overview = window.FindControl<PageReorderListBox>("PageOverview")!;
        var card = overview.GetVisualDescendants().OfType<ListBoxItem>().First();
        var centre = card.TranslatePoint(new Point(card.Bounds.Width / 2, card.Bounds.Height / 2), window)!.Value;
        var ownEdge = card.TranslatePoint(new Point(card.Bounds.Width - 8, card.Bounds.Height / 2), window)!.Value;
        window.MouseDown(centre, MouseButton.Left); window.MouseMove(ownEdge); window.MouseUp(ownEdge, MouseButton.Left);
        Assert.False(vm.IsDirty); Assert.False(vm.CanUndo);
        var outside = new Point(window.ClientSize.Width + 20, 100);
        window.MouseDown(centre, MouseButton.Left); window.MouseMove(outside); window.MouseUp(outside, MouseButton.Left);
        Assert.False(vm.IsDirty); Assert.False(vm.CanUndo);
        window.MouseDown(centre, MouseButton.Left); window.MouseMove(ownEdge + new Vector(200, 0));
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        window.MouseUp(ownEdge + new Vector(200, 0), MouseButton.Left);
        Assert.Equal(1, card.Opacity); Assert.False(vm.IsDirty); Assert.False(vm.CanUndo);
        var scroll = overview.GetVisualDescendants().OfType<ScrollViewer>().First();
        var edge = overview.TranslatePoint(new Point(overview.Bounds.Width / 2, overview.Bounds.Height - 6), window)!.Value;
        window.MouseDown(centre, MouseButton.Left); window.MouseMove(edge);
        for (var n = 0; n < 8; n++) { await Task.Delay(30, TestContext.Current.CancellationToken); Dispatcher.UIThread.RunJobs(); }
        Assert.True(scroll.Offset.Y > 0);
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        window.MouseUp(edge, MouseButton.Left);
        Assert.False(vm.IsDirty); Assert.False(vm.CanUndo);
        window.Close();
    }

    [AvaloniaFact]
    public async Task FitModes_ResizeRotateAndNavigate_WhilePreservingDraftsAndRememberingChoice()
    {
        using var fixture = new EditorDocumentTests();
        var path = await fixture.CreatePdfAsync(rotation: 90, crop: true, forms: true, pages: 2);
        var preferencesPath = System.IO.Path.ChangeExtension(path, ".preferences.json");
        var window = new MainWindow(null, preferencesPath); window.Show();
        var vm = window.ViewModel;
        Assert.True(await vm.OpenAsync(path), vm.Error);
        await WaitIdleAsync(vm);
        Assert.Equal("Fit page", vm.ZoomChoice);
        var viewer = window.FindControl<ScrollViewer>("DocumentViewer")!;
        var picker = window.FindControl<ComboBox>("ZoomPicker")!;
        var surface = window.FindControl<PageSurface>("DocumentSurface")!;
        Assert.True(vm.PageWidth + 48 <= viewer.Viewport.Width);
        Assert.True(vm.PageHeight + 38 <= viewer.Viewport.Height);
        Assert.True(viewer.Extent.Width <= viewer.Viewport.Width + 1);
        Assert.True(viewer.Extent.Height <= viewer.Viewport.Height + 1);
        await CaptureAsync(window, "fit-page");

        // A page with a different aspect ratio is fitted using its own display geometry.
        vm.SelectedPage = vm.Pages[1];
        await WaitIdleAsync(vm);
        Assert.Equal(2, vm.CurrentPage);
        Assert.Equal(300, vm.Geometry!.DisplayWidth);
        Assert.True(vm.PageHeight + 38 <= viewer.Viewport.Height);
        picker.SelectedItem = "Fit width";
        await WaitIdleAsync(vm);
        Assert.Equal("Fit width", vm.ZoomChoice);
        Assert.InRange(viewer.Bounds.Width - vm.PageWidth, 64, 69);
        Assert.True(viewer.Extent.Width <= viewer.Viewport.Width + 1);
        await CaptureAsync(window, "fit-width");

        var widthZoom = vm.Zoom;
        window.Width = 900; window.Height = 650;
        await WaitIdleAsync(vm);
        Assert.True(vm.Zoom < widthZoom);
        Assert.InRange(viewer.Bounds.Width - vm.PageWidth, 64, 69);
        Assert.True(viewer.Extent.Width <= viewer.Viewport.Width + 1);
        Assert.True(picker.TranslatePoint(new Point(picker.Bounds.Width, picker.Bounds.Height), window)!.Value.X <= window.ClientSize.Width);
        await CaptureAsync(window, "fit-width-minimum");

        await vm.AddTextAtAsync(30, 40);
        vm.DraftText = "Unapplied text";
        vm.SelectedField = vm.FormFields[0]; vm.FormDraft = "Unapplied field";
        var historyBeforeZoom = vm.Document!.AddedText[0].Text;
        picker.SelectedItem = "Fit page";
        await WaitIdleAsync(vm);
        Assert.True(vm.HasTextDraft); Assert.True(vm.HasFormDraft);
        Assert.Equal("Unapplied text", vm.DraftText); Assert.Equal("Unapplied field", vm.FormDraft);
        Assert.Equal(historyBeforeZoom, vm.Document.AddedText[0].Text);
        Assert.True(surface.Bounds.Height + 38 <= viewer.Viewport.Height);

        // Changes in page orientation re-fit without switching out of automatic mode.
        await vm.RunAsync(async () => { await vm.ApplyAllDraftsAsync(); await vm.Document.RotateAsync(2); await vm.RefreshAsync(true); });
        await WaitIdleAsync(vm);
        Assert.Equal("Fit page", vm.ZoomChoice);
        Assert.True(vm.PageWidth + 48 <= viewer.Viewport.Width);
        Assert.True(vm.PageHeight + 38 <= viewer.Viewport.Height);

        picker.SelectedItem = "125%";
        await WaitIdleAsync(vm);
        window.Width = 1180; window.Height = 850;
        await WaitIdleAsync(vm);
        Assert.Equal(125, vm.Zoom);
        Assert.Equal("125%", new EditorPreferences(preferencesPath).LoadZoomChoice());
        picker.SelectedItem = "Fit width";
        await WaitIdleAsync(vm);
        await vm.Document.SaveCopyAsync(System.IO.Path.ChangeExtension(path, ".saved.pdf"));
        window.Close();

        var reopened = new MainWindow(null, preferencesPath); reopened.Show();
        Assert.Equal("Fit width", reopened.ViewModel.ZoomChoice);
        Assert.True(await reopened.ViewModel.OpenAsync(path));
        await WaitIdleAsync(reopened.ViewModel);
        Assert.Equal("Fit width", reopened.ViewModel.ZoomChoice);
        Assert.False(reopened.ViewModel.IsDirty);
        reopened.Close();
        File.WriteAllText(preferencesPath, "invalid json");
        Assert.Equal("Fit page", new EditorPreferences(preferencesPath).LoadZoomChoice());
    }

    [AvaloniaFact]
    public async Task Refresh_KeepsWorkspaceUndimmedAndRetainsPageContainers()
    {
        using var fixture = new EditorDocumentTests();
        var path = await fixture.CreatePdfAsync(pages: 3);
        var window = new MainWindow(null, System.IO.Path.ChangeExtension(path, ".preferences.json")); window.Show();
        var vm = window.ViewModel;
        Assert.True(await vm.OpenAsync(path), vm.Error);
        Dispatcher.UIThread.RunJobs();
        var workspace = window.FindControl<Grid>("DocumentWorkspace")!;
        var surface = window.FindControl<PageSurface>("DocumentSurface")!;
        var originalPages = vm.Pages.ToArray();
        var originalPreview = vm.PageBitmap;
        var collectionResets = 0;
        var blankThumbnails = 0;
        vm.Pages.CollectionChanged += (_, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset) collectionResets++;
        };
        foreach (var page in vm.Pages)
            page.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(PageItem.Thumbnail) && page.Thumbnail is null) blankThumbnails++; };

        var release = new TaskCompletionSource();
        var operation = vm.RunAsync(async () =>
        {
            await release.Task;
            await Task.Run(() => vm.Document!.RotateAsync(1));
            await vm.RefreshAsync(resetThumbnails: true);
        });
        Dispatcher.UIThread.RunJobs();
        Assert.True(vm.IsBusy);
        Assert.True(workspace.IsEffectivelyEnabled);
        Assert.True(surface.IsEffectivelyEnabled);
        Assert.False(workspace.IsHitTestVisible);
        Assert.Same(originalPreview, vm.PageBitmap);
        Assert.False(await vm.RunAsync(() => Task.CompletedTask));
        await CaptureAsync(window, "busy-workspace");
        release.SetResult();
        Assert.True(await operation, vm.Error);
        Dispatcher.UIThread.RunJobs();
        Assert.True(workspace.IsHitTestVisible);
        Assert.Equal(originalPages, vm.Pages.ToArray());
        Assert.Equal(0, collectionResets);
        Assert.Equal(0, blankThumbnails);
        Assert.NotSame(originalPreview, vm.PageBitmap);

        // Opening the inspector must not move the page underneath the pointer.
        var before = surface.TranslatePoint(default, window);
        await vm.AddTextAtAsync(30, 40);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(before, surface.TranslatePoint(default, window));
        await vm.RunAsync(() => vm.Document!.SaveCopyAsync(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!, "refreshed.pdf")));
        window.Close();
    }

    [AvaloniaFact]
    public async Task Window_OpenPlaceTypeMoveUndoSaveAndReopen_UsesRealPdf()
    {
        using var fixture = new EditorDocumentTests();
        var path = await fixture.CreatePdfAsync(pages: 3);
        await using (var sample = File.Create(path)) await Core.SampleDocument.WriteAsync(sample);
        var window = new MainWindow(null, System.IO.Path.ChangeExtension(path, ".preferences.json")); window.Show();
        var vm = window.ViewModel;
        Assert.True(await vm.OpenAsync(path), vm.Error);
        await WaitIdleAsync(vm);
        await CaptureAsync(window, "initial-fit-page");
        vm.Zoom = 75; await WaitIdleAsync(vm);
        Assert.True(vm.IsReady); Assert.Equal(3, vm.Pages.Count);
        Assert.NotNull(vm.PageBitmap);
        await CaptureAsync(window, "text-before");
        Click(window, "AddTextButton");
        Assert.True(vm.AddingText);
        var surface = window.FindControl<PageSurface>("DocumentSurface")!;
        var point = surface.TranslatePoint(new Point(60, 450), window)!.Value;
        window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
        await WaitIdleAsync(vm);
        Assert.Single(vm.Document!.AddedText);
        var editor = window.FindControl<TextBox>("TextEditor")!;
        Assert.True(editor.IsEffectivelyVisible);
        editor.Focus(); editor.SelectAll(); window.KeyTextInput("A real PDF edit");
        Assert.Equal("A real PDF edit", vm.DraftText);
        Click(window, "ApplyTextButton"); await WaitIdleAsync(vm);
        Assert.Equal("A real PDF edit", vm.Document.AddedText[0].Text);
        Assert.False(vm.HasTextDraft);
        await CaptureAsync(window, "text-selected");
        window.Width = 900; window.Height = 650;
        await CaptureAsync(window, "minimum-window");
        var saveButton = window.FindControl<Button>("SaveButton")!;
        Assert.True(saveButton.TranslatePoint(new Point(saveButton.Bounds.Width, saveButton.Bounds.Height), window)!.Value.X <= window.ClientSize.Width);
        window.Width = 1180; window.Height = 850;
        Dispatcher.UIThread.RunJobs();
        var bounds = vm.GetTextBounds().Single();
        var centre = surface.TranslatePoint(bounds.Bounds.Center, window)!.Value;
        window.MouseDown(centre, MouseButton.Left);
        window.MouseMove(centre + new Vector(35, 25));
        window.MouseUp(centre + new Vector(35, 25), MouseButton.Left);
        await WaitIdleAsync(vm);
        Assert.True(vm.GetTextBounds().Single().Bounds.X > bounds.Bounds.X + 20);
        vm.Zoom = 200;
        await WaitIdleAsync(vm);
        var zoomed = vm.GetTextBounds().Single();
        vm.Zoom = 100;
        await WaitIdleAsync(vm);
        Assert.Equal(vm.GetTextBounds().Single().Bounds.X * 2, zoomed.Bounds.X, 5);
        var copy = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!, "ui-edited.pdf");
        Assert.True(await vm.RunAsync(async () => { await vm.ApplyAllDraftsAsync(); await vm.Document.SaveCopyAsync(copy); }), vm.Error);
        Assert.False(vm.IsDirty);
        Assert.True(await vm.OpenAsync(copy));
        Assert.Empty(vm.Document!.AddedText); // Reopened additions are now normal saved page content.
        Assert.True(vm.PageBitmap is { PixelSize.Width: > 0 });
        window.Close();
    }

    [AvaloniaFact]
    public async Task TaskSwitchingAndMultipleFieldDrafts_ArePreservedAndVisible()
    {
        using var fixture = new EditorDocumentTests();
        var path = await fixture.CreatePdfAsync(forms: true, pages: 3);
        var window = new MainWindow(null, System.IO.Path.ChangeExtension(path, ".preferences.json")); window.Show(); var vm = window.ViewModel;
        Assert.True(await vm.OpenAsync(path), vm.Error);
        Click(window, "PagesModeButton"); await WaitIdleAsync(vm);
        Assert.True(vm.IsPagesMode);
        Assert.True(window.FindControl<ListBox>("PageOverview")!.IsEffectivelyVisible);
        await CaptureAsync(window, "pages");
        Click(window, "FormsModeButton"); await WaitIdleAsync(vm);
        Assert.True(vm.IsFormsMode);
        Assert.Equal(2, vm.FormFields.Count);
        vm.SelectedField = vm.FormFields[0]; vm.FormDraft = "Visitor name";
        vm.SelectedField = vm.FormFields[1]; vm.FormDraft = "Tomorrow";
        vm.SelectedField = vm.FormFields[0]; Assert.Equal("Visitor name", vm.FormDraft);
        await vm.RunAsync(async () => { await vm.ApplyAllDraftsAsync(); await vm.RefreshAsync(); });
        Assert.False(vm.HasFormDraft);
        var fields = await vm.Document!.GetFormFieldsAsync();
        Assert.Equal("Visitor name", fields[0].Value); Assert.Equal("Tomorrow", fields[1].Value);
        await CaptureAsync(window, "forms");
        // A failed open must preserve the currently edited document.
        Assert.False(await vm.OpenAsync(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!, "missing.pdf")));
        Assert.Equal(path, vm.Document.SourcePath);
        vm.Error = "";
        var output = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!, "forms-edited.pdf");
        await vm.Document.SaveCopyAsync(output); vm.NotifyState();
        window.Close();
    }

    [AvaloniaFact]
    public async Task ClosingWithUnappliedText_CancelPreservesDraftAndDiscardCloses()
    {
        using var fixture = new EditorDocumentTests();
        var path = await fixture.CreatePdfAsync();
        var original = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        var window = new MainWindow(null, System.IO.Path.ChangeExtension(path, ".preferences.json")); window.Show(); var vm = window.ViewModel;
        Assert.True(await vm.OpenAsync(path));
        await vm.AddTextAtAsync(30, 40); vm.DraftText = "Unapplied changes";
        window.Close();
        await Task.Delay(30, TestContext.Current.CancellationToken);
        var dialog = Assert.Single(window.OwnedWindows);
        var cancel = dialog.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Cancel");
        cancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(30, TestContext.Current.CancellationToken);
        Assert.True(window.IsVisible); Assert.Equal("Unapplied changes", vm.DraftText);
        window.Close();
        await Task.Delay(30, TestContext.Current.CancellationToken);
        dialog = Assert.Single(window.OwnedWindows);
        var discard = dialog.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Discard changes");
        discard.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(30, TestContext.Current.CancellationToken);
        Assert.False(window.IsVisible);
        Assert.Equal(original, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
    }

    private static void Click(Window window, string name)
    {
        Dispatcher.UIThread.RunJobs();
        var button = window.FindControl<Button>(name)!;
        Assert.True(button.IsEffectivelyVisible); Assert.True(button.IsEffectivelyEnabled);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    private static async Task WaitIdleAsync(MainViewModel vm)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        do { await Task.Delay(20, TestContext.Current.CancellationToken); Dispatcher.UIThread.RunJobs(); }
        while ((vm.IsBusy || vm.PreviewNeedsRefresh) && DateTime.UtcNow < deadline);
        Assert.False(vm.IsBusy); Assert.False(vm.HasError, vm.Error);
        Assert.False(vm.PreviewNeedsRefresh);
    }

    private static async Task CaptureAsync(Window window, string name)
    {
        Dispatcher.UIThread.RunJobs();
        var directory = Environment.GetEnvironmentVariable("ZINGPDF_EDITOR_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await Task.Delay(30, TestContext.Current.CancellationToken);
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame.Save(System.IO.Path.Combine(directory, name + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}

