using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.DocumentStructure.PageTree
{
    /// <summary>
    /// ISO 32000-2:2020 7.7.3.2 - Page tree nodes
    /// </summary>
    internal class PageTreeNodeDictionary : Dictionary
    {
        private PageTreeNodeDictionary(Dictionary pageTreeNodeDictionary) : base(pageTreeNodeDictionary) { }

        public IndirectObjectReference? Parent => Get<IndirectObjectReference>(Constants.DictionaryKeys.PageTreeNode.Parent);

        public ArrayObject Kids => Get<ArrayObject>(Constants.DictionaryKeys.PageTreeNode.Kids)!;

        #region Inheritable properties

        /// <summary>
        /// The boundaries of the physical medium on which the page shall be displayed or printed.
        /// </summary>
        public Rectangle? MediaBox => Get<Rectangle>(Constants.DictionaryKeys.Page.MediaBox);

        #endregion

        public Integer PageCount => Get<Integer>(Constants.DictionaryKeys.PageTreeNode.Count)!;

        public void AddChild(IndirectObjectReference key)
        {
            ArgumentNullException.ThrowIfNull(key);

            Kids.Add(key);

            Set(Constants.DictionaryKeys.PageTreeNode.Count, new Integer(PageCount + 1));
        }

        public void RemoveChild(IndirectObjectReference key)
        {
            ArgumentNullException.ThrowIfNull(key);

            Kids.Remove<IndirectObjectReference>(x => x.Id.Reference == key);   

            Set(Constants.DictionaryKeys.PageTreeNode.Count, new Integer(PageCount - 1));
        }

        public void IncrementCount()
        {
            Set(Constants.DictionaryKeys.PageTreeNode.Count, new Integer(PageCount + 1));
        }

        public void DecrementCount()
        {
            Set(Constants.DictionaryKeys.PageTreeNode.Count, new Integer(PageCount - 1));
        }

        public static PageTreeNodeDictionary CreateNew(ArrayObject pageReferences)
        {
            return new(new Dictionary<Name, IPdfObject>
            {
                { Constants.DictionaryKeys.Type, new Name(Constants.DictionaryTypes.Pages) },
                { Constants.DictionaryKeys.PageTreeNode.Kids, pageReferences },
                { Constants.DictionaryKeys.PageTreeNode.Count, new Integer(pageReferences.Count()) },
            });
        }

        public static PageTreeNodeDictionary FromDictionary(Dictionary pagesCatalog)
            => new(pagesCatalog);
    }
}
