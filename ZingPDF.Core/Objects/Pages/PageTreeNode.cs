using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.Objects.Pages
{
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

        public IndirectObjectReference? Parent
        {
            get => Get<IndirectObjectReference>(DictionaryKeys.Parent);
            set
            {
                this[DictionaryKeys.Parent] = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public ArrayObject Kids
        {
            get => Get<ArrayObject>(DictionaryKeys.Kids)!;
            set
            {
                this[DictionaryKeys.Kids] = new ArrayObject(value.ToArray());
            }
        }

        public Integer PageCount { get => Get<Integer>(DictionaryKeys.Count)!; set => this[DictionaryKeys.Count] = value; }

        public static PageTreeNode CreateNew(ArrayObject pageReferences)
        {
            return new(new Dictionary<Name, PdfObject>
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
