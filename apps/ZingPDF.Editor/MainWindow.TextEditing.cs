using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ZingPDF.Text.Editing;

namespace ZingPDF.Editor;

public sealed partial class MainWindow
{
    private readonly DispatcherTimer _existingCheckTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private IEditorFontSource _fontSource = new GoogleEditorFontSource();
    private bool _existingCheckRunning;

    private void InitializeTextEditing(IEditorFontSource? fontSource)
    {
        if (fontSource is not null) _fontSource = fontSource;
        ViewModel.ExistingEditCheckRequested += () => { _existingCheckTimer.Stop(); _existingCheckTimer.Start(); };
        _existingCheckTimer.Tick += async (_, _) =>
        {
            _existingCheckTimer.Stop();
            if (_closed || !ViewModel.HasExistingSelection) return;
            if (ViewModel.IsBusy || _existingCheckRunning) { _existingCheckTimer.Start(); return; }
            _existingCheckRunning = true;
            try { await ViewModel.CheckExistingDraftAsync(); }
            finally { _existingCheckRunning = false; }
        };
        DocumentSurface.ExistingTextSelected += () => { ExistingTextEditor.Focus(); ExistingTextEditor.SelectAll(); };
        Closed += (_, _) => _existingCheckTimer.Stop();
    }

    private void DiscardExistingTextClicked(object? sender, RoutedEventArgs e) => ViewModel.DiscardExistingDraft();

    private void OriginalFontClicked(object? sender, RoutedEventArgs e) => ViewModel.UseOriginalExistingFont();

    private async void ChooseExistingFontClicked(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (!vm.CanEditExisting || vm.SelectedExistingLine is not { } selected || vm.Document is not { } document) return;
        var dialog = new Window { Title = "Choose a font", Width = 490, Height = 440, MinWidth = 440, MinHeight = 360, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var family = new AutoCompleteBox { Name = "GoogleFontFamily", ItemsSource = GoogleEditorFontSource.Families, Text = SuggestedFamily(selected.FontName), MinimumPrefixLength = 0, FilterMode = AutoCompleteFilterMode.Contains };
        var variants = new[] { "Regular", "Bold", "Italic", "Bold italic" };
        var style = new ComboBox { Name = "GoogleFontStyle", ItemsSource = variants, SelectedIndex = (selected.FontName.Contains("Bold", StringComparison.OrdinalIgnoreCase) ? 1 : 0) + (selected.FontName.Contains("Italic", StringComparison.OrdinalIgnoreCase) || selected.FontName.Contains("Oblique", StringComparison.OrdinalIgnoreCase) ? 2 : 0), HorizontalAlignment = HorizontalAlignment.Stretch };
        var note = new TextBlock { Text = "Choose a Google Fonts family, or type another family name. Downloads contain only font data; your PDF and its text stay on this computer.", TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.Parse("#627080")), FontSize = 13 };
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 13, MinHeight = 42 };
        var use = new Button { Name = "UseGoogleFontButton", Content = "Use font", IsDefault = true, Classes = { "primary" } };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        cancel.Click += (_, _) => { cancellation.Cancel(); dialog.Close(); };
        dialog.Closed += (_, _) => cancellation.Cancel();
        use.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(family.Text)) { status.Text = "Choose a font family."; return; }
            use.IsEnabled = false; family.IsEnabled = false; style.IsEnabled = false;
            status.Text = "Getting the font and checking its characters…";
            try
            {
                var requestedFamily = family.Text.Trim();
                var variant = style.SelectedIndex switch { 1 => "700", 2 => "italic", 3 => "700italic", _ => "regular" };
                var bytes = await _fontSource.GetFontAsync(requestedFamily, variant, token);
                var text = vm.ExistingDraftText; var size = vm.ExistingDraftSize;
                var check = await Task.Run(() => document.CheckExistingTextAsync(selected, text, new PdfTextReplacementOptions { TrueTypeFontData = bytes, FontSize = size, AllowOverflow = true }), token);
                token.ThrowIfCancellationRequested();
                if (check.RequiresFontChange) { status.Text = check.Message; return; }
                vm.ChooseExistingFont(bytes, requestedFamily + " " + variants[style.SelectedIndex]);
                dialog.Close();
            }
            catch (OperationCanceledException)
            {
                if (!token.IsCancellationRequested && dialog.IsVisible)
                    status.Text = "The font download timed out. Check your connection and try again.";
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                if (dialog.IsVisible) status.Text = ex is HttpRequestException or IOException ? "The font could not be downloaded. Check your connection, try another family, or use a font already cached on this computer." : ex.Message;
            }
            finally
            {
                if (dialog.IsVisible) { use.IsEnabled = true; family.IsEnabled = true; style.IsEnabled = true; }
            }
        };
        var fields = new StackPanel { Spacing = 12, Children =
        {
            new TextBlock { Text = "Font family", FontWeight = FontWeight.SemiBold }, family,
            new TextBlock { Text = "Style", FontSize = 13 }, style, note, status
        }};
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Children = { cancel, use } };
        Grid.SetRow(buttons, 1);
        dialog.Content = new Grid { Margin = new Thickness(24), RowDefinitions = new RowDefinitions("*,Auto"), RowSpacing = 14, Children = { new ScrollViewer { Content = fields }, buttons } };
        await dialog.ShowDialog(this);
    }

    private static string SuggestedFamily(string name)
    {
        var family = Regex.Replace(name, "^[A-Z]{6}\\+", "");
        family = Regex.Replace(family, "[-,](Regular|BoldItalic|Bold|Italic|Roman|Light|Medium|SemiBold)$", "", RegexOptions.IgnoreCase);
        var known = GoogleEditorFontSource.Families.FirstOrDefault(f => string.Equals(f.Replace(" ", ""), family.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
        if (known is not null) return known;
        if (family.Contains("Times", StringComparison.OrdinalIgnoreCase) || family.Contains("Serif", StringComparison.OrdinalIgnoreCase)) return "Noto Serif";
        if (family.Contains("Helvetica", StringComparison.OrdinalIgnoreCase) || family.Contains("Arial", StringComparison.OrdinalIgnoreCase)) return "Roboto";
        return family;
    }
}
