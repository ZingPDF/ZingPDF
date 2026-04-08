using ZingPDF.Elements;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.DocumentStructure.PageTree;

namespace ZingPDF;

/// <summary>
/// Fluent mutation surface for editing pages in an existing PDF.
/// </summary>
public sealed class PdfPageEditingBuilder
{
    private readonly Pdf _pdf;
    private readonly List<IPageEditOperation> _operations = [];
    private bool _applied;

    internal PdfPageEditingBuilder(Pdf pdf)
    {
        _pdf = pdf ?? throw new ArgumentNullException(nameof(pdf));
    }

    /// <summary>
    /// Configures page mutations and returns the builder so it can be saved fluently.
    /// </summary>
    public PdfPageEditingBuilder Pages(Action<PdfPagesBuilder> configurePages)
    {
        ArgumentNullException.ThrowIfNull(configurePages);

        var builder = new PdfPagesBuilder();
        configurePages(builder);
        _operations.AddRange(builder.Build());

        return this;
    }

    /// <summary>
    /// Applies the configured page mutations to the underlying <see cref="Pdf"/>.
    /// </summary>
    public async Task<Pdf> ApplyAsync()
    {
        if (_applied)
        {
            return _pdf;
        }

        var context = new PdfAuthoringBuilder.AuthoringContext(_pdf);

        foreach (var operation in _operations)
        {
            await operation.ApplyAsync(_pdf, context);
        }

        _applied = true;
        return _pdf;
    }

    /// <summary>
    /// Applies the configured page mutations and saves the PDF to the supplied stream.
    /// </summary>
    public async Task SaveAsync(Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);

        await ApplyAsync();
        await _pdf.SaveAsync(output);
    }

    /// <summary>
    /// Applies the configured page mutations and saves the PDF to a file path.
    /// </summary>
    public async Task SaveToFileAsync(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var output = File.Create(fullPath);
        await SaveAsync(output);
    }

    internal interface IPageEditOperation
    {
        Task ApplyAsync(Pdf pdf, PdfAuthoringBuilder.AuthoringContext context);
    }

    internal sealed record UpdateExistingPageOperation(
        int PageNumber,
        PdfAuthoringBuilder.PagePlan PagePlan) : IPageEditOperation
    {
        public async Task ApplyAsync(Pdf pdf, PdfAuthoringBuilder.AuthoringContext context)
        {
            var page = await pdf.GetPageAsync(PageNumber);

            if (PagePlan.PageSize is not null)
            {
                page.Dictionary.Set(
                    Constants.DictionaryKeys.PageTree.MediaBox,
                    Rectangle.FromSize(PagePlan.PageSize));
                pdf.Objects.Update(page.IndirectObject);
            }

            await PagePlan.ApplyAsync(page, context);
        }
    }

    internal sealed record AppendPageOperation(
        PdfAuthoringBuilder.PagePlan PagePlan) : IPageEditOperation
    {
        public async Task ApplyAsync(Pdf pdf, PdfAuthoringBuilder.AuthoringContext context)
        {
            var page = await pdf.AppendPageAsync(PagePlan.PageSize is null
                ? null
                : options => options.MediaBox = Rectangle.FromSize(PagePlan.PageSize));

            await PagePlan.ApplyAsync(page, context);
        }
    }

    internal sealed record InsertPageOperation(
        int PageNumber,
        PdfAuthoringBuilder.PagePlan PagePlan) : IPageEditOperation
    {
        public async Task ApplyAsync(Pdf pdf, PdfAuthoringBuilder.AuthoringContext context)
        {
            var page = await pdf.InsertPageAsync(PageNumber, PagePlan.PageSize is null
                ? null
                : options => options.MediaBox = Rectangle.FromSize(PagePlan.PageSize));

            await PagePlan.ApplyAsync(page, context);
        }
    }

    internal sealed record RemovePageOperation(int PageNumber) : IPageEditOperation
    {
        public Task ApplyAsync(Pdf pdf, PdfAuthoringBuilder.AuthoringContext context)
            => pdf.DeletePageAsync(PageNumber);
    }
}

/// <summary>
/// Configures page-level mutations for a loaded or newly created <see cref="Pdf"/>.
/// </summary>
public sealed class PdfPagesBuilder
{
    private readonly List<PdfPageEditingBuilder.IPageEditOperation> _operations = [];

    /// <summary>
    /// Selects an existing 1-based page number and applies authored page operations to it.
    /// </summary>
    public PdfPagesBuilder Page(int pageNumber, Action<PdfAuthoringBuilder.PdfPageAuthoringBuilder> configurePage)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentNullException.ThrowIfNull(configurePage);

        var builder = new PdfAuthoringBuilder.PdfPageAuthoringBuilder();
        configurePage(builder);
        _operations.Add(new PdfPageEditingBuilder.UpdateExistingPageOperation(pageNumber, builder.Build()));

        return this;
    }

    /// <summary>
    /// Appends a new page to the end of the document and applies authored page operations to it.
    /// </summary>
    public PdfPagesBuilder Append(Action<PdfAuthoringBuilder.PdfPageAuthoringBuilder> configurePage)
    {
        ArgumentNullException.ThrowIfNull(configurePage);

        var builder = new PdfAuthoringBuilder.PdfPageAuthoringBuilder();
        configurePage(builder);
        _operations.Add(new PdfPageEditingBuilder.AppendPageOperation(builder.Build()));

        return this;
    }

    /// <summary>
    /// Inserts a new page before the specified 1-based page number and applies authored page operations to it.
    /// </summary>
    public PdfPagesBuilder Insert(int pageNumber, Action<PdfAuthoringBuilder.PdfPageAuthoringBuilder> configurePage)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentNullException.ThrowIfNull(configurePage);

        var builder = new PdfAuthoringBuilder.PdfPageAuthoringBuilder();
        configurePage(builder);
        _operations.Add(new PdfPageEditingBuilder.InsertPageOperation(pageNumber, builder.Build()));

        return this;
    }

    /// <summary>
    /// Removes an existing 1-based page number from the document.
    /// </summary>
    public PdfPagesBuilder Remove(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        _operations.Add(new PdfPageEditingBuilder.RemovePageOperation(pageNumber));
        return this;
    }

    /// <summary>
    /// Removes an existing 1-based page number from the document.
    /// </summary>
    public PdfPagesBuilder Delete(int pageNumber) => Remove(pageNumber);

    internal IReadOnlyList<PdfPageEditingBuilder.IPageEditOperation> Build() => [.. _operations];
}
