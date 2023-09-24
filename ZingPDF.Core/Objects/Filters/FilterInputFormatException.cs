namespace ZingPdf.Core.Objects.Filters
{

    [Serializable]
	public class FilterInputFormatException : ArgumentException
	{
		public FilterInputFormatException() { }
		public FilterInputFormatException(string message) : base(message) { }
		public FilterInputFormatException(string message, string paramName) : base(message, paramName) { }
        public FilterInputFormatException(string message, Exception inner) : base(message, inner) { }
		protected FilterInputFormatException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
