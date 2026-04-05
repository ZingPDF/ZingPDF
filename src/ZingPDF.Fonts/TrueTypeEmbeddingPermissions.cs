namespace ZingPDF.Fonts;

/// <summary>
/// Embedding permissions derived from the OpenType OS/2 fsType field.
/// </summary>
public sealed record TrueTypeEmbeddingPermissions(
    ushort FsType,
    bool IsInstallable,
    bool AllowsPreviewAndPrint,
    bool AllowsEditableEmbedding,
    bool RequiresFullFontEmbedding,
    bool BitmapEmbeddingOnly)
{
    public bool AllowsPdfEmbedding => !BitmapEmbeddingOnly && (IsInstallable || AllowsPreviewAndPrint || AllowsEditableEmbedding);
}
