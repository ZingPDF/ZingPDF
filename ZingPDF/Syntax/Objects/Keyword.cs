using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects
{
    /// <summary>
    /// Represents special PDF keywords, such as 'trailer', or 'startxref'.
    /// </summary>
    public class Keyword(string value, ObjectOrigin objectOrigin)
        : PdfObject(objectOrigin)
    {
        public string Value { get; } = value;

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteTextAsync(Value);
        }

        public override string ToString() => $"{nameof(Keyword)}: {Value}";

        public override object Clone() => new Keyword(Value, Origin);

        public static implicit operator string(Keyword value) => value.Value;
        public static implicit operator Keyword(string value) => new(value, ObjectOrigin.ImplicitOperatorConversion);
    }
}
