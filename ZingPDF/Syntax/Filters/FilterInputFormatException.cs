namespace ZingPDF.Syntax.Filters
{

    [Serializable]
    public class FilterInputFormatException : ArgumentException
    {
        public FilterInputFormatException() { }
        public FilterInputFormatException(string paramName) : base("Invalid filter input", paramName) { }
        public FilterInputFormatException(string paramName, string message) : base(message, paramName) { }
        public FilterInputFormatException(string message, Exception inner) : base(message, inner) { }
    }
}
