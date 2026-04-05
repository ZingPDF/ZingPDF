using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax;

namespace ZingPDF
{
    internal class PdfMerger
    {
        private readonly Pdf _mainPdf;
        private readonly IPdf _pdfToAppend;
        private readonly PdfObjectGraphCopier _copier;

        public PdfMerger(Pdf mainPdf, IPdf pdfToAppend)
        {
            ArgumentNullException.ThrowIfNull(mainPdf, nameof(mainPdf));
            ArgumentNullException.ThrowIfNull(pdfToAppend, nameof(pdfToAppend));

            _mainPdf = mainPdf;
            _pdfToAppend = pdfToAppend;
            _copier = new PdfObjectGraphCopier(mainPdf, pdfToAppend);
        }

        public async Task AppendAsync()
        {
            var rootPageTreeNodeToAppend = await _pdfToAppend.Objects.PageTree.GetRootPageTreeNodeAsync();
            var clonedRoot = new PageTreeNodeDictionary((Syntax.Objects.Dictionaries.Dictionary)rootPageTreeNodeToAppend.Object.Clone());
            var newObj = await _mainPdf.Objects.AddAsync(clonedRoot);

            _copier.RegisterMapping(rootPageTreeNodeToAppend.Reference, newObj.Reference);
            newObj.Object = await _copier.CopyAsync(clonedRoot);

            var rootPageTreeNodeIndirectObject = await _mainPdf.Objects.PageTree.GetRootPageTreeNodeAsync();
            var rootPageTreeNode = (PageTreeNodeDictionary)rootPageTreeNodeIndirectObject.Object;

            ((PageTreeNodeDictionary)newObj.Object).SetParent(rootPageTreeNodeIndirectObject.Reference);

            await rootPageTreeNode.AddChildAsync(
                newObj.Reference,
                rootPageTreeNodeToAppend.Object is PageTreeNodeDictionary appendedTree
                    ? (int)(await appendedTree.PageCount.GetAsync())
                    : 1);

            _mainPdf.Objects.Update(rootPageTreeNodeIndirectObject);
            _mainPdf.Objects.PageTree.Reset();
        }
    }
}
