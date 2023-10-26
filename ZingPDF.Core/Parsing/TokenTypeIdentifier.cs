using System.Text;
using System.Text.RegularExpressions;
using ZingPdf.Core.Objects.DataStructures;
using ZingPdf.Core.Objects.IndirectObjects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing
{
    internal static class TokenTypeIdentifier
    {
        private static readonly string[] _keywords = new[] { Constants.Eof, Constants.StartXref, Constants.ObjEnd };

        private static readonly int _bufferSize = 1024;
        
        private static readonly Regex _integerPattern = new(@"^\d+\s*"); // 1234
        private static readonly Regex _realNumberPattern = new(@"^\d+\.\d+"); // 595.276000
        private static readonly Regex _namePattern = new(@"^\s*\/.+"); // /Name
        private static readonly Regex _iorPattern = new(@"^[\d]+ [\d]+ R"); // 49 0 R
        private static readonly Regex _xrefSectionIndexPattern = new(@"^[0-9]+\s[0-9]+[\n\r]"); // 0 28
        private static readonly Regex _xrefEntryPattern = new(@"^[0-9]+\s[0-9]+\s[fn]"); // 0000000000 65535 f
        private static readonly Regex _datePattern = new(@"^\(D:\d{4,14}[+\-Z]\d{2}'?\d{2}'?\)"); // (D:20230922161207+10'00')

        public static async Task<Type?> TryIdentifyAsync(Stream stream)
        {
            var buffer = new byte[_bufferSize];

            var read = await stream.ReadAsync(buffer.AsMemory(0, _bufferSize));

            var content = Encoding.UTF8.GetString(buffer, 0, read).TrimStart();

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            stream.Position -= read;

            if (_keywords.Any(k => content.StartsWith(k)))
            {
                return typeof(Keyword);
            }

            if (content.StartsWith(Constants.Null))
            {
                // TODO: figure out if the word "null" should be represented as a keyword, or the null object, or a string literal, or something else
                return typeof(Keyword);
            }

            if (_xrefSectionIndexPattern.IsMatch(content))
            {
                return typeof(CrossReferenceSection);
            }

            if (_xrefEntryPattern.IsMatch(content))
            {
                return typeof(CrossReferenceEntry);
            }

            if (_namePattern.IsMatch(content))
            {
                return typeof(Name);
            }

            if (_iorPattern.IsMatch(content))
            {
                return typeof(IndirectObjectReference);
            }

            if (_realNumberPattern.IsMatch(content))
            {
                return typeof(RealNumber);
            }

            if (_integerPattern.IsMatch(content))
            {
                return typeof(Integer);
            }

            if (_datePattern.IsMatch(content))
            {
                return typeof(Date);
            }

            if (content.StartsWith(Constants.DictionaryStart))
            {
                return typeof(Dictionary);
            }

            // This check must always come after the DictionaryStart check.
            if (content.StartsWith(Constants.LessThan))
            {
                return typeof(HexadecimalString);
            }

            if (content.StartsWith(Constants.LeftParenthesis))
            {
                return typeof(Objects.Primitives.LiteralString);
            }

            if (content.StartsWith(Constants.ArrayStart))
            {
                return typeof(ArrayObject);
            }

            if (content.StartsWith(Constants.Trailer))
            {
                return typeof(PdfObjectGroup);
            }

            if (content.StartsWith(Constants.StreamStart))
            {
                return typeof(StreamObject);
            }

            throw new ParserException("Unable to identify token from stream");
        }
    }
}
