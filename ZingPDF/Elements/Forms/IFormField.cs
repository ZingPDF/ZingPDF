using ZingPDF.Elements.Drawing;

namespace ZingPDF.Elements.Forms
{
    /// <summary>
    /// Common metadata exposed for every discovered AcroForm field.
    /// </summary>
    public interface IFormField
    {
        /// <summary>
        /// Gets the user-facing description or tooltip for the field, when present.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Gets the fully qualified field name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the decoded field flags.
        /// </summary>
        FieldProperties Properties { get; }

        /// <summary>
        /// Gets the field rectangle size.
        /// </summary>
        Task<Size> GetFieldDimensionsAsync();
    }
}
