namespace ZingPDF.Elements.Forms
{
    public abstract class FormField<TValue> : IFormField
    {
        protected FormField(string name, string? description, TValue? value, FieldProperties properties)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

            Name = name;
            Description = description;
            Value = value;
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public string Name { get; }
        public string? Description { get; }
        public FieldProperties Properties { get; }
        public TValue? Value { get; protected set; }

        public virtual void SetValue(TValue? value) => Value = value;

        public Type ValueType => typeof(TValue);
        public object? GetValue() => Value;
        void IFormField.SetValue(object? value) => SetValue((TValue?)value);
    }

    public class TextFormField : FormField<string>
    {
        public TextFormField(string name, string? description, string? value, FieldProperties properties)
            : base(name, description, value, properties) { }
    }

    public class CheckboxFormField : FormField<bool>
    {
        public CheckboxFormField(string name, string? description, bool value, FieldProperties properties)
            : base(name, description, value, properties) { }
    }
}
