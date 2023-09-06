using System.Collections;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects
{
    internal class IndirectObjectCollection : IEnumerable<KeyValuePair<IndirectObjectReference, IndirectObject>>
    {
        private readonly Dictionary<IndirectObjectReference, IndirectObject?> _items = new();

        public IndirectObjectCollection()
        {
            // First item in the list is the head of the linked list of free entries.
            _items.Add(new IndirectObjectReference(0, 65535), null);
        }

        public int Count => _items.Count;

        public IndirectObjectReference ReserveId()
        {
            // TODO: generation number
            var id = new IndirectObjectReference(_items.Count, 0);
            _items.Add(id, null);

            return id;
        }

        public IndirectObject SetChild(IndirectObjectReference id, params PdfObject[] children)
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

        public IEnumerator<KeyValuePair<IndirectObjectReference, IndirectObject>> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }
}
