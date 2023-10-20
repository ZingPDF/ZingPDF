using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core
{
    internal static class Constants
    {
        public static readonly char NewLine = '\n';
        public static readonly char CarriageReturn = '\r';
        public static readonly char Tab = '\t';
        public static readonly char Space = ' ';
        public static readonly char Comment = '%';
        public static readonly char Solidus = '/';
        public static readonly char StringStart = '(';
        public static readonly char StringEnd = ')';
        public static readonly char Whitespace = ' ';
        public static readonly char ArrayStart = '[';
        public static readonly char ArrayEnd = ']';
        public static readonly char LessThan = '<';
        public static readonly char GreaterThan = '>';
        public static readonly char IndirectReference = 'R';

        public static readonly string PdfVersionPrefix = "PDF-";
        public static readonly string ObjStart = "obj";
        public static readonly string ObjEnd = "endobj";

        public static readonly string DictionaryStart = "<<";
        public static readonly string DictionaryEnd = ">>";
        public static readonly string Trailer = "trailer";
        public static readonly string StartXref = "startxref";
        public static readonly string Xref = "xref";
        public static readonly string StreamStart = "stream";
        public static readonly string StreamEnd = "endstream";
        public static readonly string Null = "null";
        public static readonly string Eof = "%%EOF";
        
        public static readonly byte[] BinaryCharacters = new byte[] { 129, 130, 131, 132 };

        /// <summary>
        /// Special characters used to delimit syntactic entities such as arrays, names, comments.
        /// </summary>
        public static readonly char[] Delimiters = new char[] { '(', ')', '<', '>', '[', ']', '{', '}', '/', '%' };

        public static readonly char[] WhitespaceCharacters = new char[] { Space, Tab, NewLine, CarriageReturn, '\f' };

        public static class Filters
        {
            public const string ASCII85 = "ASCII85Decode";
            public const string ASCIIHex = "ASCIIHexDecode";
            public const string LZW = "LZWDecode";
            public const string Flate = "FlateDecode";
            public const string RunLength = "RunLengthDecode";
        }
    }
}
