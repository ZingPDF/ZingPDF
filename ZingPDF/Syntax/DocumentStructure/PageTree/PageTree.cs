using Nito.AsyncEx;
using ZingPDF.Extensions;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.DocumentStructure.PageTree;

public class PageTree
{
    private readonly IPdfEditor _pdfEditor;

    private readonly AsyncLazy<IndirectObject> _rootPageTreeNode;
    private readonly ResettableAsyncLazy<IList<IndirectObject>> _nodes;

    public PageTree(IPdfEditor pdfEditor, DictionaryProperty<PageTreeNodeDictionary> rootPageTreeNode)
    {
        ArgumentNullException.ThrowIfNull(pdfEditor, nameof(pdfEditor));
        ArgumentNullException.ThrowIfNull(rootPageTreeNode, nameof(rootPageTreeNode));

        _pdfEditor = pdfEditor;

        _rootPageTreeNode = new AsyncLazy<IndirectObject>(async () =>
        {
            return await rootPageTreeNode.GetIndirectObjectAsync()
                ?? throw new InvalidPdfException("Unable to find root page tree node");
        });

        _nodes = new ResettableAsyncLazy<IList<IndirectObject>>(async () =>
        {
            var rootPageTreeNode = await _rootPageTreeNode;

            var subNodes = await ((PageTreeNodeDictionary)rootPageTreeNode.Object).GetSubNodesAsync(_pdfEditor);

            return new List<IndirectObject>([rootPageTreeNode, ..subNodes]);
        });
    }

    public async Task<IndirectObject> GetRootPageTreeNodeAsync() => await _rootPageTreeNode;

    public async Task<PageTreeNodeDictionary> GetRootPageTreeNodeDictionaryAsync()
        => (PageTreeNodeDictionary)(await _rootPageTreeNode).Object;

    public async Task<IList<IndirectObject>> GetPagesAsync()
    {
        return [.. (await _nodes).Where(n => n.Object is PageDictionary)];
    }

    public async Task<int> GetPageCountAsync() => (await GetRootPageTreeNodeDictionaryAsync()).PageCount;

    public Task<IList<IndirectObject>> GetAllNodesAsync() => _nodes.Task;

    public void Reset() => _nodes.Reset();
}
