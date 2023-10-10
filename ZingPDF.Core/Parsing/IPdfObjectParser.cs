using ZingPdf.Core.Objects;

namespace ZingPdf.Core.Parsing
{
    internal interface IPdfObjectParser<out T> where T : PdfObject
    {
        /// <summary>
        /// Parses a string into an object.
        /// </summary>
        /// <remarks>
        /// This class is responsible for finding the start and end of the object,
        /// and returning the parsed type, along with the remaining content.
        /// </remarks>
        IParseResult<T> Parse(string content);
    }

    internal interface IParseResult<out T> where T : PdfObject
    {
        T Obj { get; }
        string RemainingContent { get; }
    }

    internal class ParseResult<T> : IParseResult<T> where T : PdfObject
    {
        internal ParseResult(T obj, string remainingContent)
        {
            Obj = obj;
            RemainingContent = remainingContent;
        }

        public T Obj { get; }
        public string RemainingContent { get; }
    }
}
