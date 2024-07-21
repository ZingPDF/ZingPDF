using ZingPDF.ObjectModel.Objects;

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
        public const string Subtype = "Subtype";

        public static class DocumentCatalog
        {
            public const string Version = "Version";
            public const string Extensions = "Extensions";
            public const string Pages = "Pages";
            public const string PageLabels = "PageLabels";

            public const string AcroForm = "AcroForm";
        }

        public static class LinearizationParameter
        {
            public const string Linearized = "Linearized";
            public const string L = "L";
            public const string H = "H";
            public const string O = "O";
            public const string E = "E";
            public const string N = "N";
            public const string T = "T";
            public const string P = "P";
        }

        public static class ObjectStream
        {
            public const string N = "N";
            public const string First = "First";
            public const string Extends = "Extends";
        }
        
        public static class Stream
        {
            public const string Length = "Length";
            public const string Filter = "Filter";
            public const string DecodeParms = "DecodeParms";
            public const string F = "F";
            public const string FFilter = "FFilter";
            public const string FDecodeParms = "FDecodeParms";
            public const string DL = "DL";
        }

        public static class CrossReferenceStream
        {
            public const string Index = "Index";
            public const string W = "W";
        }

        public static class PageTreeNode
        {
            public const string Parent = "Parent";
            public const string Kids = "Kids";
            public const string Count = "Count";
        }

        public static class Page
        {
            public const string Parent = "Parent";
            public const string Resources = "Resources";
            public const string MediaBox = "MediaBox";
            public const string CropBox = "CropBox";
            public const string BleedBox = "BleedBox";
            public const string TrimBox = "TrimBox";
            public const string ArtBox = "ArtBox";
            public const string Contents = "Contents";
            public const string Rotate = "Rotate";
        }

        public static class Resource
        {
            public const string ExtGState = "ExtGState";
            public const string ColorSpace = "ColorSpace";
            public const string Pattern = "Pattern";
            public const string Shading = "Shading";
            public const string XObject = "XObject";
            public const string Font = "Font";
            public const string ProcSet = "ProcSet";
            public const string Properties = "Properties";
        }

        public static class InteractiveForm
        {
            public const string Fields = "Fields";
            public const string NeedAppearances = "NeedAppearances";
            public const string SigFlags = "SigFlags";
            public const string CO = "CO";
            public const string DR = "DR";
            public const string DA = "DA";
            public const string Q = "Q";
            public const string XFA = "XFA";
        }

        public static class Field
        {
            public const string FT = "FT";
            public const string Parent = "Parent";
            public const string Kids = "Kids";
            public const string T = "T";
            public const string TU = "TU";
            public const string TM = "TM";
            public const string Ff = "Ff";
            public const string V = "V";
            public const string DV = "DV";
            public const string AA = "AA";
        }

        public static class Annotation
        {
            public const string Rect = "Rect";
            public const string Contents = "Contents";
            public const string P = "P";
            public const string NM = "NM";
            public const string M = "M";
            public const string F = "F";
            public const string AP = "AP";
            public const string AS = "AS";
            public const string Border = "Border";
            public const string C = "C";
            public const string StructParent = "StructParent";
            public const string OC = "OC";
            public const string AF = "AF";
            public const string ca = "ca";
            public const string CA = "CA";
            public const string BM = "BM";
            public const string Lang = "Lang";
        }

        public static class WidgetAnnotation
        {
            public const string H = "H";
            public const string MK = "MK";
            public const string A = "A";
            public const string AA = "AA";
            public const string BS = "BS";
            public const string Parent = "Parent";
        }
        
        public static class Appearance
        {
            public const string N = "N";
            public const string R = "R";
            public const string D = "D";
        }

        public static class Form
        {
            public const string FormType = "FormType";

            public static class Type1
            {
                public const string BBox = "BBox";
                public const string Matrix = "Matrix";
                public const string Resources = "Resources";
                public const string Group = "Group";
                public const string Ref = "Ref";
                public const string Metadata = "Metadata";
                public const string PieceInfo = "PieceInfo";
                public const string LastModified = "LastModified";
                public const string StructParent = "StructParent";
                public const string StructParents = "StructParents";
                public const string OPI = "OPI";
                public const string OC = "OC";
                public const string Name = "Name";
                public const string AF = "AF";
                public const string Measure = "Measure";
                public const string PtData = "PtData";
            }
        }

        public static class GraphicsStateParameter
        {
            public const string LW = "LW";
            public const string LC = "LC";
            public const string LJ = "LJ";
            public const string ML = "ML";
            public const string D = "D";
            public const string RI = "RI";
            public const string OP = "OP";
            public const string op = "op";
            public const string OPM = "OPM";
            public const string Font = "Font";
            public const string BG = "BG";
            public const string BG2 = "BG2";
            public const string UCR = "UCR";
            public const string UCR2 = "UCR2";
            public const string TR = "TR";
            public const string TR2 = "TR2";
            public const string HT = "HT";
            public const string FL = "FL";
            public const string SM = "SM";
            public const string SA = "SA";
            public const string BM = "BM";
            public const string SMask = "SMask";
            public const string CA = "CA";
            public const string ca = "ca";
            public const string AIS = "AIS";
            public const string TK = "TK";
            public const string UseBlackPtComp = "UseBlackPtComp";
            public const string HTO = "HTO";
        }

        public static class Function
        {
            public const string FunctionType = "FunctionType";
            public const string Domain = "Domain";
            public const string Range = "Range";

            public static class Type0
            {
                public const string Size = "Size";
                public const string BitsPerSample = "BitsPerSample";
                public const string Order = "Order";
                public const string Encode = "Encode";
                public const string Decode = "Decode";
            }
        }
    }

    public static class DictionaryTypes
    {
        public const string Catalog = "Catalog"; // Document Catalog
        public const string Pages = "Pages"; // Page Tree Node
        public const string Page = "Page"; // Page
        public const string XRef = "XRef"; // Cross Reference
        public const string ObjStm = "ObjStm"; // Object Stream
        public const string Extensions = "Extensions"; // Extensions
        public const string Annot = "Annot"; // Annotations
        public const string ExtGState = "ExtGState"; // Graphics State Parameter
        public const string XObject = "XObject"; // Form Dictionary (Form XObjects)
        public const string Metadata = "Metadata"; // Metadata Stream
    }

    public static class FunctionTypes
    {
        public const int Zero = 0;
        public const int Two = 2;
        public const int Three = 3;
        public const int Four = 4;
    }

    public static class ContentStreamOperators
    {
        /// <summary>
        /// Close, fill, and stroke path using non-zero winding number rule
        /// </summary>
        public const string b = "b";

        /// <summary>
        /// Fill and stroke path using non-zero winding number rule
        /// </summary>
        public const string B = "B";

        /// <summary>
        /// Close, fill, and stroke path using even-odd rule
        /// </summary>
        public const string bStar = "b*";

        /// <summary>
        /// Fill and stroke path using even-odd rule
        /// </summary>
        public const string BStar = "B*";

        /// <summary>
        /// (PDF 1.2) Begin marked-content sequence with property list
        /// </summary>
        public const string BDC = "BDC";

        /// <summary>
        /// Begin inline image object
        /// </summary>
        public const string BI = "BI";

        /// <summary>
        /// (PDF 1.2) Begin marked-content sequence
        /// </summary>
        public const string BMC = "BMC";

        /// <summary>
        /// Begin text object
        /// </summary>
        public const string BT = "BT";

        /// <summary>
        /// (PDF 1.1) Begin compatibility section
        /// </summary>
        public const string BX = "BX";

        /// <summary>
        /// Append curved segment to path (three control points)
        /// </summary>
        public const string c = "c";

        /// <summary>
        /// Concatenate matrix to current transformation matrix
        /// </summary>
        public const string cm = "cm";

        /// <summary>
        /// (PDF 1.1) Set colour space for stroking operations
        /// </summary>
        public const string CS = "CS";

        /// <summary>
        /// (PDF 1.1) Set colour space for nonstroking operations
        /// </summary>
        public const string cs = "cs";

        /// <summary>
        /// Set line dash pattern
        /// </summary>
        public const string d = "d";

        /// <summary>
        /// Set glyph width in Type 3 font
        /// </summary>
        public const string d0 = "d0";

        /// <summary>
        /// Set glyph width and bounding box in Type 3 font
        /// </summary>
        public const string d1 = "d1";

        /// <summary>
        /// Invoke named XObject
        /// </summary>
        public const string Do = "Do";

        /// <summary>
        /// (PDF 1.2) Define marked-content point with property list
        /// </summary>
        public const string DP = "DP";

        /// <summary>
        /// End inline image object
        /// </summary>
        public const string EI = "EI";

        /// <summary>
        /// (PDF 1.2) End marked-content sequence
        /// </summary>
        public const string EMC = "EMC";

        /// <summary>
        /// End text object
        /// </summary>
        public const string ET = "ET";

        /// <summary>
        /// (PDF 1.1) End compatibility section
        /// </summary>
        public const string EX = "EX";

        /// <summary>
        /// Fill path using non-zero winding number rule
        /// </summary>
        public const string f = "f";

        /// <summary>
        /// Fill path using non-zero winding number rule (deprecated in PDF 2.0)
        /// </summary>
        public const string F = "F";

        /// <summary>
        /// Fill path using even-odd rule
        /// </summary>
        public const string fStar = "f*";

        /// <summary>
        /// Set gray level for stroking operations
        /// </summary>
        public const string G = "G";

        /// <summary>
        /// Set gray level for nonstroking operations
        /// </summary>
        public const string g = "g";

        /// <summary>
        /// (PDF 1.2) Set parameters from graphics state parameter dictionary
        /// </summary>
        public const string gs = "gs";

        /// <summary>
        /// Close subpath
        /// </summary>
        public const string h = "h";

        /// <summary>
        /// Set flatness tolerance
        /// </summary>
        public const string i = "i";

        /// <summary>
        /// Begin inline image data
        /// </summary>
        public const string ID = "ID";

        /// <summary>
        /// Set line join style
        /// </summary>
        public const string j = "j";

        /// <summary>
        /// Set line cap style
        /// </summary>
        public const string J = "J";

        /// <summary>
        /// Set CMYK colour for stroking operations
        /// </summary>
        public const string K = "K";

        /// <summary>
        /// Set CMYK colour for nonstroking operations
        /// </summary>
        public const string k = "k";

        /// <summary>
        /// Append straight line segment to path
        /// </summary>
        public const string l = "l";

        /// <summary>
        /// Begin new subpath
        /// </summary>
        public const string m = "m";

        /// <summary>
        /// Set miter limit
        /// </summary>
        public const string M = "M";

        /// <summary>
        /// (PDF 1.2) Define marked-content point
        /// </summary>
        public const string MP = "MP";

        /// <summary>
        /// End path without filling or stroking
        /// </summary>
        public const string n = "n";

        /// <summary>
        /// Save graphics state
        /// </summary>
        public const string q = "q";

        /// <summary>
        /// Restore graphics state
        /// </summary>
        public const string Q = "Q";

        /// <summary>
        /// Append rectangle to path
        /// </summary>
        public const string re = "re";

        /// <summary>
        /// Set RGB colour for stroking operations
        /// </summary>
        public const string RG = "RG";

        /// <summary>
        /// Set RGB colour for nonstroking operations
        /// </summary>
        public const string rg = "rg";

        /// <summary>
        /// Set colour rendering intent
        /// </summary>
        public const string ri = "ri";

        /// <summary>
        /// Close and stroke path
        /// </summary>
        public const string s = "s";

        /// <summary>
        /// Stroke path
        /// </summary>
        public const string S = "S";

        /// <summary>
        /// (PDF 1.1) Set colour for stroking operations
        /// </summary>
        public const string SC = "SC";

        /// <summary>
        /// (PDF 1.1) Set colour for nonstroking operations
        /// </summary>
        public const string sc = "sc";

        /// <summary>
        /// (PDF 1.2) Set colour for stroking operations (ICCBased and special colour spaces)
        /// </summary>
        public const string SCN = "SCN";

        /// <summary>
        /// (PDF 1.2) Set colour for nonstroking operations (ICCBased and special colour spaces)
        /// </summary>
        public const string scn = "scn";

        /// <summary>
        /// (PDF 1.3) Paint area defined by shading pattern
        /// </summary>
        public const string sh = "sh";

        /// <summary>
        /// Move to start of next text line
        /// </summary>
        public const string TStar = "T*";

        /// <summary>
        /// Set character spacing
        /// </summary>
        public const string Tc = "Tc";

        /// <summary>
        /// Move text position
        /// </summary>
        public const string Td = "Td";

        /// <summary>
        /// Move text position and set leading
        /// </summary>
        public const string TD = "TD";

        /// <summary>
        /// Set text font and size
        /// </summary>
        public const string Tf = "Tf";

        /// <summary>
        /// Show text
        /// </summary>
        public const string Tj = "Tj";

        /// <summary>
        /// Show text, allowing individual glyph positioning
        /// </summary>
        public const string TJ = "TJ";

        /// <summary>
        /// Set text leading
        /// </summary>
        public const string TL = "TL";

        /// <summary>
        /// Set text matrix and text line matrix
        /// </summary>
        public const string Tm = "Tm";

        /// <summary>
        /// Set text rendering mode
        /// </summary>
        public const string Tr = "Tr";

        /// <summary>
        /// Set text rise
        /// </summary>
        public const string Ts = "Ts";

        /// <summary>
        /// Set word spacing
        /// </summary>
        public const string Tw = "Tw";

        /// <summary>
        /// Set horizontal text scaling
        /// </summary>
        public const string Tz = "Tz";

        /// <summary>
        /// Append curved segment to path (initial point replicated)
        /// </summary>
        public const string v = "v";

        /// <summary>
        /// Set line width
        /// </summary>
        public const string w = "w";

        /// <summary>
        /// Set clipping path using non-zero winding number rule
        /// </summary>
        public const string W = "W";

        /// <summary>
        /// Set clipping path using even-odd rule
        /// </summary>
        public const string WStar = "W*";

        /// <summary>
        /// Append curved segment to path (final point replicated)
        /// </summary>
        public const string y = "y";

        /// <summary>
        /// Move to next line and show text
        /// </summary>
        public const string Apostrophe = "'";

        /// <summary>
        /// Set word and character spacing, move to next line, and show text
        /// </summary>
        public const string Quote = "\"";
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
