using ZingPdf.Core.Objects.ObjectGroups.CrossReferences;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.IncrementalUpdates
{
    internal class CrossReferenceGenerator
    {
        public List<CrossReferenceSection> Generate(IEnumerable<IndirectObject> newOrUpdatedObjects, IEnumerable<IndirectObjectId> deletedObjects)
        {
            CrossReferenceSection? latestXrefSection = null;
            List<CrossReferenceSection> xrefSections = new();

            var allEntries =
                newOrUpdatedObjects.Select(x => KeyValuePair.Create(x.Id, (IndirectObject?)x))
                .Concat(deletedObjects.Select(x => KeyValuePair.Create(x, (IndirectObject?)null)))
                .OrderBy(x => x.Key.Index)
                .ToList();

            for (var i = allEntries.First().Key.Index; i <= allEntries.Last().Key.Index; i++)
            {
                var entry = allEntries.FirstOrDefault(e => e.Key.Index == i);
                if (entry.Key is not null)
                {
                    if (latestXrefSection is null)
                    {
                        latestXrefSection = new CrossReferenceSection(i);
                        xrefSections.Add(latestXrefSection);
                    }

                    var inUse = entry.Value is not null;
                    var nextFreeObjectNumber = 0; // TODO

                    latestXrefSection.Add(new CrossReferenceEntry(
                        inUse ? entry.Value!.ByteOffset!.Value : nextFreeObjectNumber,
                        entry.Key.GenerationNumber,
                        inUse,
                        false
                        ));
                }
                else
                {
                    // End the section if next entry is non-contiguous
                    latestXrefSection = null;
                }
            }

            return xrefSections;
        }
    }
}

