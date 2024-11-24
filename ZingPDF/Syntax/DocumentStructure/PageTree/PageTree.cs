using Nito.AsyncEx;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.DocumentStructure.PageTree;

public class PageTree
{
    private readonly IIndirectObjectDictionary _indirectObjectDictionary;

    private readonly AsyncLazy<IndirectObject> _rootPageTreeNode;
    private readonly ResettableAsyncLazy<IList<IndirectObject>> _nodes;

    public PageTree(IIndirectObjectDictionary indirectObjectDictionary, IndirectObjectReference rootPageTreeNodeRef)
    {
        ArgumentNullException.ThrowIfNull(indirectObjectDictionary, nameof(indirectObjectDictionary));
        ArgumentNullException.ThrowIfNull(rootPageTreeNodeRef, nameof(rootPageTreeNodeRef));

        _indirectObjectDictionary = indirectObjectDictionary;

        _rootPageTreeNode = new AsyncLazy<IndirectObject>(async () =>
        {
            return await _indirectObjectDictionary.GetAsync(rootPageTreeNodeRef)
                ?? throw new InvalidPdfException("Unable to find root page tree node");
        });

        _nodes = new ResettableAsyncLazy<IList<IndirectObject>>(async () =>
        {
            var rootPageTreeNode = await _rootPageTreeNode;

            var subNodes = await ((PageTreeNodeDictionary)rootPageTreeNode.Object).GetSubNodesAsync(_indirectObjectDictionary);

            return new List<IndirectObject>([rootPageTreeNode, ..subNodes]);
        });
    }

    public async Task<IndirectObject> GetRootPageTreeNodeAsync() => await _rootPageTreeNode;

    public async Task<PageTreeNodeDictionary> GetRootPageTreeNodeDictionaryAsync()
        => (PageTreeNodeDictionary)(await _rootPageTreeNode).Object;

    public async Task<IList<IndirectObject>> GetPagesAsync()
    {
        return (await _nodes).Where(n => n.Object is PageDictionary).ToList();
    }

    public async Task<int> GetPageCountAsync() => (await GetRootPageTreeNodeDictionaryAsync()).PageCount;

    public Task<IList<IndirectObject>> GetAllNodesAsync() => _nodes.Task;

    public void Reset() => _nodes.Reset();
}
