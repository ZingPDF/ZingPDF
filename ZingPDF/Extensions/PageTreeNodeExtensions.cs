using ZingPDF.ObjectModel.DocumentStructure.PageTree;
using ZingPDF.ObjectModel.Objects.IndirectObjects;

namespace ZingPDF.Extensions
{
    internal static class PageTreeNodeExtensions
    {
        /// <summary>
        /// Recursively get all descendant subpages from the supplied <see cref="PageTreeNode"/>.
        /// </summary>
        public static async Task<List<IndirectObject>> GetSubPagesAsync(
            this PageTreeNode pageTreeNode,
            IIndirectObjectDictionary indirectObjectDictionary
            )
        {
            // TODO: check page ordering, should mimic whatever Acrobat Reader infers

            // TODO: we're parsing all pages and nodes in full. Is there a more performant way to index all pages?

            List<IndirectObject> pages = [];

            foreach (var refObj in pageTreeNode.Kids)
            {
                var ior = (IndirectObjectReference)refObj;

                var obj = await indirectObjectDictionary.GetAsync(ior)
                    ?? throw new InvalidPdfException("Unable to find referenced page");

                if (obj.Children.First() is Page)
                {
                    pages.Add(obj);
                }
                else if (obj.Children.First() is PageTreeNode ptn)
                {
                    pages.AddRange(await ptn.GetSubPagesAsync(indirectObjectDictionary));
                }
            }

            return pages;
        }
    }
}
