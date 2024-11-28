namespace ZingPDF.Syntax.Objects.Streams
{
    internal interface IStreamObject<TDictionary> : IPdfObject where TDictionary : class, IStreamDictionary
    {
        TDictionary Dictionary { get; }
        StreamData Data { get; }
    }
}
