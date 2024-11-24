using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.DocumentStructure.PageTree
{
    /// <summary>
    /// ISO 32000-2:2020 7.7.3.2 - Page tree nodes
    /// </summary>
    public class PageTreeNodeDictionary : PageNode
    {
        private PageTreeNodeDictionary(Dictionary pageTreeNodeDictionary) : base(pageTreeNodeDictionary) { }

        public ArrayObject Kids => Get<ArrayObject>(Constants.DictionaryKeys.PageTree.PageTreeNode.Kids)!;

        public Integer PageCount => Get<Integer>(Constants.DictionaryKeys.PageTree.PageTreeNode.Count)!;

        public void AddChild(IndirectObjectReference key)
        {
            ArgumentNullException.ThrowIfNull(key, nameof(key));

            Kids.Add(key);

            Set(Constants.DictionaryKeys.PageTree.PageTreeNode.Count, new Integer(PageCount + 1));
        }

        public void RemoveChild(IndirectObjectReference key)
        {
            ArgumentNullException.ThrowIfNull(key, nameof(key));

            Kids.Remove<IndirectObjectReference>(x => x.Id.Reference == key);   

            Set(Constants.DictionaryKeys.PageTree.PageTreeNode.Count, new Integer(PageCount - 1));
        }

        public void ReplaceAllChildren(IEnumerable<IndirectObjectReference> kids)
        {
            ArgumentNullException.ThrowIfNull(kids, nameof(kids));

            Kids.Clear();

            Kids.AddRange(kids);
        }

        public void IncrementCount()
        {
            Set(Constants.DictionaryKeys.PageTree.PageTreeNode.Count, new Integer(PageCount + 1));
        }

        public void DecrementCount()
        {
            Set(Constants.DictionaryKeys.PageTree.PageTreeNode.Count, new Integer(PageCount - 1));
        }

        public static PageTreeNodeDictionary CreateNew(ArrayObject pageReferences)
        {
            return new(new Dictionary<Name, IPdfObject>
            {
                { Constants.DictionaryKeys.Type, new Name(Constants.DictionaryTypes.Pages) },
                { Constants.DictionaryKeys.PageTree.PageTreeNode.Kids, pageReferences },
                { Constants.DictionaryKeys.PageTree.PageTreeNode.Count, new Integer(pageReferences.Count()) },
            });
        }

        public static PageTreeNodeDictionary FromDictionary(Dictionary pagesCatalog)
            => new(pagesCatalog);
    }
}
