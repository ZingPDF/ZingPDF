using System.Runtime.InteropServices.JavaScript;
using ZingPDF.Extensions;
using ZingPDF.Parsing.Parsers.Objects;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF
{
    internal class PdfMerger
    {
        private readonly Pdf _mainPdf;
        private readonly IPdf _pdfToAppend;

        // New pages to add
        private readonly List<IndirectObject> _newPageNodes = [];

        // Map old to new references
        private readonly Dictionary<IndirectObjectReference, IndirectObjectReference> _oldToNewMap = [];

        public PdfMerger(Pdf mainPdf, IPdf pdfToAppend)
        {
            ArgumentNullException.ThrowIfNull(mainPdf, nameof(mainPdf));
            ArgumentNullException.ThrowIfNull(pdfToAppend, nameof(pdfToAppend));

            _mainPdf = mainPdf;
            _pdfToAppend = pdfToAppend;
        }

        public async Task AppendAsync()
        {
            var rootPageTreeNodeIndirectObject = await _mainPdf.PageTree.GetRootPageTreeNodeAsync();

            var rootPageTreeNode = (PageTreeNodeDictionary)rootPageTreeNodeIndirectObject.Object;

            // Simple document merging...
            // - Loop though all pages and page tree nodes
            //      - We should have already processed its parent, so update the parent to the new reference
            // - Add a new indirect object for each node
            //      - Add it to the dictionary to map the old to new reference
            //      - Update its parent reference
            // - Create a new indirect object for each resource
            //      - Add it to the dictionary to map the old to new reference (This way, for each reference we process, we can check if we've already created a new indirect object)
            //      - Update the reference in the resource dictionary to the new reference
            // - Now that we have new indirect objects for all nodes, perform another loop to update all kids arrays with new references
            // - Add a new page tree node to act as parent for all new pages
            // - Add new parent to root page tree node

            // Get all pages and page tree nodes
            var nodes = await _pdfToAppend.PageTree.GetAllNodesAsync();

            foreach (var node in nodes)
            {
                var pageNodeDictionary = node.Object as PageNode
                    ?? throw new InvalidOperationException("Something went wrong");

                // Update this node's parent to its new reference
                if (pageNodeDictionary.Parent is not null)
                {
                    pageNodeDictionary.SetParent(_oldToNewMap[pageNodeDictionary.Parent]);
                }

                // Add a new indirect object for each node
                var newObj = _mainPdf.IndirectObjectManager.Add(node.Object);

                // Add it to the list of new page nodes
                _newPageNodes.Add(newObj);

                // Also add it to the old-new map so it can be used as a parent in a future iteration
                _oldToNewMap.Add(node.Id.Reference, newObj.Id.Reference);

                // Create a new reference for each resource
                if (pageNodeDictionary.Resources is Dictionary resources)
                {
                    var resourceDictionary = ResourceDictionary.FromDictionary(resources);

                    await CopyResourcesAsync(resourceDictionary.ExtGState);
                    await CopyResourcesAsync(resourceDictionary.ColorSpace);
                    await CopyResourcesAsync(resourceDictionary.Pattern);
                    await CopyResourcesAsync(resourceDictionary.Shading);
                    await CopyResourcesAsync(resourceDictionary.XObject);
                    await CopyResourcesAsync(resourceDictionary.Font);
                    await CopyResourcesAsync(resourceDictionary.Properties);
                }
            }

            // Now that we have new objects for all page nodes, update all Kids arrays with new references
            foreach(var pageNode in _newPageNodes)
            {
                if (pageNode.Object is PageTreeNodeDictionary pageTreeNodeDictionary)
                {
                    var newChildRefs = pageTreeNodeDictionary.Kids
                        .Cast<IndirectObjectReference>()
                        .Select(k => _oldToNewMap[k])
                        .ToList(); // Must iterate immediately as Kids is cleared within ReplaceAllChildren

                    pageTreeNodeDictionary.ReplaceAllChildren(newChildRefs);
                }
            }

            // Create a new page tree node
            var parent = PageTreeNodeDictionary.CreateNew(_newPageNodes.Select(p => p.Id.Reference).ToArray());
            var parentIndirectObject = _mainPdf.IndirectObjectManager.Add(parent);

            rootPageTreeNode.AddChild(parentIndirectObject.Id.Reference);

            _mainPdf.IndirectObjectManager.Update(rootPageTreeNodeIndirectObject);
        }

        // For the given dictionary, find all indirect object references, dereference, copy, recurse.
        private async Task CopyResourcesAsync(Dictionary? resources)
        {
            if (resources is null)
            {
                return;
            }

            foreach (var entry in resources)
            {
                if (entry.Value is not IndirectObjectReference reference)
                {
                    continue;
                }

                if (_oldToNewMap.ContainsKey(reference))
                {
                    continue;
                }

                var obj = await _pdfToAppend.IndirectObjects.GetAsync(reference)
                        ?? throw new InvalidPdfException("Unable to dereference page resource from source PDF");

                if (obj.Object is Dictionary)
                {
                    await CopyResourcesAsync(obj.Object as Dictionary);
                }

                IPdfObject target = obj.Object;

                // If the object is a stream, copy the stream contents. For performance reasons, the content is not
                // contained within the parsed stream object itself, but has a reference to its location in the file. This 
                // won't work when trying to save the merged file as the data is in the target file.
                if (obj.Object is StreamObject<IStreamDictionary> ssObject)
                {
                    var ms = new MemoryStream();

                    ssObject.Data.Data.Position = 0;
                    await ssObject.Data.Data.CopyToAsync(ms);
                    var contents = new StreamData(ms, ssObject.Data.Compressed, ssObject.Data.Filters);

                    target = new StreamObject<IStreamDictionary>(contents, ssObject.Dictionary);
                }

                var newObj = _mainPdf.IndirectObjectManager.Add(target);

                _oldToNewMap.Add(reference, newObj.Id.Reference);
            }
        }
    }
}
