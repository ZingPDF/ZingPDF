using Nito.AsyncEx;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects.IndirectObjects;

using ZingPDF.Syntax.Objects;

namespace ZingPDF.Syntax.DocumentStructure.PageTree;

public class PageTree
{
    private readonly IPdfObjectCollection _objects;

    private readonly AsyncLazy<IndirectObject> _rootPageTreeNode;
    private readonly ResettableAsyncLazy<IList<IndirectObject>> _nodes;
    private readonly ResettableAsyncLazy<IList<IndirectObject>> _pages;
    private readonly ResettableAsyncLazy<int> _pageCount;

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

        _pages = new ResettableAsyncLazy<IList<IndirectObject>>(async () =>
            [.. (await _nodes).Where(node => node.Object is PageDictionary)]);

        _pageCount = new ResettableAsyncLazy<int>(async () =>
        {
            var rootPageTreeNode = await GetRootPageTreeNodeDictionaryAsync();

            // Reading the root /Count directly avoids the property wrapper and
            // indirect-resolution pipeline for the common page-count hot path.
            if (rootPageTreeNode.GetAs<Number>(Constants.DictionaryKeys.PageTree.PageTreeNode.Count) is Number directCount)
            {
                return directCount;
            }

            return await rootPageTreeNode.PageCount.GetAsync();
        });
    }

    public async Task<IndirectObject> GetRootPageTreeNodeAsync() => await _rootPageTreeNode;

    public async Task<PageTreeNodeDictionary> GetRootPageTreeNodeDictionaryAsync()
        => (PageTreeNodeDictionary)(await _rootPageTreeNode).Object;

    public Task<IList<IndirectObject>> GetPagesAsync() => _pages.Task;

    public Task<int> GetPageCountAsync() => _pageCount.Task;

    public Task<IList<IndirectObject>> GetAllNodesAsync() => _nodes.Task;

    public void Reset()
    {
        _nodes.Reset();
        _pages.Reset();
        _pageCount.Reset();
    }
}
