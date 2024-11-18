using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Extensions
{
    internal static class PageTreeNodeExtensions
    {
        /// <summary>
        /// Recursively get all descendant nodes from the supplied <see cref="PageTreeNodeDictionary"/>.
        /// </summary>
        public static async Task<IList<IndirectObject>> GetSubNodesAsync(
            this PageTreeNodeDictionary pageTreeNode,
            IIndirectObjectDictionary indirectObjectDictionary
            )
        {
            // TODO: check page ordering, should mimic whatever Acrobat Reader infers

            List<IndirectObject> nodes = [];

            foreach (var refObj in pageTreeNode.Kids)
            {
                var ior = (IndirectObjectReference)refObj;

                var obj = await indirectObjectDictionary.GetAsync(ior)
                    ?? throw new InvalidPdfException("Unable to find referenced page");

                nodes.Add(obj);

                if (obj.Object is PageTreeNodeDictionary ptn)
                {
                    nodes.AddRange(await ptn.GetSubNodesAsync(indirectObjectDictionary));
                }
            }

            return nodes;
        }
    }
}
