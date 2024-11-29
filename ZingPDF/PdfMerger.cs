using ZingPDF.Syntax;
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
            // - Add incoming root page tree node as a new child of this PDF's root

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
                _oldToNewMap.TryAdd(node.Id.Reference, newObj.Id.Reference);

                pageNodeDictionary = (PageNode)await CopyReferencesAsync(pageNodeDictionary);
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

            // Add incoming root page tree node as a child of this PDF's root page tree node.
            rootPageTreeNode.AddChild(_oldToNewMap[_pdfToAppend.DocumentCatalog.Pages]);

            _mainPdf.IndirectObjectManager.Update(rootPageTreeNodeIndirectObject);
        }

        private async Task<IPdfObject> CopyReferencesAsync(IPdfObject obj)
        {
            if (obj is Dictionary dict)
            {
                return await CopyDictionaryReferencesAsync(dict);
            }
            else if (obj is ArrayObject ary)
            {
                return await CopyArrayReferencesAsync(ary);
            }
            else if (obj is IndirectObjectReference ior)
            {
                return await CopyReferenceAsync(ior);
            }

            return obj;
        }

        private async Task<Dictionary> CopyDictionaryReferencesAsync(Dictionary resources)
        {
            foreach (var entry in resources)
            {
                resources[entry.Key] = await CopyReferencesAsync(entry.Value);
            }

            return resources;
        }

        private async Task<ArrayObject> CopyArrayReferencesAsync(ArrayObject ary)
        {
            var newArray = new ArrayObject();

            foreach (var item in ary)
            {
                newArray.Add(await CopyReferencesAsync(item));
            }

            return newArray;
        }

        private async Task<IndirectObjectReference> CopyReferenceAsync(IndirectObjectReference reference)
        {
            Console.WriteLine($"Copying {reference}");

            if (_oldToNewMap.TryGetValue(reference, out IndirectObjectReference? value))
            {
                return value;
            }

            var obj = await _pdfToAppend.IndirectObjects.GetAsync(reference)
                ?? throw new InvalidPdfException("Unable to dereference page resource from source PDF");

            IPdfObject target = obj.Object;

            var newObj = _mainPdf.IndirectObjectManager.Add(target);

            _oldToNewMap.Add(reference, new IndirectObjectReference(new IndirectObjectId(0, 0)));

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

            target = await CopyReferencesAsync(target);

            var newObj = _mainPdf.IndirectObjectManager.Add(target);

            _oldToNewMap[reference] = newObj.Id.Reference;

            return newObj.Id.Reference;
        }
    }
}
