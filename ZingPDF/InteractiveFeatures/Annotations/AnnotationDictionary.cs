using ZingPDF.ObjectModel;
using ZingPDF.ObjectModel.CommonDataStructures;
using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.InteractiveFeatures.Annotations
{
    public class AnnotationDictionary : Dictionary
    {
        public static class Subtypes
        {
            public const string Widget = "Widget";
        }

        public AnnotationDictionary(Name subtype) : base(Constants.DictionaryTypes.Annot)
        {
            ArgumentNullException.ThrowIfNull(subtype);

            Set(Constants.DictionaryKeys.Subtype, subtype);
        }

        protected AnnotationDictionary(Dictionary dict) : base(dict) { }

        /// <summary>
        /// <para>(Required)</para>
        /// <para>The type of annotation that this dictionary describes; see "Table 171 — Annotation types" for specific values.</para>
        /// </summary>
        public Name Subtype => Get<Name>(Constants.DictionaryKeys.Subtype)!;

        /// <summary>
        /// <para>(Required)</para>
        /// <para>The annotation rectangle, defining the location of the annotation on the page in default user space units.</para>
        /// </summary>
        public Rectangle Rect => Get<Rectangle>(Constants.DictionaryKeys.Annotation.Rect)!;

        /// <summary>
        /// <para>(Optional)</para>
        /// <para>Text that shall be displayed for the annotation or, if this type of annotation does not display text, 
        /// an alternative description of the annotation’s contents in human-readable form. In either case, this 
        /// text is useful when extracting the document’s contents in support of accessibility to users with disabilities 
        /// or for other purposes (see 14.9.3, "Alternate descriptions"). See 12.5.6, "Annotation types" for more details 
        /// on the meaning of this entry for each annotation type.</para>
        /// </summary>
        public LiteralString? Contents => Get<LiteralString>(Constants.DictionaryKeys.Annotation.Contents);

        /// <summary>
        /// <para>(Optional except as noted below; PDF 1.3; not used in FDF files)</para>
        /// <para>An indirect reference to the page object with which this annotation is associated.
        /// This entry shall be present in screen annotations associated with rendition actions 
        /// (PDF 1.5; see 12.5.6.18, "Screen annotations" and 12.6.4.14, "Rendition actions").</para>
        /// </summary>
        public Dictionary? P => Get<Dictionary>(Constants.DictionaryKeys.Annotation.P);

        /// <summary>
        /// <para>(Optional; PDF 1.4)</para>
        /// <para>The annotation name, a text string uniquely identifying it among all the annotations on its page.</para>
        /// </summary>
        public LiteralString? NM => Get<LiteralString>(Constants.DictionaryKeys.Annotation.NM);

        /// <summary>
        /// <para>(Optional; PDF 1.1)</para>
        /// <para>The date and time when the annotation was most recently modified. 
        /// The format should be a date string as described in 7.9.4, "Dates" 
        /// but interactive PDF processors shall accept and display a string in any format.</para>
        /// </summary>
        public IPdfObject? M => Get<IPdfObject>(Constants.DictionaryKeys.Annotation.M);

        /// <summary>
        /// <para>(Optional; PDF 1.1)</para>
        /// <para>A set of flags specifying various characteristics of the annotation (see 12.5.3, "Annotation flags").</para>
        /// <para>Default value: 0.</para>
        /// </summary>
        public Integer? F => Get<Integer>(Constants.DictionaryKeys.Annotation.F);

        /// <summary>
        /// <para>(Required except for conditions below (PDF 2.0); optional in PDF 1.2 to PDF 1.7)</para>
        /// <para>An appearance dictionary specifying how the annotation shall be presented visually 
        /// on the page (see 12.5.5, "Appearance streams"). A PDF writer shall include an appearance 
        /// dictionary when writing or updating the PDF file except for the two cases listed below.</para>
        /// <para>Every annotation (including those whose Subtype value is Widget, as used for form fields), 
        /// except for the two cases listed below, shall have at least one appearance dictionary.</para>
        /// <para>• Annotations where the value of the Rect key consists of an array where the value at 
        /// index 0 is equal to the value at index 2 and the value at index 1 is equal to the value at index 3.</para>
        /// <para> NOTE (2020) The bullet point above was changed from “or” to “and” in this document to match 
        /// requirements in other published ISO PDF standards (such as PDF/A).</para>
        /// <para>• Annotations whose Subtype value is Popup, Projection or Link.</para>
        /// </summary>
        public Dictionary? AP => Get<Dictionary>(Constants.DictionaryKeys.Annotation.AP);

        /// <summary>
        /// <para>(Required if the appearance dictionary AP contains one or more subdictionaries; PDF 1.2)</para>
        /// <para>The annotation’s appearance state, which selects the applicable appearance stream from an 
        /// appearance subdictionary (see 12.5.5, "Appearance streams").</para>
        /// </summary>
        public Name? AS => Get<Name>(Constants.DictionaryKeys.Annotation.AS);

        /// <summary>
        /// <para>(Optional)</para>
        /// <para>An array specifying the characteristics of the annotation’s border, which shall be drawn as a rounded rectangle.</para>
        /// <para>(PDF 1.0) The array consists of three numbers defining the horizontal corner radius, 
        /// vertical corner radius, and border width, all in default user space units. If the corner radii 
        /// are 0, the border has square (not rounded) corners; if the border width is 0, no border is drawn.</para>
        /// <para>(PDF 1.1) The array may have a fourth element, an optional dash array defining a pattern of dashes and 
        /// gaps that shall be used in drawing the border. The dash array shall be specified in the same format as 
        /// in the line dash pattern parameter of the graphics state (see 8.4.3.6, "Line dash pattern"). The dash 
        /// phase shall not be specified and shall be assumed to be 0.</para>
        /// <para>EXAMPLE A Border value of [0 0 1 [3 2]] specifies a border 1 unit wide, with square corners, drawn 
        /// with 3-unit dashes alternating with 2- unit gaps.</para>
        /// <para>NOTE (PDF 1.2) The dictionaries for some annotation types (such as free text and polygon annotations) 
        /// can include the BS entry. That entry specifies a border style dictionary that has more settings than 
        /// the array specified for the Border entry. If an annotation dictionary includes the BS entry, then the 
        /// Border entry is ignored.</para>
        /// <para>Default value: [0 0 1].</para>
        /// </summary>
        public ArrayObject? Border => Get<ArrayObject>(Constants.DictionaryKeys.Annotation.Border);

        /// <summary>
        /// <para>(Optional; PDF 1.1)</para>
        /// <para>An array of numbers in the range 0.0 to 1.0, representing a colour used for the following purposes:</para>
        /// <para>The background of the annotation’s icon when closed</para>
        /// <para>The title bar of the annotation’s popup window</para>
        /// <para>The border of a link annotation</para>
        /// <para>The number of array elements determines the colour space in which the colour shall be defined:</para>
        /// <para>0 No colour; transparent</para>
        /// <para>1 DeviceGray</para>
        /// <para>3 DeviceRGB</para>
        /// <para>4 DeviceCMYK</para>
        /// </summary>
        public ArrayObject? C => Get<ArrayObject>(Constants.DictionaryKeys.Annotation.C);

        /// <summary>
        /// <para>(Required if the annotation is a structural content item; PDF 1.3)</para>
        /// <para>The integer key of the annotation’s entry in the structural parent tree 
        /// (see 14.7.5.4, "Finding structure elements from content items").</para>
        /// </summary>
        public Integer? StructParent => Get<Integer>(Constants.DictionaryKeys.Annotation.StructParent);

        /// <summary>
        /// <para>(Optional; PDF 1.5)</para>
        /// <para>An optional content group or optional content membership 
        /// dictionary (see 8.11, "Optional content") specifying the optional content 
        /// properties for the annotation. Before the annotation is drawn, its visibility 
        /// shall be determined based on this entry as well as the annotation flags specified 
        /// in the F entry (see 12.5.3, "Annotation flags"). If it is determined to be invisible, 
        /// the annotation shall not be drawn. (See 8.11.3.3, "Optional content in XObjects and annotations".)</para>
        /// </summary>
        public Dictionary? OC => Get<Dictionary>(Constants.DictionaryKeys.Annotation.OC);

        /// <summary>
        /// <para>(Optional; PDF 2.0)</para>
        /// <para>An array of one or more file specification dictionaries 
        /// (7.11.3, "File specification dictionaries") which denote the associated files for this annotation. 
        /// See 14.13, "Associated files" and 14.13.9, "Associated files linked to an annotation dictionary" 
        /// for more details.</para>
        /// </summary>
        public ArrayObject? AF => Get<ArrayObject>(Constants.DictionaryKeys.Annotation.AF);

        /// <summary>
        /// <para>(Optional; PDF 2.0)</para>
        /// <para>When regenerating the annotation's appearance stream, this is the opacity value 
        /// (11.2, "Overview of transparency") that shall be used for all nonstroking operations 
        /// on all visible elements of the annotation in its closed state (including its background and border) 
        /// but not the popup window that appears when the annotation is opened.</para>
        /// <para>Default value: 1.0</para>
        /// <para>The specified value shall not be used if the annotation has an appearance stream
        /// (see 12.5.5, "Appearance streams"); in that case, the appearance stream shall specify any transparency.</para>
        /// <para>If no explicit appearance stream is defined for the annotation, and the processor is not able to 
        /// regenerate the appearance, the annotation may be painted by implementation-dependent means that do not 
        /// necessarily conform to the PDF imaging model; in this case, the effect of this entry is 
        /// implementation-dependent as well.</para>
        /// </summary>
        public RealNumber? ca => Get<RealNumber>(Constants.DictionaryKeys.Annotation.ca);

        /// <summary>
        /// <para>(Optional; PDF 1.4, PDF 2.0 for non-markup annotations)</para>
        /// <para>When regenerating the annotation's appearance stream, this is the opacity value 
        /// (11.2, "Overview of transparency") that shall be used for stroking all visible elements 
        /// of the annotation in its closed state, including its background and border, but not the 
        /// popup window that appears when the annotation is opened.</para>
        /// <para>If a ca entry is not present in this dictionary, then the value of this CA entry 
        /// shall also be used for nonstroking operations as well. Default Value: 1.0</para>
        /// <para>The specified value shall not be used if the annotation has an appearance stream 
        /// (12.5.5, "Appearance streams"); in that case, the appearance stream shall specify any transparency.</para>
        /// <para>If no explicit appearance stream is defined for the annotation, and the processor is not able 
        /// to regenerate the appearance, the annotation may be painted by implementation-dependent means that 
        /// do not necessarily conform to the PDF imaging model; in this case, the effect of this entry is 
        /// implementation-dependent as well.</para>
        /// </summary>
        public RealNumber? CA => Get<RealNumber>(Constants.DictionaryKeys.Annotation.CA);

        /// <summary>
        /// <para>(Optional; PDF 2.0)</para>
        /// <para>The blend mode that shall be used when painting the annotation onto the page 
        /// (see 11.3.5, "Blend Mode" and 11.6.3, "Specifying Blending Colour Space and Blend Mode"). 
        /// If this key is not present, blending shall take place using the Normal blend mode. The value shall 
        /// be a name object, designating one of the standard blend modes listed in "Table 134 — Standard 
        /// separable blend modes" and "Table 135 — Standard non-separable blend modes" in 11.3.5, "Blend mode".</para>
        /// </summary>
        public Name? BM => Get<Name>(Constants.DictionaryKeys.Annotation.BM);

        /// <summary>
        /// <para>(Optional; PDF 2.0)</para>
        /// <para>A language identifier overriding the document’s language identifier to specify the natural language 
        /// for all text in the annotation except where overridden by other explicit language specifications 
        /// (see 14.9.2, "Natural language specification").</para>
        /// </summary>
        public LiteralString? Lang => Get<LiteralString>(Constants.DictionaryKeys.Annotation.Lang);
    }
}
