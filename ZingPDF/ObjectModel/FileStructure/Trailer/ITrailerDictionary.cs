using ZingPDF.ObjectModel.Objects;
using ZingPDF.ObjectModel.Objects.IndirectObjects;

namespace ZingPDF.ObjectModel.FileStructure.Trailer
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