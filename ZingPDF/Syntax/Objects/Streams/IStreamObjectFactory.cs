namespace ZingPDF.Syntax.Objects.Streams;

internal interface IStreamObjectFactory
{
    Task<StreamObject<TDictionary>> CreateAsync<TDictionary>(TDictionary dictionary, ObjectOrigin objectOrigin)
        where TDictionary : class, IStreamDictionary;
}

