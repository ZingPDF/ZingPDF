using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.InteractiveFeatures.Annotations
{
    internal class WidgetAnnotationDictionary : AnnotationDictionary
    {
        public WidgetAnnotationDictionary() : base(Subtypes.Widget) { }

        protected WidgetAnnotationDictionary(Dictionary dict) : base(dict) { }

        /// <summary>
        /// <para>(Optional)</para>
        /// <para>The annotation’s highlighting mode, the visual effect that shall be used when the mouse 
        /// button is pressed or held down inside its active area:</para>
        /// <para>N (None) No highlighting.</para>
        /// <para>I (Invert) Invert the colours used to display the contents of the annotation rectangle.</para>
        /// <para>O (Outline) Stroke the colours used to display the annotation border. That is, for each 
        /// colour channel in the colour space used for display of the annotation value, colour values shall 
        /// be transformed by the function 𝑓 (𝑥) = 1 – 𝑥 for display.</para>
        /// <para>P (Push) Display the annotation’s down appearance, if any (see 12.5.5, "Appearance streams"). 
        /// If no down appearance is defined, the contents of the annotation rectangle shall be offset to 
        /// appear as if it were being pushed below the surface of the page.</para>
        /// <para>T (Toggle) Same as P (which is preferred).</para>
        /// <para>A highlighting mode other than P shall override any down appearance defined for the annotation. 
        /// Default value: I.</para>
        /// </summary>
        public Name? H => Get<Name>(Constants.DictionaryKeys.WidgetAnnotation.H);

        /// <summary>
        /// <para>(Optional)</para>
        /// <para>An appearance characteristics dictionary (see "Table 192 — Entries in an 
        /// appearance characteristics dictionary") that shall be used in constructing a dynamic 
        /// appearance stream specifying the annotation’s visual presentation on the page.</para>
        /// <para>The name MK for this entry is of historical significance only and has no direct meaning.</para>
        /// </summary>
        public Dictionary? MK => Get<Dictionary>(Constants.DictionaryKeys.WidgetAnnotation.MK);

        /// <summary>
        /// <para>(Optional; PDF 1.1)</para>
        /// <para>An action that shall be performed when the annotation is activated (see 12.6, "Actions").</para>
        /// </summary>
        public Dictionary? A => Get<Dictionary>(Constants.DictionaryKeys.WidgetAnnotation.A);

        /// <summary>
        /// <para>(Optional; PDF 1.2)</para>
        /// <para>An additional-actions dictionary defining the annotation’s behaviour in response to various 
        /// trigger events (see 12.6.3, "Trigger events").</para>
        /// </summary>
        public Dictionary? AA => Get<Dictionary>(Constants.DictionaryKeys.WidgetAnnotation.AA);

        /// <summary>
        /// <para>(Optional; PDF 1.2)</para>
        /// <para>A border style dictionary (see "Table 168 — Entries in a border style dictionary") specifying 
        /// the width and dash pattern that shall be used in drawing the annotation’s border.</para>
        /// </summary>
        public Dictionary? BS => Get<Dictionary>(Constants.DictionaryKeys.WidgetAnnotation.BS);

        /// <summary>
        /// <para>(Required if this widget annotation is one of multiple children in a field; optional otherwise)</para>
        /// <para>An indirect reference to the widget annotation’s parent field. A widget annotation may have at 
        /// most one parent; that is, it can be included in the Kids array of at most one field</para>
        /// </summary>
        public Dictionary? Parent => Get<Dictionary>(Constants.DictionaryKeys.WidgetAnnotation.Parent);

        public static WidgetAnnotationDictionary FromDictionary(Dictionary dict) => new(dict);
    }
}
