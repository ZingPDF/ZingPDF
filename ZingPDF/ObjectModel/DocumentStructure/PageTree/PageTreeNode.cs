using ZingPDF.ObjectModel.CommonDataStructures;
using ZingPDF.ObjectModel.Objects;
using ZingPDF.ObjectModel.Objects.IndirectObjects;

namespace ZingPDF.ObjectModel.DocumentStructure.PageTree
{
    /// <summary>
    /// ISO 32000-2:2020 7.7.3.2 - Page tree nodes
    /// </summary>
    internal class PageTreeNode : Dictionary
    {
        internal static class DictionaryKeys
        {
            public const string Pages = "Pages";
            public const string Parent = "Parent";
            public const string Kids = "Kids";
            public const string Count = "Count";
        }

        private PageTreeNode(Dictionary pageTreeNodeDictionary) : base(pageTreeNodeDictionary) { }

        public IndirectObjectReference? Parent => Get<IndirectObjectReference>(DictionaryKeys.Parent);

        public ArrayObject Kids => Get<ArrayObject>(DictionaryKeys.Kids)!;

        #region Inheritable properties

        /// <summary>
        /// The boundaries of the physical medium on which the page shall be displayed or printed.
        /// </summary>
        public Rectangle? MediaBox => Get<Rectangle>(Page.DictionaryKeys.MediaBox);

        #endregion

        public Integer PageCount => Get<Integer>(DictionaryKeys.Count)!;

        public void AddChild(IndirectObjectReference key)
        {
            ArgumentNullException.ThrowIfNull(key);

            Kids.Add(key);

            Set(DictionaryKeys.Count, new Integer(PageCount + 1));
        }

        public void RemoveChild(IndirectObjectReference key)
        {
            ArgumentNullException.ThrowIfNull(key);

            Kids.Remove<IndirectObjectReference>(x => x.Id.Reference == key);   

            Set(DictionaryKeys.Count, new Integer(PageCount - 1));
        }

        public void IncrementCount()
        {
            Set(DictionaryKeys.Count, new Integer(PageCount + 1));
        }

        public void DecrementCount()
        {
            Set(DictionaryKeys.Count, new Integer(PageCount - 1));
        }

        public static PageTreeNode CreateNew(ArrayObject pageReferences)
        {
            return new(new Dictionary<Name, IPdfObject>
            {
                { Constants.DictionaryKeys.Type, new Name(DictionaryKeys.Pages) },
                { DictionaryKeys.Kids, pageReferences },
                { DictionaryKeys.Count, new Integer(pageReferences.Count()) },
            });
        }

        public static PageTreeNode FromDictionary(Dictionary pagesCatalog)
            => new(pagesCatalog);
    }
}
