using ZingPDF.Extensions;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms
{
    public abstract class FormField<TValue> : IFormField
    {
        protected readonly IndirectObject _fieldIndirectObject;
        protected readonly FieldDictionary _fieldDictionary;
        protected readonly Form _parent;
        protected readonly IIndirectObjectDictionary _indirectObjectDictionary;
        protected readonly Name _fontResourceName;

        protected FormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            TValue? value,
            FieldProperties properties,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary,
            Name fontResourceName
            )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

            _fieldIndirectObject = fieldIndirectObject ?? throw new ArgumentNullException(nameof(fieldIndirectObject));
            _fieldDictionary = fieldIndirectObject.Get<FieldDictionary>();

            Name = name;
            Description = description;
            Value = value;
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            Bounds = Rectangle.FromSize(_fieldDictionary.Rect.Width, _fieldDictionary.Rect.Height);

            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _indirectObjectDictionary = indirectObjectDictionary ?? throw new ArgumentNullException(nameof(indirectObjectDictionary));
            _fontResourceName = fontResourceName ?? throw new ArgumentNullException(nameof(fontResourceName));
        }

        public string Name { get; }
        public string? Description { get; }
        public FieldProperties Properties { get; }
        public TValue? Value { get; protected set; }
        public Rectangle Bounds { get; private set; }

        private IndirectObjectManager IndirectObjects => (IndirectObjectManager)_indirectObjectDictionary;

        public virtual void SetValue(TValue? value)
        {
            _indirectObjectDictionary.EnsureEditable();

            AddAppearanceStream();

            IndirectObjects.Update(_fieldIndirectObject);

            Value = value;
        }

        public Type ValueType => typeof(TValue);
        public object? GetValue() => Value;
        void IFormField.SetValue(object? value) => SetValue((TValue?)value);

        private void AddAppearanceStream()
        {
            var fieldDict = _fieldIndirectObject.Get<FieldDictionary>();

            // TODO: do we need to account for fields which already have an appearance stream? or always replace?
            var fieldSizeRect = Rectangle.FromSize(fieldDict.Rect.Width, fieldDict.Rect.Height);

            var visualContent = BuildVisualContent();

            var apFormXObject = new FormXObject(
                fieldSizeRect,
                [visualContent],
                null,
                filters: null,
                sourceDataIsCompressed: false
                );

            var apIndirectObject = IndirectObjects.Add(apFormXObject);

            fieldDict.SetAppearanceStream(AppearanceDictionary.Create(apIndirectObject.Id.Reference));
        }

        protected internal abstract ContentStreamObject BuildVisualContent();
    }
}
