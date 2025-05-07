using Nito.AsyncEx;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.DocumentStructure.PageTree;

public class PageTree
{
    private readonly IPdfObjectCollection _objects;

    private readonly AsyncLazy<IndirectObject> _rootPageTreeNode;
    private readonly ResettableAsyncLazy<IList<IndirectObject>> _nodes;

    public PageTree(IPdfObjectCollection objects)
    {
        ArgumentNullException.ThrowIfNull(objects, nameof(objects));

        _objects = objects;

        _rootPageTreeNode = new AsyncLazy<IndirectObject>(async () =>
        {
            var documentCatalog = await _objects.GetDocumentCatalogAsync();

            return await documentCatalog.Pages.GetIndirectObjectAsync();
        });

        _nodes = new ResettableAsyncLazy<IList<IndirectObject>>(async () =>
        {
            var rootPageTreeNode = await _rootPageTreeNode;

            var subNodes = await ((PageTreeNodeDictionary)rootPageTreeNode.Object).GetSubNodesAsync(_objects);

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

    public async Task<int> GetPageCountAsync() => await (await GetRootPageTreeNodeDictionaryAsync()).PageCount.GetAsync();

    public Task<IList<IndirectObject>> GetAllNodesAsync() => _nodes.Task;

    public void Reset() => _nodes.Reset();
}
