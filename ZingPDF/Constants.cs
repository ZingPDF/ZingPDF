namespace ZingPDF;

internal static class Constants
{
    public const char LineFeed = '\n';
    public const char CarriageReturn = '\r';
    public const char HorizontalTab = '\t';
    public const char FormFeed = '\f';
    public const char Backspace = '\b';
    public const char Space = ' ';
    public const char Percent = '%';
    public const char Solidus = '/';
    public const char ReverseSolidus = '\\';
    public const char LeftParenthesis = '(';
    public const char RightParenthesis = ')';
    public const char Whitespace = ' ';
    public const char LeftSquareBracket = '[';
    public const char RightSquareBracket = ']';
    public const char LessThan = '<';
    public const char GreaterThan = '>';
    public const char LeftBrace = '{';
    public const char RightBrace = '}';
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
    public static readonly char[] Delimiters = [
        LeftParenthesis, RightParenthesis,
        LessThan, GreaterThan,
        LeftSquareBracket, RightSquareBracket,
        LeftBrace, RightBrace,
        Solidus,
        Percent
    ];

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

    internal static class PdfVersion
    {
        public static double v1 = 1.0;
        public static double v1_1 = 1.1;
        public static double v1_2 = 1.2;
        public static double v1_3 = 1.3;
        public static double v1_4 = 1.4;
        public static double v1_5 = 1.5;
        public static double v1_6 = 1.6;
        public static double v1_7 = 1.7;
        public static double v2 = 2.0;

        public static double[] All = [v1, v1_2, v1_3, v1_4, v1_5, v1_6, v1_7, v2];
    }
}
