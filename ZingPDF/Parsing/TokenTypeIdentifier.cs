using System.Text;
using System.Text.RegularExpressions;
using ZingPDF.Parsing;
using ZingPDF.Logging;
using ZingPDF.Syntax.FileStructure;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Parsing.Parsers;

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
            { RegularExpressions.RealNumber(), typeof(RealNumber) },
            { RegularExpressions.Integer(), typeof(Integer) },
            { RegularExpressions.Date(), typeof(Date) },
        };

        private static readonly Dictionary<string, Type> _startsWithPatterns = new()
        {
            { $"{Constants.Percent}", typeof(Comment) },
            { Constants.Null, typeof(Keyword) }, // TODO: should this be the null object type?
            { Constants.Eof, typeof(Keyword) },
            { Constants.Xref, typeof(Keyword) },
            { Constants.StartXref, typeof(Keyword) },
            { Constants.ObjEnd, typeof(Keyword) },
            { Constants.StreamEnd, typeof(Keyword) },
            { Constants.DictionaryStart, typeof(Dictionary) },
            { $"{Constants.LessThan}", typeof(HexadecimalString) }, // This check must always come after the DictionaryStart check.
            { $"{Constants.LeftParenthesis}", typeof(LiteralString) },
            { $"{Constants.LeftSquareBracket}", typeof(ArrayObject) },
            { Constants.Trailer, typeof(Trailer) },
            { Constants.StreamStart, typeof(IStreamObject<IStreamDictionary>) },
            { "true", typeof(BooleanObject) },
            { "false", typeof(BooleanObject) },
        };

        public static async Task<Type?> TryIdentifyAsync(Stream stream)
        {
            var buffer = new byte[_bufferSize];

            var read = await stream.ReadAsync(buffer.AsMemory(0, _bufferSize));
            stream.Position -= read;

            var content = Encoding.UTF8.GetString(buffer, 0, read).TrimStart();

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            Logger.Log(LogLevel.Trace, "TokenTypeIdentifier.TryIdentify:");
            Logger.Log(LogLevel.Trace, content[..Math.Min(50, content.Length)]);

            foreach (var pattern in _regexPatterns)
            {
                if (pattern.Key.IsMatch(content))
                {
                    Logger.Log(LogLevel.Trace, $"Identified as: {pattern.Value.Name}");

                    return pattern.Value;
                }
            }

            foreach (var pattern in _startsWithPatterns)
            {
                if (content.StartsWith(pattern.Key))
                {
                    Logger.Log(LogLevel.Trace, $"Identified as: {pattern.Value.Name}");

                    return pattern.Value;
                }
            }

            // TODO: consider returning null here.
            throw new ParserException("Unable to identify token from stream");
        }
    }
}
