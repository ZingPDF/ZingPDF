using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects
{
    public class Comment(string value, ObjectOrigin objectOrigin)
        : PdfObject(objectOrigin)
    {
        public string Value { get; } = value;

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteTextAsync($"{Constants.Characters.Percent}{Value}");
        }

        public static implicit operator Comment(string value) => new(value, ObjectOrigin.ImplicitOperatorConversion);
        public static implicit operator string(Comment value) => value.Value;

        public override object Clone() => new Comment(Value, Origin);
    }
}
