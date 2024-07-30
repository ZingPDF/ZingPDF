namespace PuppeteerSharp;

/// <summary>
/// New document information.
/// </summary>
internal class NewDocumentScriptEvaluation(string documentIdentifierIdentifier)
{
    /// <summary>
    /// New document identifier.
    /// </summary>
    public string Identifier { get; set; } = documentIdentifierIdentifier;
}
