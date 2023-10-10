namespace ZingPdf.Core
{
    internal static class Constants
    {
        public static char NewLine = '\n';
        public static char CarriageReturn = '\r';
        public static char Tab = '\t';
        public static char Space = ' ';
        public static char Comment = '%';
        public static char Solidus = '/';
        public static char StringStart = '(';
        public static char StringEnd = ')';
        public static char Whitespace = ' ';
        public static char ArrayStart = '[';
        public static char ArrayEnd = ']';
        public static char LessThan = '<';
        public static char GreaterThan = '>';

        public static string PdfVersionPrefix = "PDF-";
        public static string ObjStart = "obj";
        public static string ObjEnd = "endobj";

        public static string DictionaryStart = "<<";
        public static string DictionaryEnd = ">>";
        public static string Trailer = "trailer";
        public static string StartXref = "startxref";
        public static string Xref = "xref";
        public static string IndirectReference = "R";
        public static string StreamStart = "stream";
        public static string StreamEnd = "endstream";
        public static string Eof = "%%EOF";
        
        public static byte[] BinaryCharacters = new byte[] { 129, 130, 131, 132 };

        /// <summary>
        /// Special characters used to delimit syntactic entities such as arrays, names, comments.
        /// </summary>
        public static char[] Delimiters = new char[] { '(', ')', '<', '>', '[', ']', '{', '}', '/', '%' };

        public static char[] WhitespaceCharacters = new char[] { Space, Tab, NewLine, CarriageReturn, '\f' };
    }
}
