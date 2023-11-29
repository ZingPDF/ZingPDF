using System.Text;
using System.Text.RegularExpressions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.DataStructures;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;
using ZingPdf.Core.Objects.Primitives.Streams;

namespace ZingPdf.Core.Parsing
{
    internal static class TokenTypeIdentifier
    {
        private static readonly int _bufferSize = 1024;
        
        private static readonly Regex _headerPattern = new(@"^\%PDF-"); // %PDF-2.0
        private static readonly Regex _integerPattern = new(@"^-?\d+\s*"); // 1234
        private static readonly Regex _realNumberPattern = new(@"^-?\d*\.\d+"); // 595.276000
        private static readonly Regex _namePattern = new(@"^\s*\/.+"); // /Name
        private static readonly Regex _ioPattern = new(@"^[\d]+ [\d]+ obj"); // 1 0 obj
        private static readonly Regex _iorPattern = new(@"^[\d]+ [\d]+ R"); // 49 0 R
        private static readonly Regex _xrefSectionIndexPattern = new(@"^[0-9]+\s[0-9]+[\n\r]"); // 0 28
        private static readonly Regex _xrefEntryPattern = new(@"^[0-9]+\s[0-9]+\s[fn]"); // 0000000000 65535 f
        private static readonly Regex _datePattern = new(@"^\(D:\d{4,14}[+\-Z]\d{2}'?\d{2}'?\)"); // (D:20230922161207+10'00')

        private static readonly Dictionary<Regex, Type> _regexPatterns = new()
        {
            { _headerPattern, typeof(Header) },
            { _xrefSectionIndexPattern, typeof(CrossReferenceSection) },
            { _xrefEntryPattern, typeof(CrossReferenceEntry) },
            { _namePattern, typeof(Name) },
            { _ioPattern, typeof(IndirectObject) },
            { _iorPattern, typeof(IndirectObjectReference) },
            { _realNumberPattern, typeof(RealNumber) },
            { _integerPattern, typeof(Integer) },
            { _datePattern, typeof(Date) },
        };

        private static readonly Dictionary<string, Type> _startsWithPatterns = new()
        {
            { $"{Constants.Comment}", typeof(Comment) },
            { Constants.Null, typeof(Keyword) }, // TODO: should this be the null object type?
            { Constants.Eof, typeof(Keyword) },
            { Constants.Xref, typeof(Keyword) },
            { Constants.StartXref, typeof(Keyword) },
            { Constants.ObjEnd, typeof(Keyword) },
            { Constants.StreamEnd, typeof(Keyword) },
            { Constants.DictionaryStart, typeof(Dictionary) },
            { $"{Constants.LessThan}", typeof(HexadecimalString) }, // This check must always come after the DictionaryStart check.
            { $"{Constants.LeftParenthesis}", typeof(LiteralString) },
            { $"{Constants.ArrayStart}", typeof(ArrayObject) },
            { Constants.Trailer, typeof(Trailer) },
            { Constants.StreamStart, typeof(StreamObject) },
            { "true", typeof(BooleanObject) },
            { "false", typeof(BooleanObject) },
        };

        public static async Task<Type> TryIdentifyAsync(Stream stream)
        {
            var buffer = new byte[_bufferSize];

            var read = await stream.ReadAsync(buffer.AsMemory(0, _bufferSize));

            var content = Encoding.UTF8.GetString(buffer, 0, read).TrimStart();

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            stream.Position -= read;

            foreach (var pattern in _regexPatterns)
            {
                if (pattern.Key.IsMatch(content))
                {
                    return pattern.Value;
                }
            }

            foreach (var pattern in _startsWithPatterns)
            {
                if (content.StartsWith(pattern.Key))
                {
                    return pattern.Value;
                }
            }

            throw new ParserException("Unable to identify token from stream");
        }
    }
}
