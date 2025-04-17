using System.Text;
using System.Text.RegularExpressions;
using ZingPDF.Logging;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.FileStructure;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing
{
    internal static class TokenTypeIdentifier
    {
        private static readonly int _bufferSize = 256;

        private static readonly Dictionary<Regex, Type> _regexPatterns = new()
        {
            { RegularExpressions.Header(), typeof(Header) },
            { RegularExpressions.CrossReferenceEntry(), typeof(CrossReferenceEntry) },
            { RegularExpressions.Name(), typeof(Name) },
            { RegularExpressions.IndirectObject(), typeof(IndirectObject) },
            { RegularExpressions.IndirectObjectReference(), typeof(IndirectObjectReference) },
            { RegularExpressions.RealNumber(), typeof(Number) },
            { RegularExpressions.Integer(), typeof(Number) },
            { RegularExpressions.Date(), typeof(Date) },
        };

        private static readonly Dictionary<string, Type> _startsWithPatterns = new()
        {
            { $"{Constants.Characters.Percent}", typeof(Comment) },
            { Constants.Null, typeof(Keyword) }, // TODO: should this be the null object type?
            { Constants.Eof, typeof(Keyword) },
            { Constants.Xref, typeof(Keyword) },
            { Constants.StartXref, typeof(Keyword) },
            { Constants.ObjEnd, typeof(Keyword) },
            { Constants.StreamEnd, typeof(Keyword) },
            { Constants.DictionaryStart, typeof(Dictionary) },
            { $"{Constants.Characters.LessThan}", typeof(HexadecimalString) }, // This check must always come after the DictionaryStart check.
            { $"{Constants.Characters.LeftParenthesis}", typeof(LiteralString) },
            { $"{Constants.Characters.LeftSquareBracket}", typeof(ArrayObject) },
            { Constants.Trailer, typeof(Trailer) },
            { Constants.StreamStart, typeof(StreamObject<>) },
            { "true", typeof(BooleanObject) },
            { "false", typeof(BooleanObject) },
        };

        public static async Task<Type?> TryIdentifyAsync(Stream stream)
        {
            // Save the original position to reset after processing
            long originalPosition = stream.Position;

            try
            {
                var buffer = new byte[_bufferSize];
                var read = await stream.ReadAsync(buffer.AsMemory(0, _bufferSize));

                // Reset stream position for reuse
                stream.Position = originalPosition;

                // Decode and trim the content (ASCII encoding is assumed)
                var content = Encoding.UTF8.GetString(buffer, 0, read).TrimStart();

                if (string.IsNullOrWhiteSpace(content))
                {
                    return null; // No meaningful content to process
                }

                Logger.Log(LogLevel.Trace, "TokenTypeIdentifier.TryIdentify:");
                Logger.Log(LogLevel.Trace, content[..Math.Min(120, content.Length)]);

                // First-pass: Match regex patterns
                foreach (var (regex, type) in _regexPatterns)
                {
                    if (regex.IsMatch(content))
                    {
                        Logger.Log(LogLevel.Trace, $"Identified as: {type.Name}");

                        return type;
                    }
                }

                // Second-pass: Match starts-with patterns
                foreach (var (prefix, type) in _startsWithPatterns)
                {
                    if (content.StartsWith(prefix))
                    {
                        Logger.Log(LogLevel.Trace, $"Identified as: {type.Name}");

                        return type;
                    }
                }

                // Let's test this out, identify anything other than the types above as a keyword.
                return typeof(Keyword);

                // Throw exception in development to catch bugs in identification logic
                throw new ParserException("Unable to identify token from stream");
            }
            finally
            {
                // Ensure stream is restored to its original position
                stream.Position = originalPosition;
            }
        }

    }
}
