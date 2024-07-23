namespace ZingPDF.Syntax
{
    public interface IPdfObject
    {
        long? ByteOffset { get; }
        bool Written { get; }

        Task WriteAsync(Stream stream);
    }
}