using System.Collections;
using System.Diagnostics.CodeAnalysis;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.Objects
{
    internal class IndirectObjectManager : IDictionary<IndirectObjectId, IndirectObject>
    {
        private readonly Dictionary<IndirectObjectId, IndirectObject?> _items = new();

        /// <summary>
        /// Reserve an object ID to use later for an <see cref="IndirectObject"/>.
        /// </summary>
        /// <returns></returns>
        public IndirectObjectId ReserveId()
        {
            var id = new IndirectObjectId(_items.Count + 1, 0);
            _items.Add(id, null);

            return id;
        }

        /// <summary>
        /// Creates a new <see cref="IndirectObject"/> with the specified <see cref="PdfObject"/>s as children.
        /// </summary>
        /// <param name="children">The child <see cref="PdfObject"/> items</param>
        /// <returns>The new <see cref="IndirectObject"/></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public IndirectObject Create(params PdfObject[] children)
        {
            if (children is null) throw new ArgumentNullException(nameof(children));

            return Create(ReserveId(), children);
        }

        /// <summary>
        /// Creates a new <see cref="IndirectObject"/> with the specified <see cref="PdfObject"/>s as children.
        /// </summary>
        /// <param name="id">An <see cref="IndirectObjectId"/> to use for the object. Note: This ID must have been reserved using the <see cref="ReserveId"/> method.</param>
        /// <param name="children">The child <see cref="PdfObject"/> items</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public IndirectObject Create(IndirectObjectId id, params PdfObject[] children)
        {
            if (id is null) throw new ArgumentNullException(nameof(id));
            if (children is null) throw new ArgumentNullException(nameof(children));

            var indirectObject = new IndirectObject(id, children);

            _items[id] = indirectObject;

            return indirectObject;
        }

        /// <summary>
        /// When you know the Indirect Object contains a single object of a specific type, 
        /// this method provides strongly typed access to it.
        /// </summary>
        public T GetSingle<T>(IndirectObjectId id) where T : PdfObject
            => (T)this[id].Children.First();

        #region IDictionary<IndirectObjectId, IndirectObject>
        public ICollection<IndirectObjectId> Keys => ((IDictionary<IndirectObjectId, IndirectObject>)_items).Keys;
        public ICollection<IndirectObject> Values => ((IDictionary<IndirectObjectId, IndirectObject>)_items).Values;
        public int Count => ((ICollection<KeyValuePair<IndirectObjectId, IndirectObject>>)_items).Count;
        public bool IsReadOnly => ((ICollection<KeyValuePair<IndirectObjectId, IndirectObject>>)_items).IsReadOnly;
        public IndirectObject this[IndirectObjectId key] { get => ((IDictionary<IndirectObjectId, IndirectObject>)_items)[key]; set => ((IDictionary<IndirectObjectId, IndirectObject>)_items)[key] = value; }
        public void Add(IndirectObjectId key, IndirectObject value) => ((IDictionary<IndirectObjectId, IndirectObject>)_items).Add(key, value);
        public bool ContainsKey(IndirectObjectId key) => ((IDictionary<IndirectObjectId, IndirectObject>)_items).ContainsKey(key);
        public bool Remove(IndirectObjectId key) => ((IDictionary<IndirectObjectId, IndirectObject>)_items).Remove(key);
        public bool TryGetValue(IndirectObjectId key, [MaybeNullWhen(false)] out IndirectObject value) => ((IDictionary<IndirectObjectId, IndirectObject>)_items).TryGetValue(key, out value);
        public void Add(KeyValuePair<IndirectObjectId, IndirectObject> item) => ((ICollection<KeyValuePair<IndirectObjectId, IndirectObject>>)_items).Add(item);
        public void Clear() => ((ICollection<KeyValuePair<IndirectObjectId, IndirectObject>>)_items).Clear();
        public bool Contains(KeyValuePair<IndirectObjectId, IndirectObject> item) => ((ICollection<KeyValuePair<IndirectObjectId, IndirectObject>>)_items).Contains(item);
        public void CopyTo(KeyValuePair<IndirectObjectId, IndirectObject>[] array, int arrayIndex) => ((ICollection<KeyValuePair<IndirectObjectId, IndirectObject>>)_items).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<IndirectObjectId, IndirectObject> item) => ((ICollection<KeyValuePair<IndirectObjectId, IndirectObject>>)_items).Remove(item);
        public IEnumerator<KeyValuePair<IndirectObjectId, IndirectObject>> GetEnumerator() => ((IEnumerable<KeyValuePair<IndirectObjectId, IndirectObject>>)_items).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_items).GetEnumerator();
        #endregion
    }
}
