namespace ZingPdf.Core
{
    internal static class Constants
    {
        public static string Comment = "%";
        public static string PdfVersionPrefix = "PDF-";
        public static string Solidus = "/";
        public static string StringStart = "(";
        public static string StringEnd = ")";
        public static string ObjStart = "obj";
        public static string ObjEnd = "endobj";
        public static string ArrayStart = "[";
        public static string ArrayEnd = "]";
        public static string DictionaryStart = "<<";
        public static string DictionaryEnd = ">>";
        public static string Trailer = "trailer";
        public static string StartXref = "startxref";
        public static string Xref = "xref";
        public static string IndirectReference = "R";
        public static string StreamStart = "stream";
        public static string StreamEnd = "endstream";
        public static string Eof = "%%EOF";
        public static string Whitespace = " ";
        public static byte[] BinaryCharacters = new byte[] { 129, 130, 131, 132 };

        /// <summary>
        /// Special characters used to delimit syntactic entities such as arrays, names, comments.
        /// </summary>
        public static char[] Delimiters = new char[] { '(', ')', '<', '>', '[', ']', '{', '}', '/', '%' };

        public static char[] WhitespaceCharacters = new char[] { ' ', '\t', '\n', '\r', '\f' };
    }
}
