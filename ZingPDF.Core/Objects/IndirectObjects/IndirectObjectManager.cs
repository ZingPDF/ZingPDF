using System.Collections;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects.IndirectObjects
{
    internal class IndirectObjectManager : IEnumerable<KeyValuePair<IndirectObjectId, IndirectObject>>
    {
        private readonly Dictionary<IndirectObjectId, IndirectObject?> _items = new();

        public IndirectObjectManager()
        {
            // First item in the list is the head of the linked list of free entries.
            _items.Add(new IndirectObjectId(0, 65535), null);
        }

        public int Count => _items.Count;

        public IEnumerable<IndirectObject> Values => _items.Values.Skip(1).Cast<IndirectObject>();

        public IndirectObjectId ReserveId()
        {
            // TODO: generation number
            var id = new IndirectObjectId(_items.Count, 0);
            _items.Add(id, null);

            return id;
        }

        public IndirectObject SetChild(IndirectObjectId id, params PdfObject[] children)
        {
            if (id is null) throw new ArgumentNullException(nameof(id));
            if (children is null) throw new ArgumentNullException(nameof(children));

            var indirectObject = new IndirectObject(id, children);

            _items[id] = indirectObject;

            return indirectObject;
        }

        public IndirectObject Add(params PdfObject[] children)
        {
            if (children is null) throw new ArgumentNullException(nameof(children));

            var indirectObject = new IndirectObject(ReserveId(), children);

            _items[indirectObject.Id] = indirectObject;

            return indirectObject;
        }

        public IEnumerator<KeyValuePair<IndirectObjectId, IndirectObject>> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }
}
