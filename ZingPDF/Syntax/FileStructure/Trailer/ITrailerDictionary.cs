using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.FileStructure.Trailer
{
    public interface ITrailerDictionary : IPdfObject, IReadOnlyDictionary<Name, IPdfObject>
    {
        Dictionary? Encrypt { get; }
        ArrayObject? ID { get; }
        IndirectObjectReference? Info { get; }
        Integer? Prev { get; }
        IndirectObjectReference Root { get; }
        Integer Size { get; }
    }
}