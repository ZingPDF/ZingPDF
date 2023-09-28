namespace ZingPdf.Core.Objects.Filters
{

    [Serializable]
	public class FilterInputFormatException : ArgumentException
	{
		public FilterInputFormatException() { }
		public FilterInputFormatException(string paramName) : base("Invalid filter input", paramName) { }
		public FilterInputFormatException(string paramName, string message) : base(message, paramName) { }
        public FilterInputFormatException(string message, Exception inner) : base(message, inner) { }
		protected FilterInputFormatException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
