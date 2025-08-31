using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects
{
    public class Comment(string value, ObjectContext context)
        : PdfObject(context)
    {
        public string Value { get; } = value;

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteTextAsync($"{Constants.Characters.Percent}{Value}");
        }

        public static implicit operator Comment(string value) => new(value, ObjectContext.FromImplicitOperator);
        public static implicit operator string(Comment value) => value.Value;

        public override object Clone() => new Comment(Value, Context);
    }
}
