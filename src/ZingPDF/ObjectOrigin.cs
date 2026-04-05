namespace ZingPDF;

/// <summary>
/// Describes how a PDF object entered the current document model.
/// </summary>
public enum ObjectOrigin
{
    /// <summary>
    /// The object has no recorded origin.
    /// </summary>
    None,

    /// <summary>
    /// The object was parsed by the document version parser.
    /// </summary>
    DocumentVersionParser,

    /// <summary>
    /// The object was parsed from the main PDF object structure, such as a dictionary or array.
    /// </summary>
    ParsedDocumentObject,

    /// <summary>
    /// The object was parsed from within a content stream.
    /// </summary>
    ParsedContentStream,

    /// <summary>
    /// The object was created explicitly by application or library code.
    /// </summary>
    UserCreated,

    /// <summary>
    /// The object was created by an automatic conversion from another .NET value.
    /// </summary>
    ImplicitOperatorConversion,
}
