namespace ZingPDF.Syntax
{
    public interface IPdfObject : ICloneable
    {
        ObjectContext Context { get; }

        long? ByteOffset { get; }
        bool Written { get; }

        Task WriteAsync(Stream stream);
    }
}