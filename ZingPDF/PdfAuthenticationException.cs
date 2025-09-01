namespace ZingPDF
{
    internal class PdfAuthenticationException : Exception
    {
        public PdfAuthenticationException()
        {
        }

        public PdfAuthenticationException(string? message) : base(message)
        {
        }

        public PdfAuthenticationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}