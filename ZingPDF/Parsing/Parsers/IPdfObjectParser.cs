using MorseCode.ITask;
using ZingPDF.ObjectModel;

namespace ZingPDF.Parsing.Parsers
{
    internal interface IPdfObjectParser<out T> where T : IPdfObject
    {
        /// <summary>
        /// Parses a stream into an object.
        /// </summary>
        /// <remarks>
        /// This class is responsible for finding the start and end of the object,
        /// and returning the parsed type.
        /// </remarks>
        ITask<T> ParseAsync(Stream stream);
    }
}
