using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.DocumentStructure.PageTree
{
    /// <summary>
    /// ISO 32000-2:2020 7.7.3.2 - Page tree nodes
    /// </summary>
    public class PageTreeNodeDictionary : PageNode
    {
        public PageTreeNodeDictionary(Dictionary pageTreeNodeDictionary)
            : base(pageTreeNodeDictionary) { }

        private PageTreeNodeDictionary(Dictionary<string, IPdfObject> pageTreeNodeDictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin)
            : base(pageTreeNodeDictionary, pdfContext, objectOrigin) { }

        /// <summary>
        /// (Required) An array of indirect references to the immediate children of this node. The children shall only be page objects or other page tree nodes.
        /// </summary>
        public RequiredProperty<ArrayObject> Kids => GetRequiredProperty<ArrayObject>(Constants.DictionaryKeys.PageTree.PageTreeNode.Kids);

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
        public RequiredProperty<Number> PageCount => GetRequiredProperty<Number>(Constants.DictionaryKeys.PageTree.PageTreeNode.Count);

        public async Task AddChildAsync(IndirectObjectReference key)
        {
            ArgumentNullException.ThrowIfNull(key, nameof(key));

            ArrayObject kids = await Kids.GetAsync();

            kids.Add(key);

            Number count = await PageCount.GetAsync();

            Set(Constants.DictionaryKeys.PageTree.PageTreeNode.Count, count + 1);
        }

        public async Task RemoveChildAsync(IndirectObjectReference key)
        {
            ArgumentNullException.ThrowIfNull(key, nameof(key));

            ArrayObject kids = await Kids.GetAsync();

            kids.Remove<IndirectObjectReference>(x => x.Id.Index == key.Id.Index);

            Number count = await PageCount.GetAsync();

            Set(Constants.DictionaryKeys.PageTree.PageTreeNode.Count, count - 1);
        }

        public async Task ReplaceAllChildrenAsync(IEnumerable<IndirectObjectReference> newKids)
        {
            ArgumentNullException.ThrowIfNull(newKids, nameof(newKids));

            ArrayObject kids = await Kids.GetAsync();

            kids.Clear();

            kids.AddRange(newKids);
        }

        public async Task IncrementCountAsync()
        {
            Number count = await PageCount.GetAsync();

            Set(Constants.DictionaryKeys.PageTree.PageTreeNode.Count, count + 1);
        }

        public async Task DecrementCountAsync()
        {
            Number count = await PageCount.GetAsync();

            Set(Constants.DictionaryKeys.PageTree.PageTreeNode.Count, count - 1);
        }

        public static PageTreeNodeDictionary CreateNew(ArrayObject pageReferences, IPdfContext pdfContext)
        {
            return new(new Dictionary<string, IPdfObject>
            {
                { Constants.DictionaryKeys.Type, (Name)Constants.DictionaryTypes.Pages },
                { Constants.DictionaryKeys.PageTree.PageTreeNode.Kids, pageReferences },
                { Constants.DictionaryKeys.PageTree.PageTreeNode.Count, (Number) pageReferences.Count() },
            }, pdfContext, ObjectOrigin.UserCreated);
        }

        public static PageTreeNodeDictionary FromDictionary(Dictionary<string, IPdfObject> pagesCatalog, IPdfContext pdfContext, ObjectOrigin objectOrigin)
            => new(pagesCatalog, pdfContext, objectOrigin);
    }
}
