using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.2 - Boolean objects
    /// </summary>
    public class BooleanObject : PdfObject
    {
        private BooleanObject(bool value, ObjectOrigin objectOrigin)
            : base(objectOrigin)
        {
            Value = value;
        }

        public bool Value { get; }

        protected override async Task WriteOutputAsync(Stream stream) => await stream.WriteTextAsync(Value.ToString().ToLower());

        public override string ToString() => $"Boolean: {Value.ToString().ToLower()}";

        public static implicit operator bool(BooleanObject value) => value.Value;
        public static implicit operator BooleanObject(bool value) => new(value, ObjectOrigin.ImplicitOperatorConversion);

        public override object Clone() => new BooleanObject(Value, Origin);

        public static BooleanObject FromBool(bool value, ObjectOrigin objectOrigin)
            => new(value, objectOrigin);
    }
}
