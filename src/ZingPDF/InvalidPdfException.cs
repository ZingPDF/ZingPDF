namespace ZingPDF;

/// <summary>
/// Thrown when a PDF cannot be parsed or does not satisfy the library's expectations for a valid document.
/// </summary>
[Serializable]
public class InvalidPdfException : Exception
{
    /// <summary>
    /// Initializes a new <see cref="InvalidPdfException"/>.
    /// </summary>
    public InvalidPdfException() { }

    /// <summary>
    /// Initializes a new <see cref="InvalidPdfException"/> with a message.
    /// </summary>
    public InvalidPdfException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new <see cref="InvalidPdfException"/> with a message and inner exception.
    /// </summary>
    public InvalidPdfException(string message, Exception inner) : base(message, inner) { }
}
