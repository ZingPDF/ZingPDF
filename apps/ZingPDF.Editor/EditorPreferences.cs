using System.Text.Json;

namespace ZingPDF.Editor;

/// <summary>Local zoom preferences and up to ten recent file paths. No PDF contents are stored.</summary>
public sealed class EditorPreferences(string path)
{
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZingPDF Editor", "preferences.json");
    private sealed record Settings(string ZoomChoice = "Fit page", string[]? RecentFiles = null);
    private Settings Read()
    {
        try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }
    public string LoadZoomChoice()
    {
        var choice = Read().ZoomChoice;
        return MainViewModel.IsZoomChoice(choice) ? choice : "Fit page";
    }
    public string[] LoadRecentFiles() => (Read().RecentFiles ?? []).Where(p => !string.IsNullOrWhiteSpace(p) && Path.IsPathFullyQualified(p)).Distinct(PathComparer).Take(10).ToArray();
    private static StringComparer PathComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    public void SaveZoomChoice(string choice) => Write(Read() with { ZoomChoice = choice });
    public void RecordFile(string file) => Write(Read() with { RecentFiles = new[] { Path.GetFullPath(file) }.Concat(LoadRecentFiles()).Distinct(PathComparer).Take(10).ToArray() });
    public void RemoveFile(string file) => Write(Read() with { RecentFiles = LoadRecentFiles().Where(p => !PathComparer.Equals(p, file)).ToArray() });
    public void ClearRecentFiles() => Write(Read() with { RecentFiles = [] });
    private void Write(Settings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings));
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A read-only preferences directory must not prevent viewing a PDF.
        }
    }
}

public sealed record RecentFileItem(string Path)
{
    public string Name => System.IO.Path.GetFileName(Path);
    public string Folder => System.IO.Path.GetDirectoryName(Path) ?? "";
}
