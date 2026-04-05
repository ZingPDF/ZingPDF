namespace ZingPDF.Syntax.Objects.Streams;

public interface IStreamObject : IPdfObject
{
    IStreamDictionary Dictionary { get; }
    Stream Data { get; }

    Task<Stream> GetDecompressedDataAsync();
}
