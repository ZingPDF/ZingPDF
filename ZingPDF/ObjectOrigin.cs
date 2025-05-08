namespace ZingPDF;

public enum ObjectOrigin
{
    /// <summary>
    /// An object with no origin. How mysterious.
    /// </summary>
    None,

    /// <summary>
    /// The object was parsed by the <see cref="Parsing.Parsers.FileStructure.DocumentVersionParser"/>
    /// </summary>
    DocumentVersionParser,

    /// <summary>
    /// The object was parsed from the PDF file structure (e.g., from a dictionary or array).
    /// </summary>
    ParsedDocumentObject,

    /// <summary>
    /// The object was parsed from within a content stream (e.g., between operators).
    /// </summary>
    ParsedContentStream,

    /// <summary>
    /// The object was created manually by the user or application, not parsed.
    /// </summary>
    UserCreated,

    /// <summary>
    /// The object was created by the library as part of a conversion process (e.g., from a .NET string to a Number instance).
    /// </summary>
    ImplicitOperatorConversion,
}

