namespace ZingPDF
{
    internal static class Constants
    {
        public const char LineFeed = '\n';
        public const char CarriageReturn = '\r';
        public const char HorizontalTab = '\t';
        public const char FormFeed = '\f';
        public const char Backspace = '\b';
        public const char Space = ' ';
        public const char Comment = '%';
        public const char Solidus = '/';
        public const char ReverseSolidus = '\\';
        public const char LeftParenthesis = '(';
        public const char RightParenthesis = ')';
        public const char Whitespace = ' ';
        public const char ArrayStart = '[';
        public const char ArrayEnd = ']';
        public const char LessThan = '<';
        public const char GreaterThan = '>';
        public const char IndirectReference = 'R';

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

        public static readonly byte[] BinaryCharacters = [226, 227, 207, 211];

        /// <summary>
        /// Special characters used to delimit syntactic entities such as arrays, names, comments.
        /// </summary>
        public static readonly char[] Delimiters = ['(', ')', '<', '>', '[', ']', '{', '}', '/', '%'];

        public static readonly char[] WhitespaceCharacters = [Space, HorizontalTab, LineFeed, CarriageReturn, FormFeed];
        public static readonly char[] EndOfLineCharacters = [CarriageReturn, LineFeed];

        public static class Filters
        {
            public const string ASCII85 = "ASCII85Decode";
            public const string ASCIIHex = "ASCIIHexDecode";
            public const string LZW = "LZWDecode";
            public const string Flate = "FlateDecode";
            public const string RunLength = "RunLengthDecode";
        }

        public static class DictionaryKeys
        {
            public const string Type = "Type";
        }
    }
}
