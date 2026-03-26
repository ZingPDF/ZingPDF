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
            using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("PageTree.ResolveRootPageTreeNode");
            var documentCatalog = await _objects.GetDocumentCatalogAsync();

            if (documentCatalog.GetAs<IPdfObject>(Constants.DictionaryKeys.DocumentCatalog.Pages) is IndirectObjectReference rootPageTreeNodeRef)
            {
                return await _objects.GetAsync(rootPageTreeNodeRef)
                    ?? throw new InvalidPdfException($"Unable to resolve page tree root: {rootPageTreeNodeRef}");
            }

            // Fall back to the property wrapper path for unusual PDFs that don't expose
            // the root page tree as a direct indirect reference in the catalog.
            return await documentCatalog.Pages.GetIndirectObjectAsync();
        });

        _nodes = new ResettableAsyncLazy<IList<IndirectObject>>(async () =>
        {
            using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("PageTree.BuildNodeList");
            var rootPageTreeNode = await _rootPageTreeNode;

            var subNodes = await ((PageTreeNodeDictionary)rootPageTreeNode.Object).GetSubNodesAsync(_objects);

            return new List<IndirectObject>([rootPageTreeNode, ..subNodes]);
        });

        _pages = new ResettableAsyncLazy<IList<IndirectObject>>(async () =>
        {
            using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("PageTree.BuildPageList");
            return [.. (await _nodes).Where(node => node.Object is PageDictionary)];
        });

        _pageCount = new ResettableAsyncLazy<int>(async () =>
        {
            using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("PageTree.GetPageCount");
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

    public async Task<IndirectObject> GetRootPageTreeNodeAsync()
    {
        using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("PageTree.GetRootPageTreeNodeAsync");
        return await _rootPageTreeNode;
    }

    public async Task<PageTreeNodeDictionary> GetRootPageTreeNodeDictionaryAsync()
        => (PageTreeNodeDictionary)(await _rootPageTreeNode).Object;

    public async Task<IList<IndirectObject>> GetPagesAsync()
    {
        using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("PageTree.GetPagesAsync");
        return await _pages.Task;
    }

    public async Task<int> GetPageCountAsync()
    {
        using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("PageTree.GetPageCountAsync");
        return await _pageCount.Task;
    }

    public Task<IList<IndirectObject>> GetAllNodesAsync() => _nodes.Task;

    public void Reset()
    {
        _nodes.Reset();
        _pages.Reset();
        _pageCount.Reset();
    }
}
