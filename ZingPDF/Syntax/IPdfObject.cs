namespace ZingPDF.Syntax
{
    public interface IPdfObject : ICloneable
    {
        ObjectOrigin Origin { get; }

        long? ByteOffset { get; }
        bool Written { get; }

        Task WriteAsync(Stream stream);
    }
}