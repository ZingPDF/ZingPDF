using MorseCode.ITask;
using ZingPDF.Syntax;

namespace ZingPDF.Parsing.Parsers;

/// <summary>
/// Parser for PDF objects.
/// </summary>
/// <remarks>
/// This class is responsible for finding the start and end of the object,
/// and returning the parsed type.
/// </remarks>
public interface IParser<out T> where T : IPdfObject
{
    /// <summary>
    /// Parses a stream into an object.
    /// </summary>
    ITask<T> ParseAsync(Stream stream, ObjectContext context);
}
