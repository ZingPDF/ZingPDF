using ZingPDF.Syntax.Objects;

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

    public static class CheckboxStates
    {
        public static readonly Name Checked = "Yes";
        public static readonly Name NotChecked = "Off";
    }

    public static class Filters
    {
        public const string ASCII85 = "ASCII85Decode";
        public const string ASCIIHex = "ASCIIHexDecode";
        public const string LZW = "LZWDecode";
        public const string Flate = "FlateDecode";
        public const string RunLength = "RunLengthDecode";
        public const string DCT = "DCTDecode"; // JPEG
        public const string JPX = "JPXDecode"; // JPEG 2000
        public const string CCITT = "CCITTFaxDecode";
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
            public const string BoxColorInfo = "BoxColorInfo";
            public const string Contents = "Contents";
            public const string Rotate = "Rotate";
            public const string Group = "Group";
            public const string Thumb = "Thumb";
            public const string B = "B";
            public const string Dur = "Dur";
            public const string Trans = "Trans";
            public const string Annots = "Annots";
            public const string AA = "AA";
            public const string Metadata = "Metadata";
            public const string PieceInfo = "PieceInfo";
            public const string StructParents = "StructParents";
            public const string ID = "ID";
            public const string PZ = "PZ";
            public const string SeparationInfo = "SeparationInfo";
            public const string Tabs = "Tabs";
            public const string TemplateInstantiated = "TemplateInstantiated";
            public const string PresSteps = "PresSteps";
            public const string UserUnit = "UserUnit";
            public const string VP = "VP";
            public const string AF = "AF";
            public const string OutputIntents = "OutputIntents";
            public const string DPart = "DPart";
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
            public const string Opt = "Opt";
            public const string TI = "TI";
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

        public static class Image
        {
            public const string Width = "Width";
            public const string Height = "Height";
            public const string ColorSpace = "ColorSpace";
            public const string BitsPerComponent = "BitsPerComponent";
            public const string Intent = "Intent";
            public const string ImageMask = "ImageMask";
            public const string Mask = "Mask";
            public const string Decode = "Decode";
            public const string Interpolate = "Interpolate";
            public const string Alternates = "Alternates";
            public const string SMask = "SMask";
            public const string SMaskInData = "SMaskInData";
            public const string Name = "Name";
            public const string StructParent = "StructParent";
            public const string ID = "ID";
            public const string OPI = "OPI";
            public const string Metadata = "Metadata";
            public const string OC = "OC";
            public const string AF = "AF";
            public const string Measure = "Measure";
            public const string PtData = "PtData";
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

        public static class Font
        {
            public static class Type1
            {
                public const string Name = "Name";
                public const string BaseFont = "BaseFont";
                public const string FirstChar = "FirstChar";
                public const string LastChar = "LastChar";
                public const string Widths = "Widths";
                public const string FontDescriptor = "FontDescriptor";
                public const string Encoding = "Encoding";
                public const string ToUnicode = "ToUnicode";
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
        public const string Font = "Font"; // Font
    }

    public static class FunctionTypes
    {
        public const int Zero = 0;
        public const int Two = 2;
        public const int Three = 3;
        public const int Four = 4;
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
