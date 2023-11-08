using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects
{
    internal class PagesCatalog : Dictionary
    {
        private static class DictionaryKeys
        {
            public const string Type = "Type";
            public const string Kids = "Kids";
            public const string Count = "Count";
        }

        private PagesCatalog(Dictionary pagesCatalog) : base(pagesCatalog) { }

        public ArrayObject Pages
        {
            get => Get<ArrayObject>(DictionaryKeys.Kids)!;
            set
            {
                this[DictionaryKeys.Kids] = value;
                
                Set<Integer>(DictionaryKeys.Count, value.Count());
            }
        }

        public Integer PageCount { get => Get<Integer>(DictionaryKeys.Count)!; }

        public static PagesCatalog CreateNew(ArrayObject pageReferences)
        {
            return new(new Dictionary<Name, PdfObject>
            {
                { DictionaryKeys.Type, new Name("Pages") },
                { DictionaryKeys.Kids, pageReferences },
                { DictionaryKeys.Count, new Integer(pageReferences.Count()) },
            });
        }

        public static PagesCatalog FromDictionary(Dictionary pagesCatalog)
            => new(pagesCatalog);
    }
}
