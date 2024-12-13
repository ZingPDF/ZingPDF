using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.IncrementalUpdates;

internal record DocumentVersion
{
    public Trailer? Trailer { get; init; }
    public CrossReferenceTable? CrossReferenceTable { get; init; }
    public StreamObject<IStreamDictionary>? CrossReferenceStream { get; init; }

    public ITrailerDictionary TrailerDictionary => Trailer?.Dictionary
            ?? (ITrailerDictionary)CrossReferenceStream!.Dictionary;
}
