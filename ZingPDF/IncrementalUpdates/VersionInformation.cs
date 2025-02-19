using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.IncrementalUpdates;

public record VersionInformation
{
    public Trailer? Trailer { get; init; }
    public CrossReferenceTable? CrossReferenceTable { get; init; }
    public StreamObject<CrossReferenceStreamDictionary>? CrossReferenceStream { get; init; }

    public required IIndirectObjectDictionary IndirectObjects { get; init; }

    public ITrailerDictionary TrailerDictionary => Trailer?.Dictionary
            ?? (ITrailerDictionary)CrossReferenceStream!.Dictionary;
}
