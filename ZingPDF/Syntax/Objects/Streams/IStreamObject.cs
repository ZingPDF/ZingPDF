namespace ZingPDF.Syntax.Objects.Streams;

public interface IStreamObject : IPdfObject
{
    IStreamDictionary Dictionary { get; }

    Task<Stream> GetDecompressedDataAsync();
}
