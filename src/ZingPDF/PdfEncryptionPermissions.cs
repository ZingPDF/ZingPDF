namespace ZingPDF;

/// <summary>
/// Controls which user actions remain allowed on an encrypted PDF.
/// </summary>
[Flags]
public enum PdfEncryptionPermissions
{
    /// <summary>
    /// No additional user permissions.
    /// </summary>
    None = 0,

    /// <summary>
    /// Allow printing.
    /// </summary>
    Print = 1 << 2,

    /// <summary>
    /// Allow editing document content.
    /// </summary>
    Modify = 1 << 3,

    /// <summary>
    /// Allow copying text or other content.
    /// </summary>
    Copy = 1 << 4,

    /// <summary>
    /// Allow comments, annotations, and similar markup.
    /// </summary>
    Annotate = 1 << 5,

    /// <summary>
    /// Allow filling in existing form fields.
    /// </summary>
    FillForms = 1 << 8,

    /// <summary>
    /// Allow page insertion, deletion, and document assembly operations.
    /// </summary>
    AssembleDocument = 1 << 10,

    /// <summary>
    /// Allow high-quality printing. This also implies <see cref="Print"/>.
    /// </summary>
    PrintHighQuality = 1 << 11,

    /// <summary>
    /// Every high-level permission exposed by ZingPDF.
    /// </summary>
    All = Print | Modify | Copy | Annotate | FillForms | AssembleDocument | PrintHighQuality,
}

internal static class PdfEncryptionPermissionBits
{
    private const int RequiredBaseBits = unchecked((int)0xFFFFF0C0);

    public static int ToStandardPermissionValue(PdfEncryptionPermissions permissions)
    {
        if ((permissions & PdfEncryptionPermissions.PrintHighQuality) != 0)
        {
            permissions |= PdfEncryptionPermissions.Print;
        }

        return RequiredBaseBits | (int)permissions;
    }
}
