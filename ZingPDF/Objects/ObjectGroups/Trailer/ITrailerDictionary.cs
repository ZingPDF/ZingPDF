using ZingPDF.Objects;
using ZingPDF.Objects.Primitives;
using ZingPDF.Objects.Primitives.IndirectObjects;

namespace ZingPDF.Objects.ObjectGroups.Trailer
{
    internal interface ITrailerDictionary : IPdfObject, IDictionary<Name, IPdfObject>
    {
        Dictionary? Encrypt { get; }
        ArrayObject? ID { get; }
        IndirectObjectReference? Info { get; }
        Integer? Prev { get; }
        IndirectObjectReference Root { get; }
        Integer Size { get; }
    }
}