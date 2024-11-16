using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects.IndirectObjects
{
    /// <summary>
    /// <para>ISO 32000-2:2020 7.3.10 - Indirect objects</para>
    /// 
    /// Wraps any object with an identifier so that it may be referenced by other objects.
    /// </summary>
    /// TODO: see if this works as a generic class
    public class IndirectObject : PdfObject, IEquatable<IndirectObject?>
    {
        public IndirectObject(IndirectObjectId id, IPdfObject obj)
        {
            ArgumentNullException.ThrowIfNull(id, nameof(id));
            ArgumentNullException.ThrowIfNull(obj, nameof(obj));

            Id = id;
            Object = obj;
        }

        public IndirectObjectId Id { get; }
        public IPdfObject Object { get; protected set; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            // e.g.
            // 8 0 obj
            // 77
            // endobj

            // Object number
            await stream.WriteIntAsync(Id.Index);
            await stream.WriteWhitespaceAsync();

            // Generation number
            await stream.WriteIntAsync(Id.GenerationNumber);
            await stream.WriteWhitespaceAsync();

            await stream.WriteTextAsync(Constants.ObjStart);
            await stream.WriteNewLineAsync();

            await Object.WriteAsync(stream);

            await stream.WriteNewLineAsync();
            await stream.WriteTextAsync(Constants.ObjEnd);
            await stream.WriteNewLineAsync();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as IndirectObject);
        }

        public bool Equals(IndirectObject? other)
        {
            return other is not null &&
                   EqualityComparer<IndirectObjectId>.Default.Equals(Id, other.Id);
        }

        public static bool operator ==(IndirectObject? left, IndirectObject? right)
        {
            return EqualityComparer<IndirectObject>.Default.Equals(left, right);
        }

        public static bool operator !=(IndirectObject? left, IndirectObject? right)
        {
            return !(left == right);
        }
    }
}
