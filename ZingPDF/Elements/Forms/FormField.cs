using ZingPDF.Elements.Drawing;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms
{
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

        public string Name { get; }
        public string? Description { get; }
        public FieldProperties Properties { get; }

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
