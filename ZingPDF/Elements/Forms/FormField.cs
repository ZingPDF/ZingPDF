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
            string? description,
            TValue? value,
            FieldProperties properties,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary
            )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

            _fieldIndirectObject = fieldIndirectObject ?? throw new ArgumentNullException(nameof(fieldIndirectObject));
            _fieldDictionary = fieldIndirectObject.Get<FieldDictionary>();

            Name = name;
            Description = description;
            Value = value;
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));

            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _indirectObjectDictionary = indirectObjectDictionary ?? throw new ArgumentNullException(nameof(indirectObjectDictionary));
        }

        public string Name { get; }
        public string? Description { get; }
        public FieldProperties Properties { get; }
        public TValue? Value { get; protected set; }

        protected IndirectObjectManager IndirectObjects => (IndirectObjectManager)_indirectObjectDictionary;

        public virtual void SetValue(TValue? value)
        {
            _indirectObjectDictionary.EnsureEditable();

            Value = value;

            if (Value is not null)
            {
                _fieldDictionary.SetValue(Value);
            }

            OnChange();

            IndirectObjects.Update(_fieldIndirectObject);
        }

        public Type ValueType => typeof(TValue);
        public object? GetValue() => Value;
        void IFormField.SetValue(object? value) => SetValue((TValue?)value);

        /// <summary>
        /// When overriden in a subclass, this method may perform actions necessary for the field type when the value changes.
        /// For example, a text field will update its appearance stream with the new value.
        /// </summary>
        protected virtual void OnChange() { }
    }
}
