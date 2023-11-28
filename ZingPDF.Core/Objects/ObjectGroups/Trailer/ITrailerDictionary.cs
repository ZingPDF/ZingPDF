using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.Objects.ObjectGroups.Trailer
{
    internal interface ITrailerDictionary : IPdfObject
    {
        Dictionary? Encrypt { get; }
        ArrayObject? ID { get; }
        IndirectObjectReference? Info { get; }
        Integer? Prev { get; }
        IndirectObjectReference Root { get; }
        Integer Size { get; }
    }
}