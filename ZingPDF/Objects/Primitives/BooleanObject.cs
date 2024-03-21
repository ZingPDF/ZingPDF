using ZingPDF.Extensions;

namespace ZingPDF.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.2 - Boolean objects
    /// </summary>
    internal class BooleanObject : PdfObject
    {
        public BooleanObject(bool value)
        {
            Value = value;
        }

        public bool Value { get; }

        protected override async Task WriteOutputAsync(Stream stream) => await stream.WriteTextAsync(Value.ToString().ToLower());

        public override string ToString() => $"Boolean: {Value.ToString().ToLower()}";

        public static implicit operator bool(BooleanObject value) => value.Value;
        public static implicit operator BooleanObject(bool value) => new(value);
    }
}
