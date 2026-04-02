using System.Runtime.CompilerServices;
using Nito.AsyncEx;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.DocumentStructure.PageTree;

public class PageTree
{
    private readonly IPdfObjectCollection _objects;
    private readonly Dictionary<int, IndirectObject> _pageLookupCache = [];

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

            return await documentCatalog.Pages.GetIndirectObjectAsync();
        });

        _nodes = new ResettableAsyncLazy<IList<IndirectObject>>(async () =>
        {
            using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("PageTree.BuildNodeList");
            var rootPageTreeNode = await _rootPageTreeNode;

            var subNodes = await ((PageTreeNodeDictionary)rootPageTreeNode.Object).GetSubNodesAsync(_objects);

            return new List<IndirectObject>([rootPageTreeNode, .. subNodes]);
        });

        _pages = new ResettableAsyncLazy<IList<IndirectObject>>(async () =>
        {
            using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("PageTree.BuildPageList");
            var pages = new List<IndirectObject>();
            var pageNumber = 1;

            await foreach (var page in EnumeratePagesAsync())
            {
                pages.Add(page);
                _pageLookupCache[pageNumber++] = page;
            }

            return pages;
        });

        _pageCount = new ResettableAsyncLazy<int>(async () =>
        {
            using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("PageTree.GetPageCount");
            var rootPageTreeNode = await GetRootPageTreeNodeDictionaryAsync();

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

    public async IAsyncEnumerable<IndirectObject> EnumeratePagesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var page in EnumeratePagesUnderNodeAsync(await _rootPageTreeNode, cancellationToken))
        {
            yield return page;
        }
    }

    public async Task<IndirectObject> GetPageAsync(int pageNumber)
    {
        using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("PageTree.GetPageAsync");
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));

        if (_pageLookupCache.TryGetValue(pageNumber, out var cachedPage))
        {
            return cachedPage;
        }

        var location = await TryFindPageLocationAsync(await _rootPageTreeNode, pageNumber - 1);
        if (location != null)
        {
            _pageLookupCache[pageNumber] = location.Page;
        }

        return location?.Page
            ?? throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} must be less than or equal to the total number of pages.");
    }

    public async Task<(IndirectObject Page, IndirectObject Parent, int ChildIndex)> GetPageLocationAsync(int pageNumber)
    {
        using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("PageTree.GetPageLocationAsync");
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));

        var location = await TryFindPageLocationAsync(await _rootPageTreeNode, pageNumber - 1);
        return location is null
            ? throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} must be less than or equal to the total number of pages.")
            : (location.Page, location.Parent, location.ChildIndex);
    }

    public async Task<int> GetPageCountAsync()
    {
        using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("PageTree.GetPageCountAsync");
        var fastCount = await TryGetDirectPageCountAsync();
        if (fastCount != null)
        {
            return fastCount.Value;
        }

        return await _pageCount.Task;
    }

    public Task<IList<IndirectObject>> GetAllNodesAsync() => _nodes.Task;

    public void Reset()
    {
        _pageLookupCache.Clear();
        _nodes.Reset();
        _pages.Reset();
        _pageCount.Reset();
    }

    private async Task<int?> TryGetDirectPageCountAsync()
    {
        var catalog = await _objects.GetDocumentCatalogAsync();
        if (catalog.GetAs<IndirectObjectReference>(Constants.DictionaryKeys.DocumentCatalog.Pages) is not IndirectObjectReference pageTreeRef)
        {
            return null;
        }

        var pageTreeNode = (await _objects.GetAsync(pageTreeRef)).Object as PageTreeNodeDictionary;
        return pageTreeNode?.GetAs<Number>(Constants.DictionaryKeys.PageTree.PageTreeNode.Count);
    }

    private async Task<PageLocation?> TryFindPageLocationAsync(IndirectObject node, int zeroBasedPageIndex)
    {
        if (node.Object is PageDictionary)
        {
            return zeroBasedPageIndex == 0
                ? new PageLocation(node, node, -1)
                : null;
        }

        var pageTreeNode = (PageTreeNodeDictionary)node.Object;
        var remainingIndex = zeroBasedPageIndex;
        var kids = await pageTreeNode.Kids.GetAsync();
        var childIndex = 0;

        foreach (var childRef in kids.Cast<IndirectObjectReference>())
        {
            var child = await _objects.GetAsync(childRef);

            if (child.Object is PageDictionary)
            {
                if (remainingIndex == 0)
                {
                    return new PageLocation(child, node, childIndex);
                }

                remainingIndex--;
                childIndex++;
                continue;
            }

            if (remainingIndex == 0)
            {
                return await TryFindPageLocationAsync(child, 0);
            }

            var childPageCount = await GetNodePageCountAsync((PageTreeNodeDictionary)child.Object);
            if (remainingIndex < childPageCount)
            {
                return await TryFindPageLocationAsync(child, remainingIndex);
            }

            remainingIndex -= childPageCount;
            childIndex++;
        }

        return null;
    }

    private async IAsyncEnumerable<IndirectObject> EnumeratePagesUnderNodeAsync(
        IndirectObject node,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (node.Object is PageDictionary)
        {
            yield return node;
            yield break;
        }

        var pageTreeNode = (PageTreeNodeDictionary)node.Object;
        var kids = await pageTreeNode.Kids.GetAsync();

        foreach (var childRef in kids.Cast<IndirectObjectReference>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var child = await _objects.GetAsync(childRef);

            await foreach (var page in EnumeratePagesUnderNodeAsync(child, cancellationToken))
            {
                yield return page;
            }
        }
    }

    private async Task<int> GetNodePageCountAsync(PageTreeNodeDictionary pageTreeNode)
    {
        if (pageTreeNode.GetAs<Number>(Constants.DictionaryKeys.PageTree.PageTreeNode.Count) is Number directCount)
        {
            return directCount;
        }

        var kids = await pageTreeNode.Kids.GetAsync();
        var count = 0;

        foreach (var childRef in kids.Cast<IndirectObjectReference>())
        {
            var child = await _objects.GetAsync(childRef);
            count += child.Object is PageDictionary
                ? 1
                : await GetNodePageCountAsync((PageTreeNodeDictionary)child.Object);
        }

        return count;
    }

    private sealed record PageLocation(IndirectObject Page, IndirectObject Parent, int ChildIndex);
}
