using System.Runtime.CompilerServices;

namespace ZingPDF.Syntax.Objects
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.9 - Null object
    /// </summary>
    public class Null : PdfObject
    {
        public Null(ObjectOrigin objectOrigin)
            : base(objectOrigin)
        {
        }

        protected override Task WriteOutputAsync(Stream stream)
            => new Keyword(Constants.Null, Origin).WriteAsync(stream);

        // Override Equals to make *every* Null unequal, even to another Null
        public override bool Equals(object? obj) => false;

        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

        public override object Clone() => new Null(Origin);
    }
}
