using Nito.AsyncEx;
using ZingPDF.ObjectModel.DocumentStructure.PageTree;
using ZingPDF.ObjectModel.Objects.IndirectObjects;

namespace ZingPDF.Parsing;

internal class PageTree
{
    private readonly ReadOnlyIndirectObjectDictionary _indirectObjectDictionary;

    private readonly AsyncLazy<PageTreeNode> _root;
    private readonly AsyncLazy<List<IndirectObject>> _pages;

    public PageTree(IndirectObjectReference root, ReadOnlyIndirectObjectDictionary indirectObjectDictionary)
    {
        _indirectObjectDictionary = indirectObjectDictionary ?? throw new ArgumentNullException(nameof(indirectObjectDictionary));

        _root = new AsyncLazy<PageTreeNode>(async () =>
        {
            return await _indirectObjectDictionary.GetAsync<PageTreeNode>(root)
                ?? throw new InvalidPdfException("Unable to find root page tree node");
        });

        _pages = new AsyncLazy<List<IndirectObject>>(async () =>
        {
            return await GetSubPagesAsync(await _root);
        });
    }

    public async Task<IndirectObject> GetAsync(int pageNumber)
    {
        return (await _pages)[pageNumber - 1];
    }

    public async Task<int> GetPageCountAsync()
    {
        return (await _root).PageCount;
    }

    /// <summary>
    /// Recursively get all descendant subpages from the supplied <see cref="PageTreeNode"/>.
    /// </summary>
    private async Task<List<IndirectObject>> GetSubPagesAsync(PageTreeNode pageTreeNode)
    {
        // TODO: check page ordering, should mimic whatever Acrobat Reader infers

        // TODO: we're parsing all pages and nodes in full. Is there a more performant way to index all pages?

        List<IndirectObject> pages = [];

        foreach (var refObj in pageTreeNode.Kids)
        {
            var ior = (IndirectObjectReference)refObj;

            var obj = await _indirectObjectDictionary.GetAsync(ior)
                ?? throw new InvalidPdfException("Unable to find referenced page");

            if (obj.Children.First() is Page)
            {
                pages.Add(obj);
            }
            else if (obj.Children.First() is PageTreeNode ptn)
            {
                pages.AddRange(await GetSubPagesAsync(ptn));
            }
        }

        return pages;
    }
}
