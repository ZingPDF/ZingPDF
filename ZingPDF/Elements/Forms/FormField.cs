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
        private TValue? _value;

        protected FormField(
            IndirectObject fieldIndirectObject,
            string name,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary
            )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

            _fieldIndirectObject = fieldIndirectObject ?? throw new ArgumentNullException(nameof(fieldIndirectObject));
            _fieldDictionary = fieldIndirectObject.Get<FieldDictionary>();

            Name = name;
            Description = _fieldDictionary.TU;
            _value = GetValue();
            Properties = new FieldProperties(_fieldDictionary.Ff ?? 0);

            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _indirectObjectDictionary = indirectObjectDictionary ?? throw new ArgumentNullException(nameof(indirectObjectDictionary));
        }

        public string Name { get; }
        public string? Description { get; }
        public FieldProperties Properties { get; }

        public TValue? Value
        {
            get => _value;
            set
            {
                _indirectObjectDictionary.EnsureEditable();

                _value = value;

                if (_value is not null)
                {
                    _fieldDictionary.SetValue(_value);
                }

                OnChange();

                IndirectObjects.Update(_fieldIndirectObject);
            }
        }

        protected IndirectObjectManager IndirectObjects => (IndirectObjectManager)_indirectObjectDictionary;

        /// <summary>
        /// When overriden in a subclass, this method may perform actions necessary for the field type when the value changes.
        /// For example, a text field will update its appearance stream with the new value.
        /// </summary>
        protected virtual void OnChange() { }

        protected abstract TValue? GetValue();
    }
}
