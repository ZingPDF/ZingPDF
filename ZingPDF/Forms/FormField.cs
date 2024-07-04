namespace ZingPDF.Forms
{
    public class FormField
    {
        public FormField(string name, string? description, string? value)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));

            Name = name;
            Description = description;
            Value = value;
        }

        public string Name { get; }
        public string? Description { get; }
        public string? Value { get; }
    }
}
