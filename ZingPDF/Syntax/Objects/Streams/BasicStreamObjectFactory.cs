namespace ZingPDF.Syntax.Objects.Streams;

/// <summary>
/// Simple implementation of <see cref="StreamObjectFactory"/> that uses a provided <see cref="Stream"/> as the source data.
/// Optionally, a collection of <see cref="FilterConfig"/> can be provided to specify the filters to be applied.
/// </summary>
internal class BasicStreamObjectFactory(Stream data, IEnumerable<FilterConfig>? filters) : StreamObjectFactory
{
    protected override Task<Stream> GetDataAsync() => Task.FromResult(data);
    protected override IEnumerable<FilterConfig> GetFilters() => filters ?? [];
}
