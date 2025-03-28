using ZingPDF.IncrementalUpdates;

namespace ZingPDF.Syntax.Objects.Streams;

internal interface IStreamObjectFactory<TDictionary> where TDictionary : class, IStreamDictionary
{
    StreamObject<TDictionary> Create(IPdfEditor pdfEditor);
}

