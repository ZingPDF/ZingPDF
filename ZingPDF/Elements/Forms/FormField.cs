using ZingPDF.Extensions;
using ZingPDF.IncrementalUpdates;
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
        protected readonly IIndirectObjectDictionary _indirectObjectDictionary;

        protected FormField(
            IndirectObject fieldIndirectObject,
            string name,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary
            )
        {
            ArgumentNullException.ThrowIfNull(fieldIndirectObject, nameof(fieldIndirectObject));
            ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

            _fieldIndirectObject = fieldIndirectObject;
            _fieldDictionary = (FieldDictionary)fieldIndirectObject.Object;

            Name = name;
            Description = _fieldDictionary.TU;
            Properties = new FieldProperties(_fieldDictionary.Ff ?? 0);

            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _indirectObjectDictionary = indirectObjectDictionary ?? throw new ArgumentNullException(nameof(indirectObjectDictionary));
        }

        public string Name { get; }
        public string? Description { get; }
        public FieldProperties Properties { get; }

        protected void SetValue(TValue? value)
        {
            if (value is not null)
            {
                _fieldDictionary.SetValue(value);
            }

            _indirectObjectDictionary.Update(_fieldIndirectObject);

            _parent.MarkForUpdate();
        }
    }
}
