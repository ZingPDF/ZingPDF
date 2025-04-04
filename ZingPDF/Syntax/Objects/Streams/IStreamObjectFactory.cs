namespace ZingPDF.Syntax.Objects.Streams;

internal interface IStreamObjectFactory
{
    Task<StreamObject<TDictionary>> CreateAsync<TDictionary>(TDictionary dictionary) where TDictionary : class, IStreamDictionary;
}

