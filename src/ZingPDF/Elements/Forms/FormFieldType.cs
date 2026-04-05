namespace ZingPDF.Elements.Forms
{
    /// <summary>
    /// High-level AcroForm field classifications.
    /// </summary>
    public enum FormFieldType
    {
        /// <summary>
        /// A button field such as a checkbox, radio button, or push button.
        /// </summary>
        Button,

        /// <summary>
        /// A text-input field.
        /// </summary>
        Text,

        /// <summary>
        /// A choice field such as a combo box or list box.
        /// </summary>
        Choice,

        /// <summary>
        /// A digital signature field.
        /// </summary>
        Signature
    }
}
