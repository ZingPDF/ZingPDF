using ZingPDF.Objects;

namespace ZingPDF.Objects.Primitives.Streams
{
    internal interface IStreamObject<TDictionary> : IPdfObject where TDictionary : class, IStreamDictionary
    {
        TDictionary Dictionary { get; }

        Task<Stream> GetDecompressedDataAsync();
    }
}
