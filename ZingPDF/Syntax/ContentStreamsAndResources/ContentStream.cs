using ZingPDF.Extensions;

namespace ZingPDF.Syntax.ContentStreamsAndResources;

public class ContentStream(ObjectContext context)
    : PdfObject(context)
{
    public ContentStream(IEnumerable<ContentStreamOperation> operations, ObjectContext context)
        : this(context)
    {
        ArgumentNullException.ThrowIfNull(operations);

        Operations = [.. operations];
    }

    public ContentStream(IEnumerable<ContentStreamOperation> operations)
        : this(operations, ObjectContext.UserCreated)
    {
    }

    public ContentStream()
        : this(ObjectContext.UserCreated)
    {
    }

    public List<ContentStreamOperation> Operations { get; } = [];

    public override object Clone()
    {
        return new ContentStream(
            [.. Operations.Select(x => (ContentStreamOperation)x.Clone())],
            Context
            );
    }

    protected override async Task WriteOutputAsync(Stream stream)
    {
        for (int i = 0; i < Operations.Count; i++)
        {
            ContentStreamOperation? instruction = Operations[i];
            if (instruction.Operands != null)
            {
                foreach (var operand in instruction.Operands)
                {
                    await operand.WriteAsync(stream);
                    await stream.WriteWhitespaceAsync();
                }
            }

            await stream.WriteTextAsync(instruction.Operator);

            if (i < Operations.Count - 1)
            {
                await stream.WriteWhitespaceAsync();
            }
        }
    }

    #region Operators

    internal static class Operators
    {
        public static class GeneralGraphicsState
        {
            /// <summary>
            /// Set line width
            /// </summary>
            public const string w = "w";

            /// <summary>
            /// Set line cap style
            /// </summary>
            public const string J = "J";

            /// <summary>
            /// Set line join style
            /// </summary>
            public const string j = "j";

            /// <summary>
            /// Set miter limit
            /// </summary>
            public const string M = "M";

            /// <summary>
            /// Set line dash pattern
            /// </summary>
            public const string d = "d";

            /// <summary>
            /// Set colour rendering intent
            /// </summary>
            public const string ri = "ri";

            /// <summary>
            /// Set flatness tolerance
            /// </summary>
            public const string i = "i";

            /// <summary>
            /// (PDF 1.2) Set parameters from graphics state parameter dictionary
            /// </summary>
            public const string gs = "gs";

            /// <summary>
            /// Save graphics state
            /// </summary>
            public const string q = "q";

            /// <summary>
            /// Restore graphics state
            /// </summary>
            public const string Q = "Q";

            public static IEnumerable<string> All = [w, J, j, M, d, ri, i, gs, q, Q];
        }

        public static class SpecialGraphicsState
        {
            /// <summary>
            /// Concatenate matrix to current transformation matrix
            /// </summary>
            public const string cm = "cm";
        }

        public static class PathConstruction
        {
            /// <summary>
            /// Begin new subpath
            /// </summary>
            public const string m = "m";

            /// <summary>
            /// Append straight line segment to path
            /// </summary>
            public const string l = "l";

            /// <summary>
            /// Append curved segment to path (three control points)
            /// </summary>
            public const string c = "c";

            /// <summary>
            /// Append curved segment to path (initial point replicated)
            /// </summary>
            public const string v = "v";

            /// <summary>
            /// Append curved segment to path (final point replicated)
            /// </summary>
            public const string y = "y";

            /// <summary>
            /// Close subpath
            /// </summary>
            public const string h = "h";

            /// <summary>
            /// Append rectangle to path
            /// </summary>
            public const string re = "re";

            public static IEnumerable<string> All = [m, l, c, v, y, h, re];
        }

        public static class PathPainting
        {
            /// <summary>
            /// Stroke path
            /// </summary>
            public const string S = "S";

            /// <summary>
            /// Close and stroke path
            /// </summary>
            public const string s = "s";

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
            /// Fill and stroke path using non-zero winding number rule
            /// </summary>
            public const string B = "B";

            /// <summary>
            /// Fill and stroke path using even-odd rule
            /// </summary>
            public const string BStar = "B*";

            /// <summary>
            /// Close, fill, and stroke path using non-zero winding number rule
            /// </summary>
            public const string b = "b";

            /// <summary>
            /// Close, fill, and stroke path using even-odd rule
            /// </summary>
            public const string bStar = "b*";

            /// <summary>
            /// End path without filling or stroking
            /// </summary>
            public const string n = "n";

            public static IEnumerable<string> All = [S, s, F, f, fStar, B, BStar, b, bStar, n];
        }

        public static class ClippingPaths
        {
            /// <summary>
            /// Set clipping path using non-zero winding number rule
            /// </summary>
            public const string W = "W";

            /// <summary>
            /// Set clipping path using even-odd rule
            /// </summary>
            public const string WStar = "W*";

            public static IEnumerable<string> All = [W, WStar];
        }

        public static class TextObjects
        {
            /// <summary>
            /// Begin text object
            /// </summary>
            public const string BT = "BT";

            /// <summary>
            /// End text object
            /// </summary>
            public const string ET = "ET";

            public static IEnumerable<string> All = [BT, ET];
        }

        public static class TextState
        {
            /// <summary>
            /// Set character spacing
            /// </summary>
            public const string Tc = "Tc";

            /// <summary>
            /// Set word spacing
            /// </summary>
            public const string Tw = "Tw";

            /// <summary>
            /// Set horizontal text scaling
            /// </summary>
            public const string Tz = "Tz";

            /// <summary>
            /// Set text leading
            /// </summary>
            public const string TL = "TL";

            /// <summary>
            /// Set text font and size
            /// </summary>
            public const string Tf = "Tf";

            /// <summary>
            /// Set text rendering mode
            /// </summary>
            public const string Tr = "Tr";

            /// <summary>
            /// Set text rise
            /// </summary>
            public const string Ts = "Ts";

            public static IEnumerable<string> All = [Tc, Tw, Tz, TL, Tf, Tr, Ts];
        }

        public static class TextPositioning
        {
            /// <summary>
            /// Move text position
            /// </summary>
            public const string Td = "Td";

            /// <summary>
            /// Move text position and set leading
            /// </summary>
            public const string TD = "TD";

            /// <summary>
            /// Set text matrix and text line matrix
            /// </summary>
            public const string Tm = "Tm";

            /// <summary>
            /// Move to start of next text line
            /// </summary>
            public const string TStar = "T*";

            public static IEnumerable<string> All = [Td, TD, Tm, TStar];
        }

        public static class TextShowing
        {
            /// <summary>
            /// Show text
            /// </summary>
            public const string Tj = "Tj";

            /// <summary>
            /// Show text, allowing individual glyph positioning
            /// </summary>
            public const string TJ = "TJ";

            /// <summary>
            /// Move to next line and show text
            /// </summary>
            public const string Apostrophe = "'";

            /// <summary>
            /// Set word and character spacing, move to next line, and show text
            /// </summary>
            public const string Quote = "\"";

            public static IEnumerable<string> All = [Tj, TJ, Apostrophe, Quote];
        }

        public static class Type3Fonts
        {
            /// <summary>
            /// Set glyph width in Type 3 font
            /// </summary>
            public const string d0 = "d0";

            /// <summary>
            /// Set glyph width and bounding box in Type 3 font
            /// </summary>
            public const string d1 = "d1";

            public static IEnumerable<string> All = [d0, d1];
        }

        public static class Colour
        {
            /// <summary>
            /// (PDF 1.1) Set colour space for stroking operations
            /// </summary>
            public const string CS = "CS";

            /// <summary>
            /// (PDF 1.1) Set colour space for nonstroking operations
            /// </summary>
            public const string cs = "cs";

            /// <summary>
            /// (PDF 1.1) Set colour for stroking operations
            /// </summary>
            public const string SC = "SC";

            /// <summary>
            /// (PDF 1.2) Set colour for stroking operations (ICCBased and special colour spaces)
            /// </summary>
            public const string SCN = "SCN";

            /// <summary>
            /// (PDF 1.1) Set colour for nonstroking operations
            /// </summary>
            public const string sc = "sc";

            /// <summary>
            /// (PDF 1.2) Set colour for nonstroking operations (ICCBased and special colour spaces)
            /// </summary>
            public const string scn = "scn";

            /// <summary>
            /// Set gray level for stroking operations
            /// </summary>
            public const string G = "G";

            /// <summary>
            /// Set gray level for nonstroking operations
            /// </summary>
            public const string g = "g";

            /// <summary>
            /// Set RGB colour for stroking operations
            /// </summary>
            public const string RG = "RG";

            /// <summary>
            /// Set RGB colour for nonstroking operations
            /// </summary>
            public const string rg = "rg";

            /// <summary>
            /// Set CMYK colour for stroking operations
            /// </summary>
            public const string K = "K";

            /// <summary>
            /// Set CMYK colour for nonstroking operations
            /// </summary>
            public const string k = "k";

            public static IEnumerable<string> All = [cs, CS, SC, SCN, sc, scn, G, g, RG, rg, K, k];
        }

        public static class ShadingPatterns
        {
            /// <summary>
            /// (PDF 1.3) Paint area defined by shading pattern
            /// </summary>
            public const string sh = "sh";
        }

        public static class InlineImages
        {
            /// <summary>
            /// Begin inline image object
            /// </summary>
            public const string BI = "BI";

            /// <summary>
            /// Begin inline image data
            /// </summary>
            public const string ID = "ID";

            /// <summary>
            /// End inline image object
            /// </summary>
            public const string EI = "EI";

            public static IEnumerable<string> All = [BI, ID, EI];
        }

        public static class XObjects
        {
            /// <summary>
            /// Invoke named XObject
            /// </summary>
            public const string Do = "Do";
        }

        public static class MarkedContent
        {
            /// <summary>
            /// (PDF 1.2) Define marked-content point
            /// </summary>
            public const string MP = "MP";

            /// <summary>
            /// (PDF 1.2) Define marked-content point with property list
            /// </summary>
            public const string DP = "DP";

            /// <summary>
            /// (PDF 1.2) Begin marked-content sequence
            /// </summary>
            public const string BMC = "BMC";

            /// <summary>
            /// (PDF 1.2) Begin marked-content sequence with property list
            /// </summary>
            public const string BDC = "BDC";

            /// <summary>
            /// (PDF 1.2) End marked-content sequence
            /// </summary>
            public const string EMC = "EMC";

            public static IEnumerable<string> All = [MP, DP, BMC, BDC, EMC];
        }

        public static class Compatibility
        {
            /// <summary>
            /// (PDF 1.1) Begin compatibility section
            /// </summary>
            public const string BX = "BX";

            /// <summary>
            /// (PDF 1.1) End compatibility section
            /// </summary>
            public const string EX = "EX";

            public static IEnumerable<string> All = [BX, EX];
        }

        public static IEnumerable<string> All = [
            ..GeneralGraphicsState.All,
                SpecialGraphicsState.cm,
                ..PathConstruction.All,
                ..PathPainting.All,
                ..ClippingPaths.All,
                ..TextObjects.All,
                ..TextState.All,
                ..TextPositioning.All,
                ..TextShowing.All,
                ..Type3Fonts.All,
                ..Colour.All,
                ShadingPatterns.sh,
                ..InlineImages.All,
                XObjects.Do,
                ..MarkedContent.All,
                ..Compatibility.All,
                ];
    }

    #endregion Operators
}
