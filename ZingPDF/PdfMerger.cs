using ZingPDF.Syntax;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF
{
    internal class PdfMerger
    {
        private readonly Pdf _mainPdf;
        private readonly IPdf _pdfToAppend;

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
            // Simple document merging...
            // We're going to add the root page tree node of the incoming to this one as a child of its root
            // - Starting with the incoming root page tree node, recursively process all properties.
            // - For an indirect object reference, dereference, copy the source, and recurse again.
            // - For dictionaries and arrays, loop through properties/items and recurse.

            var rootPageTreeNodeToAppend = await _pdfToAppend.PageTree.GetRootPageTreeNodeAsync();

            var newObj = _mainPdf.IndirectObjects.Add(rootPageTreeNodeToAppend.Object);

            _oldToNewMap.Add(rootPageTreeNodeToAppend.Id.Reference, newObj.Id.Reference);

            newObj.Object = await CopyReferencesAsync(rootPageTreeNodeToAppend.Object);

            var rootPageTreeNodeIndirectObject = await _mainPdf.PageTree.GetRootPageTreeNodeAsync();
            var rootPageTreeNode = (PageTreeNodeDictionary)rootPageTreeNodeIndirectObject.Object;

            // The old root page tree node won't have had a parent, set it to the new root.
            // N.B. This part is vital for correct display in Acrobat Reader.
            ((PageTreeNodeDictionary)newObj.Object).SetParent(rootPageTreeNodeIndirectObject.Id.Reference);

            // Add incoming root page tree node as a child of this PDF's root page tree node.
            await rootPageTreeNode.AddChildAsync(_oldToNewMap[rootPageTreeNodeToAppend.Id.Reference]);

            _mainPdf.IndirectObjects.Update(rootPageTreeNodeIndirectObject);
        }

        // For a given object, loop through its items and recursively process.
        // For a dictionary, loop through its properties.
        // For a stream object, process its dictionary.
        // For an array, loop through its items.
        // For an indirect object reference, dereference, copy the source, and recurse again.
        private async Task<IPdfObject> CopyReferencesAsync(IPdfObject obj)
        {
            if (obj is Dictionary dict)
            {
                return await CopyDictionaryReferencesAsync(dict);
            }
            else if (obj is IStreamObject streamObject)
            {
                return await CopyStreamObjectReferencesAsync(streamObject);
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

        private async Task<Dictionary> CopyDictionaryReferencesAsync(Dictionary dictionary)
        {
            foreach (var entry in dictionary)
            {
                dictionary.Set(entry.Key, await CopyReferencesAsync(entry.Value));
            }

            return dictionary;
        }

        private async Task<IStreamObject> CopyStreamObjectReferencesAsync(IStreamObject streamObject)
        {
            foreach (var entry in streamObject.Dictionary)
            {
                streamObject.Dictionary.Set(entry.Key, await CopyReferencesAsync(entry.Value));
            }

            return streamObject;
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
            if (_oldToNewMap.TryGetValue(reference, out IndirectObjectReference? value))
            {
                return value;
            }

            var obj = await _pdfToAppend.IndirectObjects.GetAsync(reference)
                ?? throw new InvalidPdfException("Unable to dereference page resource from source PDF");

            IPdfObject target = obj.Object;

            var newObj = _mainPdf.IndirectObjects.Add(target);

            _oldToNewMap.Add(reference, newObj.Id.Reference);

            // If the object is a stream, copy the stream contents. For performance reasons, the content is not
            // contained within the parsed stream object itself, but has a reference to its location in the file. This 
            // won't work when trying to save the merged file as the data is in the target file.
            if (obj.Object is StreamObject<IStreamDictionary> ssObject)
            {
                var ms = new MemoryStream();

                ssObject.Data.Position = 0;
                await ssObject.Data.CopyToAsync(ms);

                target = new StreamObject<IStreamDictionary>(ms, ssObject.Dictionary);
            }

            newObj.Object = await CopyReferencesAsync(target);

            return newObj.Id.Reference;
        }
    }
}
