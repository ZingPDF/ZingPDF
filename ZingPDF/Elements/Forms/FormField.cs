using ZingPDF.Elements.Drawing;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms
{
    /// <summary>
    /// Base class for strongly typed form fields.
    /// </summary>
    /// <typeparam name="TValue">The PDF object type used to store the field value.</typeparam>
    public abstract class FormField<TValue> : IFormField where TValue : IPdfObject
    {
        protected readonly IndirectObject _fieldIndirectObject;
        protected readonly FieldDictionary _fieldDictionary;
        protected readonly Form _parent;
        protected readonly IPdf _pdf;

        protected FormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            FieldProperties properties,
            Form parent,
            IPdf pdf
            )
        {
            ArgumentNullException.ThrowIfNull(fieldIndirectObject, nameof(fieldIndirectObject));
            ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
            ArgumentNullException.ThrowIfNull(parent, nameof(parent));
            ArgumentNullException.ThrowIfNull(pdf, nameof(pdf));

            _fieldIndirectObject = fieldIndirectObject;
            _fieldDictionary = (FieldDictionary)fieldIndirectObject.Object;

            Name = name;
            Description = description;
            Properties = properties;

            _parent = parent;
            _pdf = pdf;
        }

        /// <summary>
        /// Gets the fully qualified field name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the user-facing description or tooltip for the field, when present.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the decoded field flags.
        /// </summary>
        public FieldProperties Properties { get; }

        /// <inheritdoc />
        public async Task<Rectangle> GetFieldBoundsAsync() => await _fieldDictionary.Rect.GetAsync();

        /// <inheritdoc />
        public async Task<Size> GetFieldDimensionsAsync() => (await _fieldDictionary.Rect.GetAsync()).Size;

        protected void SetValue(TValue? value)
        {
            if (value is not null)
            {
                _fieldDictionary.SetValue(value);
            }

            _pdf.Objects.Update(_fieldIndirectObject);

            _parent.MarkForUpdate();
        }
    }
}
