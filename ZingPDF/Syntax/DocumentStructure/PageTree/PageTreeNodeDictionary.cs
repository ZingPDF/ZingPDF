using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.DocumentStructure.PageTree
{
    /// <summary>
    /// ISO 32000-2:2020 7.7.3.2 - Page tree nodes
    /// </summary>
    public class PageTreeNodeDictionary : PageNode
    {
        private PageTreeNodeDictionary(Dictionary pageTreeNodeDictionary) : base(pageTreeNodeDictionary) { }

        /// <summary>
        /// (Required) An array of indirect references to the immediate children of this node. The children shall only be page objects or other page tree nodes.
        /// </summary>
        public ArrayObject Kids => GetAs<ArrayObject>(Constants.DictionaryKeys.PageTree.PageTreeNode.Kids)!;

        /// <summary>
        /// <para>(Required) The number of leaf nodes (page objects) that are descendants of this node within the page tree.</para>
        /// <para>
        /// NOTE Since the number of pages descendent from a Pages dictionary can be accurately determined by examining the tree itself using 
        /// the Kids arrays, the Count entry is redundant.
        /// </para>
        /// <para>
        /// A PDF writer shall ensure that the value of the Count key is consistent with the number of entries in the Kids array and its 
        /// descendants which definitively determines the number of descendant pages.
        /// </para>
        /// <para>
        /// NOTE The PDF spec defines this property as `Count`. In this library, all PDF dictionaries implement <see cref="Dictionary{TKey, TValue}"/>
        /// which already defines a `Count` property, therefore it is called `PageCount` here.
        /// </para>
        /// </summary>
        public Integer PageCount => GetAs<Integer>(Constants.DictionaryKeys.PageTree.PageTreeNode.Count)!;

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
