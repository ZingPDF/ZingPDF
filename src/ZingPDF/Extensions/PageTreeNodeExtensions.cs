using ZingPDF.Diagnostics;
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
            IPdfObjectCollection pdfEditor
            )
        {
            using var trace = PerformanceTrace.Measure("PageTreeNodeExtensions.GetSubNodesAsync");

            // TODO: check page ordering, should mimic whatever Acrobat Reader infers

            List<IndirectObject> nodes = [];
            Syntax.Objects.ArrayObject kids = await pageTreeNode.Kids.GetAsync();

            foreach (var refObj in kids)
            {
                var ior = (IndirectObjectReference)refObj;

                var obj = await pdfEditor.GetAsync(ior)
                    ?? throw new InvalidPdfException("Unable to find referenced page");

                nodes.Add(obj);

                if (obj.Object is PageTreeNodeDictionary ptn)
                {
                    nodes.AddRange(await ptn.GetSubNodesAsync(pdfEditor));
                }
            }

            return nodes;
        }
    }
}
