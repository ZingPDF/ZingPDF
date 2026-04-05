namespace ZingPDF.Text;

[Flags]
public enum FontFlags
{
    /// <summary>
    /// All glyphs have the same width (as opposed to proportional or variable-pitch fonts, which have different widths).
    /// </summary>
    FixedPitch = 1 << 0,

    /// <summary>
    /// Glyphs have serifs, which are short strokes drawn at an angle on the top and bottom of glyph stems. (Sans serif fonts do not have serifs.)
    /// </summary>
    Serif = 1 << 1,

    /// <summary>
    /// Font contains glyphs outside the Standard Latin character set. This flag and the Nonsymbolic flag shall not both be set or both be clear.
    /// </summary>
    Symbolic = 1 << 2,

    /// <summary>
    /// Glyphs resemble cursive handwriting.
    /// </summary>
    Script = 1 << 3,

    /// <summary>
    /// Font uses the Standard Latin character set or a subset of it. This flag and the Symbolic flag shall not both be set or both be clear.
    /// </summary>
    NonSymbolic = 1 << 5,

    /// <summary>
    /// Glyphs have dominant vertical strokes that are slanted.
    /// </summary>
    Italic = 1 << 6,

    /// <summary>
    /// Font contains no lowercase letters; typically used for display purposes, such as for titles or headlines.
    /// </summary>
    AllCap = 1 << 16,

    /// <summary>
    /// Font contains both uppercase and lowercase letters. The uppercase letters are similar to those in the regular version of the same 
    /// typeface family. The glyphs for the lowercase letters have the same shapes as the corresponding uppercase letters, but they are 
    /// sized and their proportions adjusted so that they have the same size and stroke weight as lowercase glyphs in the same typeface family.
    /// </summary>
    SmallCap = 1 << 17,

    /// <summary>
    /// <para>
    /// The ForceBold flag (bit 19) shall determine whether bold glyphs shall be painted with extra pixels even at very small text sizes 
    /// by a PDF processor. If the ForceBold flag is set, features of bold glyphs may be thickened at small text sizes.
    /// </para>
    /// <para>
    /// NOTE Typically, when glyphs are painted at small sizes on very low-resolution devices such as display screens, features of bold 
    /// glyphs can appear only 1 pixel wide. Because this is the minimum feature width on a pixel-based device, ordinary (nonbold) glyphs 
    /// also appear with 1-pixel-wide features and therefore cannot be distinguished from bold glyphs.
    /// </para>
    /// </summary>
    ForceBold = 1 << 18
}
