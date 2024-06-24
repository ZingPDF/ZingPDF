namespace ZingPDF
{

    [Serializable]
	public class InvalidPdfException : Exception
	{
		public InvalidPdfException() { }
		public InvalidPdfException(string message) : base(message) { }
		public InvalidPdfException(string message, Exception inner) : base(message, inner) { }
	}
}
