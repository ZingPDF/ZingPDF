using ZingPDF.Extensions;

namespace ZingPDF.ObjectModel.Objects
{
    internal class Comment : PdfObject
    {
        public Comment(string value)
        {
            Value = value;
        }

        public string Value { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteTextAsync($"{Constants.Percent}{Value}");
        }

        public static implicit operator Comment(string value) => new(value);
        public static implicit operator string(Comment value) => value.Value;
    }
}
