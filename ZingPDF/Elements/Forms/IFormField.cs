namespace ZingPDF.Elements.Forms
{
    /// <summary>
    /// Represents a form field in a document.
    /// </summary>
    /// <remarks>
    /// This interface provides a common contract for all types of form fields.
    /// Implementations should ensure type safety and proper value handling.
    /// Library developers should implement this interface for each specific field type,
    /// typically by inheriting from FormField&lt;TValue&gt;.
    /// 
    /// Users of this interface should be aware that while SetValue accepts object,
    /// it should be called with values compatible with the specific field type.
    /// </remarks>
    /// <summary>
    /// Represents a form field in a document.
    /// </summary>
    public interface IFormField
    {
        /// <summary>
        /// Gets the name of the form field.
        /// </summary>
        /// <remarks>
        /// The name is a unique identifier for the field within the form.
        /// It cannot be null or whitespace.
        /// </remarks>
        string Name { get; }

        /// <summary>
        /// Gets the description of the form field.
        /// </summary>
        /// <remarks>
        /// The description provides additional information about the field's purpose or usage.
        /// It may be null if no description is provided.
        /// </remarks>
        string? Description { get; }

        /// <summary>
        /// Gets the properties of the form field.
        /// </summary>
        /// <remarks>
        /// FieldProperties contain metadata about the field, such as whether it's read-only,
        /// required, or has specific display characteristics.
        /// </remarks>
        FieldProperties Properties { get; }

        /// <summary>
        /// Gets the type of the value that this form field can hold.
        /// </summary>
        /// <remarks>
        /// This property is useful for runtime type checking and for
        /// scenarios where reflection might be needed.
        /// </remarks>
        Type ValueType { get; }

        /// <summary>
        /// Gets the current value of the form field.
        /// </summary>
        /// <returns>
        /// The current value of the field as an object, or null if the field has no value.
        /// </returns>
        /// <remarks>
        /// The returned object should be cast to the appropriate type
        /// as indicated by the ValueType property.
        /// </remarks>
        object? GetValue();

        /// <summary>
        /// Sets the value of the form field.
        /// </summary>
        /// <param name="value">The value to set. Should be of the type indicated by ValueType.</param>
        /// <remarks>
        /// Implementations should perform type checking and throw an appropriate exception
        /// if the provided value is not compatible with the field's value type.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the provided value is not compatible with the field's value type.
        /// </exception>
        void SetValue(object? value);
    }
}