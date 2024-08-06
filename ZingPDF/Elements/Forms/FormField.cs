namespace ZingPDF.Elements.Forms
{
    public class FormField
    {
        public FormField(string name, FormFieldType type, string? description, string? value, FieldProperties properties)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));

            Name = name;
            Type = type;
            Description = description;
            Value = value;
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public string Name { get; }
        public FormFieldType Type { get; }
        public string? Description { get; }
        public string? Value { get; }
        public FieldProperties Properties { get; }
    }
}
